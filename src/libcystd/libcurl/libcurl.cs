using System;
using System.Runtime.InteropServices;

namespace LibCyStd.LibCurl
{
    /// <summary>
    /// hello
    /// </summary>
    /// <remarks>
    ///     Type mappings (C -> C#):
    ///     - size_t -> UIntPtr
    ///     - int    -> int
    ///     - long   -> int
    /// </remarks>

#pragma warning disable IDE1006 // Naming Styles
    public static class libcurl
    {
        private const string LIBCURL = "libcurl";

#if DOWS
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);

        private static void LoadCurlErr()
        {
#if DEBUG
            throw new InvalidOperationException("Failed to load libcurl.dll. Is it located in application directory?");
#else
            Environment.FailFast("Failed to load libcurl.dll. Is it located in application directory?");
#endif
        }

        private static void LoadUvErr()
        {
#if DEBUG
            throw new InvalidOperationException("Failed to load libuv.dll. Is it located in application directory?");
#else
            Environment.FailFast("Failed to load libcurl.dll. Is it located in application directory?");
#endif
        }

        static libcurl()
        {
            var ptr = LoadLibraryW("libcurl.dll");
            if (ptr == IntPtr.Zero)
                LoadCurlErr();
            ptr = LoadLibraryW("libuv.dll");
            if (ptr == IntPtr.Zero)
                LoadUvErr();
        }
#endif

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_global_init(CURLglobal flags = CURLglobal.DEFAULT);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_global_cleanup();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong write_callback(IntPtr data, ulong size, ulong nmemb, IntPtr userdata);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate ulong unsafe_write_callback(byte* data, ulong size, ulong nmemb, void* userdata);

        [DllImport(LIBCURL, EntryPoint = "curl_easy_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr _curl_easy_init();

        public static CurlEzHandle curl_easy_init()
        {
            var handle = _curl_easy_init();
            if (handle == IntPtr.Zero)
                CurlModule.CurlEx("curl_easy_init returned NULL.");
            return new CurlEzHandle(handle);
        }

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_easy_cleanup(IntPtr handle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_perform(IntPtr handle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_easy_reset(IntPtr handle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(IntPtr handle, CURLoption option, int value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(IntPtr handle, CURLoption option, IntPtr value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern CURLcode curl_easy_setopt(IntPtr handle, CURLoption option, string value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(IntPtr handle, CURLoption option, byte[] value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(IntPtr handle, CURLoption option, write_callback value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(IntPtr handle, CURLoption option, unsafe_write_callback value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_getinfo(IntPtr handle, CURLINFO option, out int value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_getinfo(IntPtr handle, CURLINFO option, out IntPtr value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_getinfo(IntPtr handle, CURLINFO option, out double value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern CURLcode curl_easy_getinfo(IntPtr handle, CURLINFO option, IntPtr value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_easy_strerror(CURLcode errornum);

        [DllImport(LIBCURL, EntryPoint = "curl_multi_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr _curl_multi_init();

        public static CurlMultiHandle curl_multi_init()
        {
            var handle = _curl_multi_init();
            if (handle == IntPtr.Zero)
                CurlModule.CurlEx("curl_multi_init returned NULL.");
            return new CurlMultiHandle(handle);
        }

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_cleanup(IntPtr multiHandle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_add_handle(IntPtr multiHandle, IntPtr easyHandle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_remove_handle(IntPtr multiHandle, IntPtr easyHandle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_setopt(IntPtr multiHandle, CURLMoption option, int value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_multi_info_read(IntPtr multiHandle, out int msgsInQueue);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_socket_action(IntPtr multiHandle, IntPtr sockfd,
            CURLcselect evBitmask,
            out int runningHandles);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_multi_strerror(CURLMcode errornum);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int timer_callback(IntPtr multiHandle, int timeoutMs, IntPtr userp);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_setopt(IntPtr multiHandle, CURLMoption option, timer_callback value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int socket_callback(IntPtr easy, IntPtr s, CURLpoll what, IntPtr userp, IntPtr socketp);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_setopt(IntPtr multiHandle, CURLMoption option,
            socket_callback value);

        [DllImport(LIBCURL, EntryPoint = "curl_slist_append", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr _curl_slist_append(IntPtr slist, string data);

        public static CurlSlist curl_slist_append(CurlSlist slist, string data)
        {
            var handle = _curl_slist_append(slist, data);
            if (handle == IntPtr.Zero)
                CurlModule.CurlEx("curl_slist_append returned NULL.");
            slist.SetHandle(handle);
            return slist;
        }

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_slist_free_all(IntPtr pList);
    }
#pragma warning restore IDE1006 // Naming Styles
}
