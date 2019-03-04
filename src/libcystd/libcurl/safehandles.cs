using System;
using System.Runtime.InteropServices;

namespace LibCyStd.LibCurl
{
    public sealed class SafeEasyHandle : SafeHandle
    {
        private SafeEasyHandle() : base(IntPtr.Zero, false)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            libcurl.curl_easy_cleanup(handle);
            return true;
        }
    }

    public sealed class SafeMultiHandle : SafeHandle
    {
        private SafeMultiHandle() : base(IntPtr.Zero, false)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            return libcurl.curl_multi_cleanup(handle) == CURLMcode.OK;
        }
    }

    public sealed class SafeSlistHandle : SafeHandle
    {
        private SafeSlistHandle() : base(IntPtr.Zero, false)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public static SafeSlistHandle Null => new SafeSlistHandle();

        protected override bool ReleaseHandle()
        {
            libcurl.curl_slist_free_all(this);
            return true;
        }
    }

    public sealed class SafeSocketHandle : SafeHandle
    {
        private SafeSocketHandle() : base(new IntPtr(-1), false)
        {
        }

        public override bool IsInvalid => handle == new IntPtr(-1);

        protected override bool ReleaseHandle()
        {
            return true;
        }

        public static implicit operator SafeSocketHandle(IntPtr ptr)
        {
            var handle = new SafeSocketHandle();
            handle.SetHandle(ptr);
            return handle;
        }

        public static readonly SafeSocketHandle Invalid = new IntPtr(-1);
    }
}
