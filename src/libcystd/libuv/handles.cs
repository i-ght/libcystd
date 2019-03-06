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
    using UvInitArgs = OneOf<InitTimerArgs, InitPollArgs, InitAsyncArgs, None>;
    using UvInitFunction = OneOf<Func<IntPtr, uv_err_code>, Func<IntPtr, IntPtr, uv_err_code>, Func<IntPtr, IntPtr, IntPtr, uv_err_code>, Func<IntPtr, IntPtr, libuv.uv_async_cb, uv_err_code>>;

    public class InitTimerArgs
    {
        public IntPtr LoopPtr { get; }

        public InitTimerArgs(in IntPtr loopPtr)
        {
            LoopPtr = loopPtr;
        }
    }

    public class InitPollArgs
    {
        public IntPtr LoopPtr { get; }
        public IntPtr Fd { get; }

        public InitPollArgs(in IntPtr loopPtr, in IntPtr fd)
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

    public class UvInitializer
    {
        public string CFunctionName { get; }
        public UvInitArgs Args { get; }
        public UvInitFunction Init { get; }

        public UvInitializer(
            in string cFunctionName,
            in UvInitArgs args,
            in UvInitFunction init)
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
#pragma warning restore IDE1006 // Naming Styles

        static UvMemory()
        {
            uv_loop_init = libuv.uv_loop_init;
            uv_poll_init_socket = libuv.uv_poll_init_socket;
            uv_timer_init = libuv.uv_timer_init;
            uv_async_init = libuv.uv_async_init;
        }

        private bool _disposed;

        protected IntPtr Handle { get; }

        public Option<object> Data { get; set; }

        private uv_err_code InitUvObj(
            UvInitArgs argsUnion,
            in UvInitFunction initFnUnion)
        {
            return initFnUnion.Match(
                initLoop => initLoop(Handle),
                initTimer =>
                {
                    InitTimerArgs args = argsUnion.AsT0;
                    return initTimer(args.LoopPtr, Handle);
                },
                initPoll =>
                {
                    InitPollArgs args = argsUnion.AsT1;
                    return initPoll(args.LoopPtr, Handle, args.Fd);
                },
                initAsync =>
                {
                    InitAsyncArgs args = argsUnion.AsT2;
                    return initAsync(args.LoopPtr, Handle, args.Cb);
                }
            );
        }

        private void InitMem(UvInitializer init)
        {
            var result = InitUvObj(init.Args, init.Init);
            UvUtils.ValidateResult(init.CFunctionName, result);
        }

        public bool Equals(UvMemory other) => Handle == other.Handle;

        protected virtual void Dispose(in bool disposing)
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
            in int size,
            in UvInitializer uvInit)
        {
            Handle = Marshal.AllocCoTaskMem(size);
            try { InitMem(uvInit); }
            catch { Marshal.FreeCoTaskMem(Handle); throw; }
        }

        ~UvMemory() => Dispose(false);

        public static implicit operator IntPtr(in UvMemory mem)
        {
            if (mem._disposed) ExnModule.ObjDisposed(mem.GetType().Name);
            return mem.Handle;
        }
    }

    public class UvLoop : UvMemory
    {
        private bool _disposed;

        public UvLoop() : base(libuv.uv_loop_size(), new UvInitializer("uv_loop_init", None.Value, uv_loop_init)) { }

        protected override void Dispose(in bool disposing)
        {
            if (_disposed) return;
            if (disposing) { }
            libuv.uv_stop(Handle);
            try { UvUtils.ValidateResult("uv_loop_close", libuv.uv_loop_close(Handle)); }
            finally { base.Dispose(true); }
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

        protected static unsafe GCHandle GCHandle(IntPtr handle)
        {
            var ptr = ((uv_handle_t*)handle)->data;
            if (ptr == IntPtr.Zero)
                ExnModule.InvalidOp("unexpected null @ data field of uv_handle_t* struct.");

            var gcHandle = System.Runtime.InteropServices.GCHandle.FromIntPtr(ptr);
            if (!gcHandle.IsAllocated)
                ExnModule.InvalidOp("expected gc handle to be allocated when attempting to get data field from uv_handle_t* struct");
            return gcHandle;
        }

        protected static unsafe T StructData<T>(IntPtr handle) where T : UvHandle
        {
            var gcHandle = GCHandle(handle);
            return (T)gcHandle.Target;
        }

        private void BaseDispose() => base.Dispose(true);

        public void Dispose(Action<IntPtr> closeCb)
        {
            _closeCbOpt = closeCb;
            Dispose();
        }

        protected override void Dispose(in bool disposing)
        {
            if (_disposed) return;
            if (disposing) { }
            libuv.uv_close(Handle, CloseCb);
            _disposed = true;
        }

        protected unsafe UvHandle(
            in uv_handle_type handleType,
            in UvLoop loop,
            in UvInitializer uvInit) : base(libuv.uv_handle_size(handleType), uvInit)
        {
            HandleType = handleType;
            Loop = loop;
            var gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(this);
            var p = (uv_handle_t*)Handle;
            p->data = System.Runtime.InteropServices.GCHandle.ToIntPtr(gcHandle);
        }

        private static void CloseCallback(IntPtr handle)
        {
            var uvHandle = StructData<UvHandle>(handle);
            if (uvHandle._closeCbOpt.IsSome)
                uvHandle._closeCbOpt.Value(handle);
            var gcHandle = GCHandle(handle);
            gcHandle.Free();
            uvHandle.BaseDispose();
        }

        static UvHandle() => CloseCb = CloseCallback;
    }

    public class UvTimer : UvHandle
    {
        private static readonly libuv.uv_timer_cb Cb;

        private Option<Action<UvTimer>> _cb;

        public UvTimer(in UvLoop loop) : base(uv_handle_type.UV_TIMER, loop, new UvInitializer("uv_timer_init", new InitTimerArgs(loop), uv_timer_init)) { }

        public void Start(Action<UvTimer> callback, long timeout, long repeat)
        {
            _cb = callback;
            UvUtils.ValidateResult("uv_timer_start", libuv.uv_timer_start(Handle, Cb, timeout, repeat));
        }

        public void Stop() => UvUtils.ValidateResult("uv_timer_stop", libuv.uv_timer_stop(Handle));

        private static void TimerCallback(IntPtr handle)
        {
            var timer = StructData<UvTimer>(handle);
            if (!timer._cb.IsSome)
                ExnModule.InvalidOp("expected timer callback to have value.");
            timer._cb.Value(timer);
        }

        static UvTimer() => Cb = TimerCallback;
    }

    public class UvPoll : UvHandle
    {
        private static readonly libuv.uv_poll_cb Cb;

        private Option<Action<UvPoll, int, int>> _cb;

        public UvPoll(in UvLoop loop, in IntPtr fd) : base(uv_handle_type.UV_POLL, loop, new UvInitializer("uv_poll_init", new InitPollArgs(loop, fd), uv_poll_init_socket))
            => _cb = None.Value;

        public void Start(uv_poll_event eventMask, Action<UvPoll, int, int> callback)
        {
            _cb = callback;
            UvUtils.ValidateResult("uv_poll_start", libuv.uv_poll_start(Handle, (int)eventMask, Cb));
        }

        public void Stop() => UvUtils.ValidateResult("uv_poll_stop", libuv.uv_poll_stop(Handle));

        private static void PollCallback(IntPtr handle, int status, int events)
        {
            var poll = StructData<UvPoll>(handle);
            if (!poll._cb.IsSome)
                ExnModule.InvalidOp("expected poll callback to have value.");
            poll._cb.Value(poll, status, events);
        }

        static UvPoll() => Cb = PollCallback;
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

        public UvAsync(in UvLoop loop, in Action<UvAsync> cb) : base(uv_handle_type.UV_ASYNC, loop, new UvInitializer("uv_async_init", new InitAsyncArgs(loop, Cb), (UvInitAsyncFnSig)libuv.uv_async_init))
            => _cb = cb;

        public void Send() => UvUtils.ValidateResult("uv_async_send", libuv.uv_async_send(Handle));
    }
}
