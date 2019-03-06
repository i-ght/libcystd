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
        public OneOf<InitTimerArgs, InitPollArgs, InitAsyncArgs, None> Args { get; }

        public OneOf<
            UvInitLoopFnSig, //init_loop
            UvInitTimerFnSig, //init_timer
            UvInitPollFnSig, //init_poll
            UvInitAsyncFnSig //init async
        > Init{ get; }

        public UvInitializer(
            in string cFunctionName,
            in OneOf<InitTimerArgs, InitPollArgs, InitAsyncArgs, None> args,
            in OneOf<
                UvInitLoopFnSig, //uv_init_loop fn sig
                UvInitTimerFnSig, //uv_init_timer fn sig
                UvInitPollFnSig, //uvi_nit_poll fn sig
                UvInitAsyncFnSig //uv_init_async fn sig
            > init)
        {
            CFunctionName = cFunctionName;
            Args = args;
            Init = init;
        }
    }

    public abstract class UvMemory : IDisposable
    {
        private bool _disposed;

        protected IntPtr Handle { get; }

        public Option<object> Data { get; set; }

        private uv_err_code InitUvObj(
            OneOf<InitTimerArgs, InitPollArgs, InitAsyncArgs, None> argsUnion,
            in OneOf<
                UvInitLoopFnSig,
                UvInitTimerFnSig,
                UvInitPollFnSig,
                UvInitAsyncFnSig
            > initFnUnion)
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

        protected virtual void Dispose(in bool disposing)
        {
            if (_disposed)
                return;

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

        public UvLoop()
            : base(libuv.uv_loop_size(), new UvInitializer("uv_loop_init", None.Value, (UvInitLoopFnSig)libuv.uv_loop_init))
        {
        }

        protected override void Dispose(in bool disposing)
        {
            if (_disposed)
                return;

            if (disposing) { }

            libuv.uv_stop(Handle);
            try
            {
                UvUtils.ValidateResult("uv_loop_close", libuv.uv_loop_close(Handle));
            }
            finally
            {
                base.Dispose(true);
            }

            _disposed = true;
        }
    }

    public abstract class UvHandle : UvMemory
    {
        private static readonly libuv.uv_close_cb CloseCb;

        static UvHandle()
        {
            CloseCb = CloseCallback;
        }

        private bool _disposed;
        private Option<Action<IntPtr>> _closeCbOpt;

        public uv_handle_type HandleType { get; }

        public IntPtr LoopPtr { get; }

        protected static unsafe GCHandle GCHandle(IntPtr handle)
        {
            IntPtr ptr = ((uv_handle_t*)handle)->data;
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

        private void BaseDispose()
        {
            base.Dispose(true);
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
            in IntPtr loopPtr,
            in UvInitializer uvInit)
            : base(libuv.uv_handle_size(handleType), uvInit)
        {
            HandleType = handleType;
            LoopPtr = loopPtr;
            var gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(this);
            var p = (uv_handle_t*)Handle;
            p->data = System.Runtime.InteropServices.GCHandle.ToIntPtr(gcHandle);
        }
    }

    public class UvTimer : UvHandle
    {
        private static readonly libuv.uv_timer_cb Cb;

        private static void TimerCallback(IntPtr handle)
        {
            var timer = StructData<UvTimer>(handle);
            if (!timer._cb.IsSome)
                ExnModule.InvalidOp("expected timer callback to have value.");
            timer._cb.Value(timer);
        }

        static UvTimer()
        {
            Cb = TimerCallback;
        }

        private Option<Action<UvTimer>> _cb;

        public UvTimer(in IntPtr loopPtr)
            : base(uv_handle_type.UV_TIMER, loopPtr, new UvInitializer("uv_timer_init", new InitTimerArgs(loopPtr), (UvInitTimerFnSig)libuv.uv_timer_init))
        {
        }

        public void Start(Action<UvTimer> callback, long timeout, long repeat)
        {
            _cb = callback;
            UvUtils.ValidateResult("uv_timer_start", libuv.uv_timer_start(Handle, Cb, timeout, repeat));
        }

        public void Stop()
        {
            UvUtils.ValidateResult("uv_timer_stop", libuv.uv_timer_stop(Handle));
        }
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

        static UvPoll()
        {
            Cb = PollCallback;
        }

        private Option<Action<UvPoll, int, int>> _cb;

        public UvPoll(in UvLoop loop, in IntPtr fd)
            : base(uv_handle_type.UV_POLL, loop, new UvInitializer("uv_poll_init", new InitPollArgs(loop, fd), (UvInitPollFnSig)libuv.uv_poll_init_socket))
        {
            _cb = None.Value;
        }

        public void Start(uv_poll_event eventMask, Action<UvPoll, int, int> callback)
        {
            _cb = callback;
            UvUtils.ValidateResult("uv_poll_start", libuv.uv_poll_start(Handle, (int)eventMask, Cb));
        }

        public void Stop()
        {
            UvUtils.ValidateResult("uv_poll_stop", libuv.uv_poll_stop(Handle));
        }
    }

    public class UvAsync : UvHandle
    {
        private static readonly libuv.uv_async_cb Cb;

        private static void AsyncCallback(IntPtr handle)
        {
            var async = StructData<UvAsync>(handle);
            async._cb(async);
        }

        static UvAsync()
        {
            Cb = AsyncCallback;
        }

        private readonly Action<UvAsync> _cb;

        public UvAsync(in IntPtr loopPtr, in Action<UvAsync> cb)
            : base(uv_handle_type.UV_ASYNC, loopPtr, new UvInitializer("uv_async_init", new InitAsyncArgs(loopPtr, Cb), (UvInitAsyncFnSig)libuv.uv_async_init))
        {
            _cb = cb;
        }

        public void Send()
        {
            UvUtils.ValidateResult("uv_async_send", libuv.uv_async_send(Handle));
        }
    }
}
