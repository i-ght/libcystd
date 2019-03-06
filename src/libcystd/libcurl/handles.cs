using System;
using System.Runtime.InteropServices;

namespace LibCyStd.LibCurl
{
    public enum NullPtrPolicy
    {
        Allowed,
        NotAllowed
    }

    public abstract class CurlMemory : IEquatable<CurlMemory>
    {
        private bool _disposed;

        protected IntPtr Handle { get; set; }

        protected abstract void Delete();

        protected virtual void Dispose(in bool disposing)
        {
            if (_disposed) return;
            if (disposing) { }
            Delete();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool Equals(CurlMemory other) => Handle == other.Handle;

        public override string ToString() => $"{GetType().Name}@{Handle}";

        protected CurlMemory(in IntPtr handle, in NullPtrPolicy nullPtrPolicy)
        {
            if (nullPtrPolicy == NullPtrPolicy.NotAllowed && handle == IntPtr.Zero)
                ExnModule.InvalidArg("cannot init with NULL handle.", nameof(handle));
            Handle = handle;
        }

        ~CurlMemory() => Dispose(false);

        public static implicit operator IntPtr(in CurlMemory mem)
        {
            if (mem._disposed) ExnModule.ObjDisposed(mem.GetType().Name);
            return mem.Handle;
        }
    }

    public class CurlEzHandle : CurlMemory
    {
        internal CurlEzHandle(in IntPtr handle) : base(handle, NullPtrPolicy.NotAllowed) { }

        protected override void Delete() => libcurl.curl_easy_cleanup(Handle);
    }

    public class CurlMultiHandle : CurlMemory
    {
        internal CurlMultiHandle(in IntPtr handle) : base(handle, NullPtrPolicy.NotAllowed) { }

        protected override void Delete()
        {
            var result = libcurl.curl_multi_cleanup(Handle);
            if (result != CURLMcode.OK)
                throw new CurlException($"curl_multi_cleanup returned error: {libcurl.curl_multi_strerror(result)}");
        }
    }

    public class CurlSlist : CurlMemory
    {
        public CurlSlist(in IntPtr handle) : base(handle, NullPtrPolicy.Allowed) { }

        protected override void Delete()
        {
            if (Handle == IntPtr.Zero) return;
            libcurl.curl_slist_free_all(Handle);
        }

        internal void SetHandle(in IntPtr handle) => Handle = handle;
    }
}
