using LibCyStd.LibOneOf;
using LibCyStd.LibOneOf.Types;
using System;
using System.Runtime.InteropServices;

namespace LibCyStd.LibUv
{
    using UvInitLoopFnSig = Func<IntPtr, uv_err_code>;
    using UvInitTimerFnSig = Func<IntPtr, IntPtr, uv_err_code>;
    using UvInitPollFnSig = Func<IntPtr, IntPtr, IntPtr, uv_err_code>;
    using UvInitAsyncFnSig = Func<IntPtr, IntPtr, libuv.uv_async_cb, uv_err_code>;
    using UvInitTcpFnSig = Func<IntPtr, IntPtr, uv_err_code>;
    using UvInitArgs = OneOf<InitTimerArgs, InitPollArgs, InitAsyncArgs, InitTcpArgs, None>;
    using UvInitFunction = OneOf<Func<IntPtr, uv_err_code>, Func<IntPtr, IntPtr, uv_err_code>, Func<IntPtr, IntPtr, IntPtr, uv_err_code>, Func<IntPtr, IntPtr, libuv.uv_async_cb, uv_err_code>>;

    public class InitTimerArgs
    {
        public IntPtr LoopPtr { get; }

        public InitTimerArgs(IntPtr loopPtr)
        {
            LoopPtr = loopPtr;
        }
    }

    public class InitPollArgs
    {
        public IntPtr LoopPtr { get; }
        public IntPtr Fd { get; }

        public InitPollArgs(IntPtr loopPtr, IntPtr fd)
        {
            LoopPtr = loopPtr;
            Fd = fd;
        }
    }

    public class InitAsyncArgs
    {
        public IntPtr LoopPtr { get; }
        public libuv.uv_async_cb Cb { get; }

        public InitAsyncArgs(IntPtr loopPtr, libuv.uv_async_cb cb)
        {
            LoopPtr = loopPtr;
            Cb = cb;
        }
    }

    public class InitTcpArgs
    {
        public IntPtr LoopPtr { get; }

        public InitTcpArgs(IntPtr loopPtr)
        {
            LoopPtr = loopPtr;
        }
    }

    public class UvInitializer
    {
        public string CFunctionName { get; }
        public UvInitArgs Args { get; }
        public UvInitFunction Init { get; }

        public UvInitializer(
            string cFunctionName,
            UvInitArgs args,
            UvInitFunction init)
        {
            CFunctionName = cFunctionName;
            Args = args;
            Init = init;
        }
    }

    public abstract class UvMemory : IDisposable, IEquatable<UvMemory>
    {
#pragma warning disable IDE1006 // Naming Styles
        protected static UvInitLoopFnSig uv_loop_init { get; }
        protected static UvInitPollFnSig uv_poll_init_socket { get; }
        protected static UvInitTimerFnSig uv_timer_init { get; }
        protected static UvInitAsyncFnSig uv_async_init { get; }
        protected static UvInitTcpFnSig uv_tcp_init { get; }
#pragma warning restore IDE1006 // Naming Styles

        static UvMemory()
        {
            uv_loop_init = libuv.uv_loop_init;
            uv_poll_init_socket = libuv.uv_poll_init_socket;
            uv_timer_init = libuv.uv_timer_init;
            uv_async_init = libuv.uv_async_init;
            uv_tcp_init = libuv.uv_tcp_init;
        }

        private bool _disposed;

        protected IntPtr Handle { get; }

        public Option<object> Data { get; set; }

        private uv_err_code InitUvObj(
            UvInitArgs argsUnion,
            UvInitFunction initFnUnion)
        {
            return initFnUnion.Match(
                initLoop => initLoop(Handle),
                initTimer =>
                {
                    InitTimerArgs args = argsUnion.T0Value;
                    return initTimer(args.LoopPtr, Handle);
                },
                initPoll =>
                {
                    InitPollArgs args = argsUnion.T1Value;
                    return initPoll(args.LoopPtr, Handle, args.Fd);
                },
                initAsync =>
                {
                    InitAsyncArgs args = argsUnion.T2Value;
                    return initAsync(args.LoopPtr, Handle, args.Cb);
                }
            );
        }

        private void InitMem(UvInitializer init)
        {
            var result = InitUvObj(init.Args, init.Init);
            UvModule.ValidateResult(init.CFunctionName, result);
        }

        public bool Equals(UvMemory other) => Handle == other.Handle;

        public override string ToString() => $"libuv.{GetType().Name}@{Handle}";

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) { }
            Marshal.FreeCoTaskMem(Handle);
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected UvMemory(
            int size,
            UvInitializer uvInit)
        {
            Handle = Marshal.AllocCoTaskMem(size);
            try { InitMem(uvInit); }
            catch { Marshal.FreeCoTaskMem(Handle); throw; }
        }

        ~UvMemory() => Dispose(false);

