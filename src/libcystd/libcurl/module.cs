using System.Runtime.InteropServices;

namespace LibCyStd.LibCurl
{
    public static class CurlModule
    {
        public static Unit CurlEx(string message) => throw new CurlException(message);

        public static string CurlEzStrErr(CURLcode code)
        {
            var ptr = libcurl.curl_easy_strerror(code);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public static Unit CurlEx(string message, CURLcode result) => throw new CurlException(message, result);

        public static string CurlMultiStrErr(CURLMcode code)
        {
            var ptr = libcurl.curl_multi_strerror(code);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public static void ValidateSetOptResult(CURLcode code)
        {
            if (code == CURLcode.OK)
                return;
            else
                CurlEx("curl_easy_setopt returned error", code);
        }

        public static void ValidateGetInfoResult(CURLcode code)
        {
            if (code == CURLcode.OK)
                return;
            else
                CurlEx("curl_easy_getinfo returned error", code);
        }

        public static void ValidateMultiResult(CURLMcode code)
        {
            if (code == CURLMcode.OK)
                return;
            else
                throw new CurlException($"curl_multi_setopt returned: {code} ~ {CurlMultiStrErr(code)}");
        }
    }
}
