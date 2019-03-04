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

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern SafeEasyHandle curl_easy_init();

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_easy_cleanup(IntPtr handle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_perform(SafeEasyHandle handle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_easy_reset(SafeEasyHandle handle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(SafeEasyHandle handle, CURLoption option, int value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(SafeEasyHandle handle, CURLoption option, IntPtr value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern CURLcode curl_easy_setopt(SafeEasyHandle handle, CURLoption option, string value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(SafeEasyHandle handle, CURLoption option, byte[] value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(SafeEasyHandle handle, CURLoption option, write_callback value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_setopt(SafeEasyHandle handle, CURLoption option, unsafe_write_callback value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_getinfo(SafeEasyHandle handle, CURLINFO option, out int value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_getinfo(SafeEasyHandle handle, CURLINFO option, out IntPtr value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLcode curl_easy_getinfo(SafeEasyHandle handle, CURLINFO option, out double value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern CURLcode curl_easy_getinfo(SafeEasyHandle handle, CURLINFO option, IntPtr value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_easy_strerror(CURLcode errornum);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern SafeMultiHandle curl_multi_init();

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_cleanup(IntPtr multiHandle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_add_handle(SafeMultiHandle multiHandle, SafeEasyHandle easyHandle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_remove_handle(SafeMultiHandle multiHandle, SafeEasyHandle easyHandle);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_setopt(SafeMultiHandle multiHandle, CURLMoption option, int value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_multi_info_read(SafeMultiHandle multiHandle, out int msgsInQueue);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_socket_action(SafeMultiHandle multiHandle, SafeSocketHandle sockfd,
            CURLcselect evBitmask,
            out int runningHandles);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_multi_strerror(CURLMcode errornum);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int timer_callback(IntPtr multiHandle, int timeoutMs, IntPtr userp);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_setopt(SafeMultiHandle multiHandle, CURLMoption option, timer_callback value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int socket_callback(IntPtr easy, IntPtr s, CURLpoll what, IntPtr userp, IntPtr socketp);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CURLMcode curl_multi_setopt(SafeMultiHandle multiHandle, CURLMoption option,
            socket_callback value);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern SafeSlistHandle curl_slist_append(SafeSlistHandle slist, string data);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_slist_free_all(SafeSlistHandle pList);
    }
#pragma warning restore IDE1006 // Naming Styles
}