        public static implicit operator IntPtr(UvMemory mem)
        {
            if (mem._disposed) ExnModule.ObjDisposed(mem.GetType().Name);
            return mem.Handle;
        }
    }

    public class UvLoop : UvMemory
    {
        private bool _disposed;

        public UvLoop() : base(libuv.uv_loop_size(), new UvInitializer("uv_loop_init", Option.None, uv_loop_init)) { }

        public uv_err_code Close() => libuv.uv_loop_close(Handle);

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) { }
            libuv.uv_stop(Handle);
            UvModule.ValidateResult("uv_loop_close", Close());
            base.Dispose(true);
            _disposed = true;
        }
    }

    public abstract class UvHandle : UvMemory
    {
        private static readonly libuv.uv_close_cb CloseCb;

        private bool _disposed;
        private Option<Action<IntPtr>> _closeCbOpt;

        public uv_handle_type HandleType { get; }

        public UvLoop Loop { get; }

        protected static unsafe GCHandle GetGCHandle(IntPtr handle)
        {
            var ptr = ((uv_handle_t*)handle)->data;
            if (ptr == IntPtr.Zero)
                ExnModule.InvalidOp("unexpected null @ data field of uv_handle_t* struct.");

            var gcHandle = GCHandle.FromIntPtr(ptr);
            if (!gcHandle.IsAllocated)
                ExnModule.InvalidOp("expected gc handle to be allocated when attempting to get data field from uv_handle_t* struct");
            return gcHandle;
        }

        protected static unsafe T StructData<T>(IntPtr handle) where T : UvHandle
        {
            var gcHandle = GetGCHandle(handle);
            return (T)gcHandle.Target;
        }

        private void BaseDispose() => base.Dispose(true);

        public void Dispose(Action<IntPtr> closeCb)
        {
            _closeCbOpt = closeCb;
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) { }
            libuv.uv_close(Handle, CloseCb);
            _disposed = true;
        }

        public override string ToString() => $"{HandleType}@{Handle}";

        protected unsafe UvHandle(
            uv_handle_type handleType,
            UvLoop loop,
            UvInitializer uvInit) : base(libuv.uv_handle_size(handleType), uvInit)
        {
            HandleType = handleType;
            Loop = loop;
            var gcHandle = GCHandle.Alloc(this);
            var p = (uv_handle_t*)Handle;
            p->data = GCHandle.ToIntPtr(gcHandle);
        }

        private static void CloseCallback(IntPtr handle)
        {
            var uvHandle = StructData<UvHandle>(handle);
            if (uvHandle._closeCbOpt.IsSome)
                uvHandle._closeCbOpt.Value(handle);
            var gcHandle = GetGCHandle(handle);
            gcHandle.Free();
            uvHandle.BaseDispose();
        }

        static UvHandle() => CloseCb = CloseCallback;
    }

    public class UvTimer : UvHandle
    {
        private static readonly libuv.uv_timer_cb Cb;

        private Option<Action<UvTimer>> _cb;

        public UvTimer(UvLoop loop) : base(uv_handle_type.UV_TIMER, loop, new UvInitializer("uv_timer_init", new InitTimerArgs(loop), uv_timer_init)) { }

        public void Start(Action<UvTimer> callback, long timeout, long repeat)
        {
            _cb = callback;
            UvModule.ValidateResult("uv_timer_start", libuv.uv_timer_start(Handle, Cb, timeout, repeat));
        }

        public void Stop() => UvModule.ValidateResult("uv_timer_stop", libuv.uv_timer_stop(Handle));

        private static void TimerCallback(IntPtr handle)
        {
            var timer = StructData<UvTimer>(handle);
            if (timer._cb.IsNone)
                ExnModule.InvalidOp("expected timer callback to have value.");
            timer._cb.Value(timer);
        }

        static UvTimer() => Cb = TimerCallback;
    }

    public class UvPoll : UvHandle
    {
        private static readonly libuv.uv_poll_cb Cb;

        private static void PollCallback(IntPtr handle, int status, int events)
        {
            var poll = StructData<UvPoll>(handle);
            if (!poll._cb.IsSome)
                ExnModule.InvalidOp("expected poll callback to have value.");
            poll._cb.Value(poll, status, events);
        }

        static UvPoll() => Cb = PollCallback;

        private Option<Action<UvPoll, int, int>> _cb;

        public UvPoll(UvLoop loop, IntPtr fd) : base(uv_handle_type.UV_POLL, loop, new UvInitializer("uv_poll_init", new InitPollArgs(loop, fd), uv_poll_init_socket))
            => _cb = Option.None;

        public void Start(
            uv_poll_event eventMask,
            Action<UvPoll, int, int> callback)
        {
            _cb = callback;
            UvModule.ValidateResult("uv_poll_start", libuv.uv_poll_start(Handle, (int)eventMask, Cb));
        }

        public void Stop() => UvModule.ValidateResult("uv_poll_stop", libuv.uv_poll_stop(Handle));
    }

    public class UvAsync : UvHandle
    {
        private static readonly libuv.uv_async_cb Cb;

        private static void AsyncCallback(IntPtr handle)
        {
            var async = StructData<UvAsync>(handle);
            async._cb(async);
        }

        static UvAsync() => Cb = AsyncCallback;

        private readonly Action<UvAsync> _cb;

        public void Send() => UvModule.ValidateResult("uv_async_send", libuv.uv_async_send(Handle));

        public UvAsync(UvLoop loop, Action<UvAsync> cb) : base(uv_handle_type.UV_ASYNC, loop, new UvInitializer("uv_async_init", new InitAsyncArgs(loop, Cb), uv_async_init)) => _cb = cb;
    }

    public class UvTcp : UvHandle
    {
        private bool _noDelay;
        //private TimeSpan _keepAlive;

        public bool NoDelay
        {
            get => _noDelay;
            set
            {
                var val = value ? 1 : 0;
                UvModule.ValidateResult("uv_tcp_nodelay", libuv.uv_tcp_nodelay(this, val));
                _noDelay = value;
            }
        }

        public UvTcp(UvLoop loop) : base(uv_handle_type.UV_TCP, loop, new UvInitializer("uv_tcp_init", new InitTcpArgs(loop), uv_tcp_init)) { }
    }
}
