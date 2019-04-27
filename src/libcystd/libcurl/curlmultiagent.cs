using LibCyStd.LibUv;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibCyStd.LibCurl
{
    /// <summary>
    ///     What should be done after handle has completed its work?
    /// </summary>
    public enum ReqOpCompletedAction
    {
        /// <summary>
        ///     Reuse the handle with all its options unchanged and attach it again.
        ///     Useful for example if your request has failed and you want to try again.
        /// </summary>
        ReuseHandleAndRetry,

        /// <summary>
        ///     Reset the handle with <see cref="libcurl.curl_easy_reset" />. 
        /// </summary>
        ResetHandleAndNext
    }

    public class ReqCtx<TReqState>
    {
        public TReqState ReqState { get; }
        public Action<CurlEzHandle, TReqState> ConfigureEzReq { get; }
        public Func<CurlEzHandle, TReqState, CURLcode, ReqOpCompletedAction> HandleResp { get; }

        public ReqCtx(
            in TReqState reqState,
            in Action<CurlEzHandle, TReqState> configureEzReq,
            in Func<CurlEzHandle, TReqState, CURLcode, ReqOpCompletedAction> handleResp)
        {
            ReqState = reqState;
            ConfigureEzReq = configureEzReq;
            HandleResp = handleResp;
        }
    }

    internal class CurlMultiAgentState<TReqState>
    {
        public CurlMultiHandle MultiHandle { get; }
        public libcurl.socket_callback SocketCallback { get; }
        public libcurl.timer_callback TimerCallback { get; }
        public ConcurrentDictionary<IntPtr, ReqCtx<TReqState>> ActiveEzHandles { get; }
        public ConcurrentQueue<CurlEzHandle> InactiveEzHandles { get; }
        public ReadOnlyDictionary<IntPtr, CurlEzHandle> EzPool { get; }
        public ConcurrentDictionary<IntPtr, UvPoll> Sockets { get; }
        public ConcurrentQueue<ReqCtx<TReqState>> PendingRequests { get; }
        public ConcurrentQueue<CurlEzHandle> EzsToAdd { get; }
        public UvLoop Loop { get; set; }
        public UvTimer Timer { get; set; }
        public UvAsync MultiAdd { get; set; }
        public UvAsync Disposer { get; set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public CurlMultiAgentState(
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
            in CurlMultiHandle multiHandle,
            in libcurl.timer_callback timerCallback,
            in libcurl.socket_callback socketCallback,
            in ConcurrentDictionary<IntPtr, ReqCtx<TReqState>> activeEzHandles,
            in ConcurrentQueue<CurlEzHandle> inactiveEzHandles,
            in ReadOnlyDictionary<IntPtr, CurlEzHandle> ezPool,
            in ConcurrentDictionary<IntPtr, UvPoll> sockets,
            in ConcurrentQueue<ReqCtx<TReqState>> pendingReq,
            in ConcurrentQueue<CurlEzHandle> ezsToAdd)
        {
            MultiHandle = multiHandle;
            TimerCallback = timerCallback;
            SocketCallback = socketCallback;
            ActiveEzHandles = activeEzHandles;
            InactiveEzHandles = inactiveEzHandles;
            EzPool = ezPool;
            Sockets = sockets;
            PendingRequests = pendingReq;
            EzsToAdd = ezsToAdd;
        }
    }

    public class CurlMultiAgent<TReqState> : IDisposable
    {
        private readonly CurlMultiAgentState<TReqState> _state;
        private readonly ManualResetEventSlim _disposeEvent;

        private bool _disposed;

        private void Timeout(UvTimer __)
        {
            CurlModule.ValidateMultiResult(
                libcurl.curl_multi_socket_action(_state.MultiHandle, new IntPtr(-1), 0, out _)
            );
            CheckMultiInfo();
        }

        public void ExecReq(ReqCtx<TReqState> reqCtx)
        {
            Unit Enqueue()
            {
                _state.PendingRequests.Enqueue(reqCtx);
                return Unit.Value;
            }

            Unit Send(in CurlEzHandle easy)
            {
                reqCtx.ConfigureEzReq(easy, reqCtx.ReqState);

                if (!_state.ActiveEzHandles.ContainsKey(easy))
                    _state.ActiveEzHandles.TryAdd(easy, reqCtx);
                else
                    _state.ActiveEzHandles[easy] = reqCtx;

                _state.EzsToAdd.Enqueue(easy);
                _state.MultiAdd.Send();
                return Unit.Value;
            }

            _ = _state.InactiveEzHandles.TryDequeue(out var easy) switch
            {
                true => Send(easy),
                false => Enqueue()
            };
        }

        /// <summary>
        ///     TIMERFUNCTION implementation. called by curl
        /// </summary>
        /// <param name="multiHandle"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="userp"></param>
        /// <returns></returns>
        private int StartTimeout(IntPtr multiHandle, int timeoutMs, IntPtr userp)
        {
            if (timeoutMs < 0)
            {
                _state.Timer.Stop();
            }
            else
            {
                if (timeoutMs == 0)
                    timeoutMs = 1;
                _state.Timer.Start(Timeout, timeoutMs, 0);
            }
            return 0;
        }

        private static void EzClearCookies(in CurlEzHandle easy)
        {
            var result = libcurl.curl_easy_setopt(easy, CURLoption.COOKIELIST, "ALL");
            _ = result switch
            {
                CURLcode.OK => Unit.Value,
                _ => CurlModule.CurlEx("failed to clear ez req cookies", result)
            };
        }

        private void CheckMultiInfo()
        {
            IntPtr pMessage;

            while ((pMessage = libcurl.curl_multi_info_read(_state.MultiHandle, out _)) != IntPtr.Zero)
            {
                var message = Marshal.PtrToStructure<CURLMsg>(pMessage);
                if (message.msg != CURLMSG.DONE)
                    CurlModule.CurlEx($"Unexpected curl_multi_info_read result message: {message.msg}.");

                var easy = _state.EzPool[message.easy_handle];
                var reqCtx = _state.ActiveEzHandles[easy];
                var action = reqCtx.HandleResp(easy, reqCtx.ReqState, message.data.result);

                Unit Reuse()
                {
                    CurlModule.ValidateMultiResult(
                        libcurl.curl_multi_remove_handle(_state.MultiHandle, easy)
                    );
                    CurlModule.ValidateMultiResult(
                        libcurl.curl_multi_add_handle(_state.MultiHandle, easy)
                    );
                    return Unit.Value;
                }

                Unit Reset()
                {
                    CurlModule.ValidateMultiResult(
                         libcurl.curl_multi_remove_handle(_state.MultiHandle, easy)
                    );
                    EzClearCookies(easy);
                    libcurl.curl_easy_reset(easy);
                    _state.ActiveEzHandles.TryRemove(easy, out _);
                    _state.InactiveEzHandles.Enqueue(easy);
                    if (_state.PendingRequests.TryDequeue(out reqCtx))
                        ExecReq(reqCtx);
                    return Unit.Value;
                }

                _ = action switch
                {
                    ReqOpCompletedAction.ResetHandleAndNext => Reset(),
                    ReqOpCompletedAction.ReuseHandleAndRetry => Reuse(),
                    _ => ExnModule.InvalidOp($"out of range ReqOpCompletedAction returned {action}")
                };
            }
        }

        //private void EndPoll(in PollStatus status, in IntPtr sockfd)
        //{
        //    CURLcselect flags = 0;

        //    if ((status.Mask & PollMask.Readable) != 0)
        //        flags |= CURLcselect.IN;

        //    if ((status.Mask & PollMask.Writable) != 0)
        //        flags |= CURLcselect.OUT;

        //    Console.WriteLine("sockfd is " + sockfd);
        //    CheckMultiResult(
        //        libcurl.curl_multi_socket_action(_state.MultiHandle, sockfd, flags, out int _)
        //    );
        //    CheckMultiInfo();
        //}

        private void BeginPoll(in CURLpoll what, IntPtr sockfd)
        {
            uv_poll_event events = 0;

            if (what != CURLpoll.IN)
                events |= uv_poll_event.UV_WRITABLE;

            if (what != CURLpoll.OUT)
                events |= uv_poll_event.UV_READABLE;

            if (!_state.Sockets.TryGetValue(sockfd, out var poll))
            {
                poll = new UvPoll(_state.Loop, sockfd);/*_state.Loop.CreatePoll(sockfd);*/
                _state.Sockets.TryAdd(sockfd, poll);
            }

            void PollCb(UvPoll _, int status, int events)
            {
                var mask = (uv_poll_event)events;

                PollStatus Err()
                {
                    var errMsg = Marshal.PtrToStringAnsi(libuv.uv_strerror((uv_err_code)status));
                    throw new UvException(errMsg);
                }

                var pollStatus = status switch
                {
                    var i when i < 0 => Err(),
                    _ => new PollStatus(mask)
                };

                CURLcselect flags = 0;
                if ((pollStatus.Mask & uv_poll_event.UV_READABLE) != 0)
                    flags |= CURLcselect.IN;

                if ((pollStatus.Mask & uv_poll_event.UV_WRITABLE) != 0)
                    flags |= CURLcselect.OUT;

                CurlModule.ValidateMultiResult(
                    libcurl.curl_multi_socket_action(_state.MultiHandle, sockfd, flags, out var __)
                );
                CheckMultiInfo();
            }

            poll.Start(events, PollCb);
        }

        private void DisposePoll(IntPtr sockfd)
        {
            var poll = _state.Sockets[sockfd];
            poll.Stop();
            poll.Dispose();
            _state.Sockets.TryRemove(sockfd, out _);
        }

        /// <summary>
        ///     SOCKETFUNCTION implementation. will be called with information about what sockets to wait for, and for what activity. called by curl
        /// </summary>
        /// <param name="easy"></param>
        /// <param name="sockfd"></param>
        /// <param name="what"></param>
        /// <param name="userp"></param>
        /// <param name="socketp"></param>
        /// <returns></returns>
        private int HandleSocket(IntPtr easy, IntPtr sockfd, CURLpoll what, IntPtr userp, IntPtr socketp)
        {
            switch (what)
            {
                case CURLpoll.IN:
                case CURLpoll.OUT:
                case CURLpoll.INOUT:
                    BeginPoll(what, sockfd);
                    break;

                case CURLpoll.REMOVE:
                    DisposePoll(sockfd);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid CURLpoll received: {what}.");
            }
            return 0;
        }

        private void AddHandle(UvAsync _)
        {
            while (_state.EzsToAdd.TryDequeue(out var ez))
            {
                CurlModule.ValidateMultiResult(
                    libcurl.curl_multi_add_handle(_state.MultiHandle, ez)
                );
            }
        }

        private void Dispose(UvAsync _)
        {
            foreach (var ez in _state.EzPool.Values)
            {
                CurlModule.ValidateMultiResult(
                    libcurl.curl_multi_remove_handle(_state.MultiHandle, ez)
                );
                ez.Dispose();
            }

            _state.Timer.Stop();
            _state.Timer.Dispose();

            foreach (var poll in _state.Sockets.Values)
            {
                poll.Stop();
                poll.Dispose();
            }

            _state.MultiAdd.Dispose();
            _state.Disposer.Dispose();
            _disposed = true;
        }

        private void Activate()
        {
            using var waitHandle = new ManualResetEventSlim();
            void StartLoop()
            {
                using var loop = new UvLoop();
                var timer = new UvTimer(loop);
                var multiAdd = new UvAsync(loop, AddHandle);
                var disposer = new UvAsync(loop, Dispose);
                _state.Loop = loop;
                _state.Timer = timer;
                _state.MultiAdd = multiAdd;
                _state.Disposer = disposer;

                CurlModule.ValidateMultiResult(
                    libcurl.curl_multi_setopt(_state.MultiHandle, CURLMoption.SOCKETFUNCTION, _state.SocketCallback)
                );
                CurlModule.ValidateMultiResult(
                    libcurl.curl_multi_setopt(_state.MultiHandle, CURLMoption.TIMERFUNCTION, _state.TimerCallback)
                );

                waitHandle.Set();
                UvModule.ValidateResult("uv_run", libuv.uv_run(loop, uv_run_mode.UV_RUN_DEFAULT));
                _disposeEvent.Set();
            }

            new Thread(StartLoop).Start();
            waitHandle.Wait();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _state.Disposer.Send();
            _disposeEvent.Wait();
            _disposeEvent.Dispose();
        }

        public CurlMultiAgent(in int ezPoolSize)
        {
            _disposeEvent = new ManualResetEventSlim();
            var multiHandle = libcurl.curl_multi_init();
            var activeEzHandles = new ConcurrentDictionary<IntPtr, ReqCtx<TReqState>>();
            var inactiveEzHandles = new ConcurrentQueue<CurlEzHandle>();
            var tmp = new Dictionary<IntPtr, CurlEzHandle>();

            foreach (var _ in Enumerable.Range(0, ezPoolSize))
            {
                var handle = libcurl.curl_easy_init();
                tmp.Add(handle, handle);
                inactiveEzHandles.Enqueue(handle);
            }

            var pool = new ReadOnlyDictionary<IntPtr, CurlEzHandle>(tmp);
            var sockets = new ConcurrentDictionary<IntPtr, UvPoll>();
            var pending = new ConcurrentQueue<ReqCtx<TReqState>>();
            var ezsToAdd = new ConcurrentQueue<CurlEzHandle>();

            _state = new CurlMultiAgentState<TReqState>(
                multiHandle,
                StartTimeout,
                HandleSocket,
                activeEzHandles,
                inactiveEzHandles,
                pool,
                sockets,
                pending,
                ezsToAdd
            );

            Activate();
        }
    }
}

//using NetUV.Core.Handles;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Threading;

//namespace LibCyStd.LibCurl
//{
//    /// <summary>
//    ///     What should be done after handle has completed its work?
//    /// </summary>
//    public enum ReqOpCompletedAction
//    {
//        /// <summary>
//        ///     Reuse the handle with all its options unchanged and attach it again.
//        ///     Useful for example if your request has failed and you want to try again.
//        /// </summary>
//        ReuseHandleAndRetry,

//        /// <summary>
//        ///     Reset the handle with <see cref="libcurl.Reset" />. 
//        /// </summary>
//        ResetHandleAndNext
//    }

//    public class ReqCtx<TReqState>
//    {
//        public TReqState ReqState { get; }
//        public Action<SafeEasyHandle, TReqState> ConfigureEzReq { get; }
//        public Func<SafeEasyHandle, TReqState, CURLcode, ReqOpCompletedAction> HandleResp { get; }

//        public ReqCtx(
//            TReqState reqState,
//            Action<SafeEasyHandle, TReqState> configureEzReq,
//            Func<SafeEasyHandle, TReqState, CURLcode, ReqOpCompletedAction> handleResp)
//        {
//            ReqState = reqState;
//            ConfigureEzReq = configureEzReq;
//            HandleResp = handleResp;
//        }
//    }

//    internal class CurlMultiAgentState<TReqState>
//    {
//#pragma warning disable CS8618 // Non-nullable field is uninitialized.
//        public CurlMultiAgentState(
//#pragma warning restore CS8618 // Non-nullable field is uninitialized.
//        in SafeMultiHandle multiHandle,
//            in libcurl.timer_callback timerCallback,
//            in libcurl.socket_callback socketCallback,
//            in ConcurrentDictionary<IntPtr, ReqCtx<TReqState>> activeEzHandles,
//            in ConcurrentQueue<SafeEasyHandle> inactiveEzHandles,
//            in ReadOnlyDictionary<IntPtr, SafeEasyHandle> ezPool,
//            in ConcurrentDictionary<IntPtr, Poll> sockets,
//            in ConcurrentQueue<ReqCtx<TReqState>> pendingReq,
//            in ConcurrentQueue<SafeEasyHandle> ezsToAdd)
//        {
//            MultiHandle = multiHandle;
//            TimerCallback = timerCallback;
//            SocketCallback = socketCallback;
//            ActiveEzHandles = activeEzHandles;
//            InactiveEzHandles = inactiveEzHandles;
//            EzPool = ezPool;
//            Sockets = sockets;
//            PendingRequests = pendingReq;
//            EzsToAdd = ezsToAdd;
//        }

//        public SafeMultiHandle MultiHandle { get; }
//        public libcurl.socket_callback SocketCallback { get; }
//        public libcurl.timer_callback TimerCallback { get; }
//        public ConcurrentDictionary<IntPtr, ReqCtx<TReqState>> ActiveEzHandles { get; }
//        public ConcurrentQueue<SafeEasyHandle> InactiveEzHandles { get; }
//        public ReadOnlyDictionary<IntPtr, SafeEasyHandle> EzPool { get; }
//        public ConcurrentDictionary<IntPtr, Poll> Sockets { get; }
//        public ConcurrentQueue<ReqCtx<TReqState>> PendingRequests { get; }
//        public ConcurrentQueue<SafeEasyHandle> EzsToAdd { get; }
//        public Loop Loop { get; set; }
//        public NetUV.Core.Handles.Timer Timer { get; set; }
//        public Async MultiAdd { get; set; }
//        public Async Disposer { get; set; }
//    }

//    public class CurlMultiAgent<TReqState> : IDisposable
//    {
//        private readonly CurlMultiAgentState<TReqState> _state;

//        private bool _disposed;

//        private static string CurlEzStrErr(CURLcode code)
//        {
//            var ptr = libcurl.curl_easy_strerror(code);
//            return Marshal.PtrToStringAnsi(ptr);
//        }

//        private static string CurlMultiStrErr(CURLMcode code)
//        {
//            var ptr = libcurl.curl_multi_strerror(code);
//            return Marshal.PtrToStringAnsi(ptr);
//        }

//        private static void CheckMultiResult(CURLMcode code)
//        {
//            if (code == CURLMcode.OK)
//                return;

//            throw new CurlException($"curl_multi_setopt returned: {code} ~ {CurlMultiStrErr(code)}");
//        }

//        private void Timeout()
//        {
//            CheckMultiResult(
//                libcurl.curl_multi_socket_action(_state.MultiHandle, SafeSocketHandle.Invalid, 0, out _)
//            );
//            CheckMultiInfo();
//        }

//        public void ExecReq(ReqCtx<TReqState> reqCtx)
//        {
//            if (!_state.InactiveEzHandles.TryDequeue(out var easy))
//            {
//                _state.PendingRequests.Enqueue(reqCtx);
//                return;
//            }

//            reqCtx.ConfigureEzReq(easy, reqCtx.ReqState);

//            var ezHandle = easy.DangerousGetHandle();
//            if (!_state.ActiveEzHandles.ContainsKey(ezHandle))
//            {
//                if (!_state.ActiveEzHandles.TryAdd(ezHandle, reqCtx))
//                    throw new InvalidOperationException("failed to add ez handle.");
//            }
//            else
//            {
//                _state.ActiveEzHandles[ezHandle] = reqCtx;
//            }

//            _state.EzsToAdd.Enqueue(easy);
//            _state.MultiAdd.Send();
//        }

//        /// <summary>
//        ///     TIMERFUNCTION implementation. called by curl
//        /// </summary>
//        /// <param name="multiHandle"></param>
//        /// <param name="timeoutMs"></param>
//        /// <param name="userp"></param>
//        /// <returns></returns>
//        private int StartTimeout(IntPtr multiHandle, int timeoutMs, IntPtr userp)
//        {
//            if (timeoutMs < 0)
//                _state.Timer.Stop();
//            else if (timeoutMs == 0)
//                Timeout();
//            else
//                _state.Timer.Start(_ => Timeout(), timeoutMs, 0);
//            return 0;
//        }

//        private static void EzClearCookies(SafeEasyHandle easy)
//        {
//            var result = libcurl.curl_easy_setopt(easy, CURLoption.COOKIELIST, "ALL");
//            if (result != CURLcode.OK)
//                throw new CurlException($"failed to clear curl ez req cookies. curl_easy_setopt returned {result}, {CurlEzStrErr(result)}");
//        }

//        private void CheckMultiInfo()
//        {
//            IntPtr pMessage;

//            while ((pMessage = libcurl.curl_multi_info_read(_state.MultiHandle, out _)) != IntPtr.Zero)
//            {
//                var message = Marshal.PtrToStructure<CURLMsg>(pMessage);
//                if (message.msg != CURLMSG.DONE)
//                    throw new CurlException($"Unexpected curl_multi_info_read result message: {message.msg}.");

//                var easy = _state.EzPool[message.easy_handle];
//                var ezHandle = easy.DangerousGetHandle();

//                var reqCtx = _state.ActiveEzHandles[ezHandle];
//                var action = reqCtx.HandleResp(easy, reqCtx.ReqState, message.data.result);

//                if (action == ReqOpCompletedAction.ReuseHandleAndRetry)
//                {
//                    CheckMultiResult(
//                        libcurl.curl_multi_remove_handle(_state.MultiHandle, easy)
//                    );
//                    CheckMultiResult(
//                        libcurl.curl_multi_add_handle(_state.MultiHandle, easy)
//                    );
//                }
//                else if (action == ReqOpCompletedAction.ResetHandleAndNext)
//                {
//                    CheckMultiResult(
//                            libcurl.curl_multi_remove_handle(_state.MultiHandle, easy)
//                    );
//                    libcurl.curl_easy_reset(easy);
//                    EzClearCookies(easy);
//                    _state.ActiveEzHandles.TryRemove(ezHandle, out _);
//                    _state.InactiveEzHandles.Enqueue(easy);
//                    if (_state.PendingRequests.TryDequeue(out reqCtx))
//                        ExecReq(reqCtx);
//                }
//            }
//        }

//        private void EndPoll(in PollStatus status, in IntPtr sockfd)
//        {
//            CURLcselect flags = 0;

//            if ((status.Mask & PollMask.Readable) != 0)
//                flags |= CURLcselect.IN;

//            if ((status.Mask & PollMask.Writable) != 0)
//                flags |= CURLcselect.OUT;

//            CheckMultiResult(
//                libcurl.curl_multi_socket_action(_state.MultiHandle, sockfd, flags, out int _)
//            );
//            CheckMultiInfo();
//        }

//        private void BeginPoll(in CURLpoll what, in IntPtr sockfd)
//        {
//            PollMask events = 0;

//            if (what != CURLpoll.IN)
//                events |= PollMask.Writable;

//            if (what != CURLpoll.OUT)
//                events |= PollMask.Readable;

//            if (!_state.Sockets.TryGetValue(sockfd, out var poll))
//            {
//                poll = _state.Loop.CreatePoll(sockfd);
//                _state.Sockets.TryAdd(sockfd, poll);
//            }

//            var fd = sockfd;
//            poll.Start(events, (_, status) => EndPoll(status, fd));
//        }

//        private void DisposePoll(in IntPtr sockfd)
//        {
//            var poll = _state.Sockets[sockfd];
//            poll.Stop();
//            poll.CloseHandle(p => p.Dispose());
//            _state.Sockets.TryRemove(sockfd, out _);
//        }

//        /// <summary>
//        ///     SOCKETFUNCTION implementation. will be called with information about what sockets to wait for, and for what activity. called by curl
//        /// </summary>
//        /// <param name="easy"></param>
//        /// <param name="sockfd"></param>
//        /// <param name="what"></param>
//        /// <param name="userp"></param>
//        /// <param name="socketp"></param>
//        /// <returns></returns>
//        private int HandleSocket(IntPtr easy, IntPtr sockfd, CURLpoll what, IntPtr userp, IntPtr socketp)
//        {
//            switch (what)
//            {
//                case CURLpoll.IN:
//                case CURLpoll.OUT:
//                case CURLpoll.INOUT:
//                    BeginPoll(what, sockfd);
//                    break;

//                case CURLpoll.REMOVE:
//                    DisposePoll(sockfd);
//                    break;

//                default:
//                    throw new InvalidOperationException($"Invalid CURLpoll received: {what}");
//            }
//            return 0;
//        }

//        private void AddHandle(Async _)
//        {
//            while (_state.EzsToAdd.TryDequeue(out var ez))
//            {
//                CheckMultiResult(
//                    libcurl.curl_multi_add_handle(_state.MultiHandle, ez)
//                );
//            }
//        }

//        private void Dispose(Async ayy)
//        {
//            foreach (var ez in _state.EzPool.Values)
//            {
//                CheckMultiResult(
//                    libcurl.curl_multi_remove_handle(_state.MultiHandle, ez)
//                );
//                ez.Dispose();
//            }

//            _state.Timer.Stop();
//            _state.Timer.CloseHandle(t => t.Dispose());
//            _state.MultiAdd.CloseHandle(a => a.Dispose());

//            foreach (var poll in _state.Sockets.Values)
//            {
//                poll.Stop();
//                poll.CloseHandle(p => p.Dispose());
//            }

//            ayy.CloseHandle(a => a.Dispose());
//        }

//        private void Activate()
//        {
//            using (var waitHandle = new ManualResetEventSlim())
//            {
//                void StartLoop()
//                {
//                    using (var loop = new Loop())
//                    {
//                        var multiAdd = loop.CreateAsync(AddHandle);
//                        var timer = loop.CreateTimer();
//                        var disposer = loop.CreateAsync(Dispose);
//                        _state.Loop = loop;
//                        _state.Timer = timer;
//                        _state.MultiAdd = multiAdd;
//                        _state.Disposer = disposer;

//                        CheckMultiResult(
//                            libcurl.curl_multi_setopt(_state.MultiHandle, CURLMoption.SOCKETFUNCTION, _state.SocketCallback)
//                        );
//                        CheckMultiResult(
//                            libcurl.curl_multi_setopt(_state.MultiHandle, CURLMoption.TIMERFUNCTION, _state.TimerCallback)
//                        );

//                        waitHandle.Set();
//                        loop.RunDefault();
//                        _disposed = true;
//                    }
//                }

//                new Thread(StartLoop).Start();
//                waitHandle.Wait();
//            }
//        }

//        public void Dispose()
//        {
//            if (_disposed) return;
//            _state.Disposer.Send();
//        }

//        public CurlMultiAgent(in int ezPoolSize)
//        {
//            var multiHandle = libcurl.curl_multi_init();
//            if (multiHandle.IsInvalid)
//                throw new CurlException("curl_multi_handle returned NULL");

//            var activeEzHandles = new ConcurrentDictionary<IntPtr, ReqCtx<TReqState>>();
//            var inactiveEzHandles = new ConcurrentQueue<SafeEasyHandle>();
//            var tmp = new Dictionary<IntPtr, SafeEasyHandle>();

//            foreach (var _ in Enumerable.Range(0, ezPoolSize))
//            {
//                var handle = libcurl.curl_easy_init();
//                if (handle.IsInvalid)
//                    throw new CurlException("curl_easy_init returned NULL");
//                tmp.Add(handle.DangerousGetHandle(), handle);
//                inactiveEzHandles.Enqueue(handle);
//            }

//            var pool = new ReadOnlyDictionary<IntPtr, SafeEasyHandle>(tmp);
//            var sockets = new ConcurrentDictionary<IntPtr, Poll>();
//            var pending = new ConcurrentQueue<ReqCtx<TReqState>>();
//            var ezsToAdd = new ConcurrentQueue<SafeEasyHandle>();

//            _state = new CurlMultiAgentState<TReqState>(
//                multiHandle,
//                StartTimeout,
//                HandleSocket,
//                activeEzHandles,
//                inactiveEzHandles,
//                pool,
//                sockets,
//                pending,
//                ezsToAdd
//            );

//            Activate();
//        }
//    }
//}