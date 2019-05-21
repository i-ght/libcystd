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
            TReqState reqState,
            Action<CurlEzHandle, TReqState> configureEzReq,
            Func<CurlEzHandle, TReqState, CURLcode, ReqOpCompletedAction> handleResp)
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
        public Dictionary<IntPtr, ReqCtx<TReqState>> ActiveEzHandles { get; }
        public Queue<CurlEzHandle> InactiveEzHandles { get; }
        public ReadOnlyDictionary<IntPtr, CurlEzHandle> EzPool { get; }
        public Dictionary<IntPtr, UvPoll> Sockets { get; }
        public Queue<ReqCtx<TReqState>> PendingRequests { get; }
        public Queue<CurlEzHandle> EzsToAdd { get; }
        public UvLoop Loop { get; set; }
        public UvTimer Timer { get; set; }
        public UvAsync MultiAdd { get; set; }
        public UvAsync Disposer { get; set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public CurlMultiAgentState(
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
            CurlMultiHandle multiHandle,
            libcurl.timer_callback timerCallback,
            libcurl.socket_callback socketCallback,
            Dictionary<IntPtr, ReqCtx<TReqState>> activeEzHandles,
            Queue<CurlEzHandle> inactiveEzHandles,
            ReadOnlyDictionary<IntPtr, CurlEzHandle> ezPool,
            Dictionary<IntPtr, UvPoll> sockets,
            Queue<ReqCtx<TReqState>> pendingReq,
            Queue<CurlEzHandle> ezsToAdd)
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
            // called when all easy handles are currently use.
            Unit Enqueue()
            {
                _state.PendingRequests.Enqueue(reqCtx);
                return Unit.Value;
            }

            //enqueue the easy handle, and send it to the uv event loop by using uv_async.
            Unit Send(CurlEzHandle easy)
            {
                reqCtx.ConfigureEzReq(easy, reqCtx.ReqState);
                _state.ActiveEzHandles[easy] = reqCtx;
                _state.EzsToAdd.Enqueue(easy);
                _state.MultiAdd.Send();
                return Unit.Value;
            }

            lock (_state)
            {
                if ( _state.InactiveEzHandles.Count > 0)
                {
                    var ez = _state.InactiveEzHandles.Dequeue();
                    Send(ez);
                }
                else
                {
                    Enqueue();
                }
            }
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

        private static void EzClearCookies(CurlEzHandle easy)
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

                Unit Retry()
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

                    Option<ReqCtx<TReqState>> next = Option.None;

                    lock (_state)
                    {
                        _state.ActiveEzHandles.Remove(easy);
                        _state.InactiveEzHandles.Enqueue(easy);
                        if (_state.PendingRequests.Count > 0)
                            next = _state.PendingRequests.Dequeue();
                    }

                    if (next.IsSome)
                        ExecReq(next.Value);

                    return Unit.Value;
                }

                _ = action switch
                {
                    ReqOpCompletedAction.ResetHandleAndNext => Reset(),
                    ReqOpCompletedAction.ReuseHandleAndRetry => Retry(),
                    _ => ExnModule.InvalidOp($"out of range ReqOpCompletedAction returned ~ {action}")
                };
            }
        }

        //private void EndPoll(PollStatus status, IntPtr sockfd)
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

        private void BeginPoll(CURLpoll what, IntPtr sockfd)
        {
            uv_poll_event events = 0;

            if (what != CURLpoll.IN)
                events |= uv_poll_event.UV_WRITABLE;

            if (what != CURLpoll.OUT)
                events |= uv_poll_event.UV_READABLE;

            if (!_state.Sockets.TryGetValue(sockfd, out var poll))
            {
                poll = new UvPoll(_state.Loop, sockfd);
                _state.Sockets.Add(sockfd, poll);
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

                CURLcselect flags = CURLcselect.NONE;
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
            _state.Sockets.Remove(sockfd);
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
            lock (_state)
            {
                while (_state.EzsToAdd.Count > 0)
                {
                    var ez = _state.EzsToAdd.Dequeue();
                    CurlModule.ValidateMultiResult(
                        libcurl.curl_multi_add_handle(_state.MultiHandle, ez)
                    );
                }
            }
        }

        private void Dispose(UvAsync __)
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
            _disposed = true;
            _state.Disposer.Send();
            _disposeEvent.Wait();
            _disposeEvent.Dispose();
        }

        public CurlMultiAgent(int ezPoolSize)
        {
            _disposeEvent = new ManualResetEventSlim();
            var multiHandle = libcurl.curl_multi_init();
            var activeEzHandles = new Dictionary<IntPtr, ReqCtx<TReqState>>();
            var inactiveEzHandles = new Queue<CurlEzHandle>();
            var handsUp = new Dictionary<IntPtr, CurlEzHandle>();

            for (var i = 0; i < ezPoolSize; i++)
            {
                var handle = libcurl.curl_easy_init();
                handsUp.Add(handle, handle);
                inactiveEzHandles.Enqueue(handle);
            }

            var pool = new ReadOnlyDictionary<IntPtr, CurlEzHandle>(handsUp);
            var sockets = new Dictionary<IntPtr, UvPoll>();
            var pending = new Queue<ReqCtx<TReqState>>();
            var ezsToAdd = new Queue<CurlEzHandle>();

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