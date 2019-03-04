using LibCyStd.LibOneOf;
using LibCyStd.LibOneOf.Types;
using System;
using System.Runtime.InteropServices;

namespace LibCyStd.LibUv
{
    public struct InitTimerArgs
    {
        public IntPtr LoopPtr { get; }

        public InitTimerArgs(in IntPtr loopPtr)
        {
            LoopPtr = loopPtr;
        }
    }

    public struct InitPollArgs
    {
        public IntPtr LoopPtr { get; }
        public IntPtr Fd { get; }

        public InitPollArgs(in IntPtr loopPtr, in IntPtr fd)
        {
            LoopPtr = loopPtr;
            Fd = fd;
        }
    }

    public struct InitAsyncArgs
    {
        public IntPtr LoopPtr { get; }
        public libuv.uv_async_cb Cb { get; }

        public InitAsyncArgs(IntPtr loopPtr, libuv.uv_async_cb cb)
        {
            LoopPtr = loopPtr;
            Cb = cb;
        }
    }

    public struct UvInitializer
    {
        public string CFunctionName { get; }
        public OneOf<InitTimerArgs, InitPollArgs, InitAsyncArgs, None> Args { get; }

        public OneOf<
            Func<IntPtr, uv_err_code>, //init_loop
            Func<IntPtr, IntPtr, uv_err_code>, //init_timer
            Func<IntPtr, IntPtr, IntPtr, uv_err_code>, //init_poll
            Func<IntPtr, IntPtr, libuv.uv_async_cb, uv_err_code> //init async
        > Init
        { get; }

        public UvInitializer(
            in string cFunctionName,
            in OneOf<InitTimerArgs, InitPollArgs, InitAsyncArgs, None> args,
            in OneOf<
                Func<IntPtr, uv_err_code>, //init_loop
                Func<IntPtr, IntPtr, uv_err_code>, //init_timer
                Func<IntPtr, IntPtr, IntPtr, uv_err_code>, //init_poll
                Func<IntPtr, IntPtr, libuv.uv_async_cb, uv_err_code> //init async
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
        private readonly IntPtr _handle;

        public IntPtr Handle
        {
            get
            {
                if (_disposed)
                    ExnModule.ObjDisposed(GetType().Name);
                return _handle;
            }
        }

        private uv_err_code InitUvObj(
            OneOf<InitTimerArgs, InitPollArgs, InitAsyncArgs, None> argsUnion,
            in OneOf<
                Func<IntPtr, uv_err_code>, //init_loop
                Func<IntPtr, IntPtr, uv_err_code>, //init_timer
                Func<IntPtr, IntPtr, IntPtr, uv_err_code>, //init_poll
                Func<IntPtr, IntPtr, libuv.uv_async_cb, uv_err_code> //init async
            > initFnUnion) // wtf i love C# function pointer syntax
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
            try
            {
                var result = InitUvObj(init.Args, init.Init);
                UvUtils.ValidateResult(init.CFunctionName, result);
            }
            catch
            {
                Marshal.FreeCoTaskMem(_handle);
                throw;
            }
        }

        protected UvMemory(
            in int size,
            in UvInitializer uvInit)
        {
            _handle = Marshal.AllocCoTaskMem(size);
            InitMem(uvInit);
        }

        protected virtual void Dispose(in bool disposing)
        {
            if (_disposed)
                return;

            if (disposing) { }

            Marshal.FreeCoTaskMem(_handle);

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UvMemory()
        {
            Dispose(false);
        }
    }

    public class UvLoop : UvMemory
    {
        private bool _disposed;

        public UvLoop()
            : base(libuv.uv_loop_size(), new UvInitializer("uv_loop_init", None.Value, (Func<IntPtr, uv_err_code>)libuv.uv_loop_init))
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
        private bool _disposed;
        private Option<Action<IntPtr>> _closeCbOpt;

        private readonly libuv.uv_close_cb _closeCb;

        public uv_handle_type HandleType { get; }

        public IntPtr LoopPtr { get; }

        private void OnClosed(IntPtr handle)
        {
            if (_closeCbOpt.IsSome)
                _closeCbOpt.Value(handle);
            base.Dispose(true);
        }

        public void Dispose(Action<IntPtr> closeCb)
        {
            _closeCbOpt = closeCb;
            Dispose();
        }

        protected override void Dispose(in bool disposing)
        {
            if (_disposed)
                return;
            if (disposing) { }
            libuv.uv_close(Handle, _closeCb);
            _disposed = true;
        }

        protected UvHandle(
            in uv_handle_type handleType,
            in IntPtr loopPtr,
            in UvInitializer uvInit)
            : base(libuv.uv_handle_size(handleType), uvInit)
        {
            HandleType = handleType;
            LoopPtr = loopPtr;
            _closeCb = OnClosed;
        }
    }

    public class UvTimer : UvHandle
    {
        public UvTimer(in IntPtr loopPtr)
            : base(uv_handle_type.UV_TIMER, loopPtr, new UvInitializer("uv_timer_init", new InitTimerArgs(loopPtr), (Func<IntPtr, IntPtr, uv_err_code>)libuv.uv_timer_init))
        {
        }
    }

    public class UvPoll : UvHandle
    {
        public UvPoll(in IntPtr loopPtr, in IntPtr fd)
            : base(uv_handle_type.UV_POLL, loopPtr, new UvInitializer("uv_poll_init", new InitPollArgs(loopPtr, fd), (Func<IntPtr, IntPtr, IntPtr, uv_err_code>)libuv.uv_poll_init_socket))
        {
        }
    }

    public class UvAsync : UvHandle
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly libuv.uv_async_cb _cb;
#pragma warning restore IDE0052 // Remove unread private members

        public UvAsync(in IntPtr loopPtr, in libuv.uv_async_cb cb)
            : base(uv_handle_type.UV_ASYNC, loopPtr, new UvInitializer("uv_async_init", new InitAsyncArgs(loopPtr, cb), (Func<IntPtr, IntPtr, libuv.uv_async_cb, uv_err_code>)libuv.uv_async_init))
        {
            _cb = cb;
        }
    }
}
