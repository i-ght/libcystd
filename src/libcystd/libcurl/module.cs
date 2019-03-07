using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibCyStd.LibCurl
{
    public static class CurlModule
    {
        public static void CurlEx(in string message) => throw new CurlException(message);

        public static void CurlEx(in string message, in Exception inner) => throw new CurlException(message, inner);

        public static string CurlEzStrErr(in CURLcode code)
        {
            var ptr = libcurl.curl_easy_strerror(code);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public static void CurlEx2(in string message, in CURLcode result) => throw new CurlException(message, result);

        public static void CurlEx(in string funcName, in CURLcode result) => CurlEx($"{funcName} returned error: {result} ~ {CurlEzStrErr(result)}");

        public static string CurlMultiStrErr(in CURLMcode code)
        {
            var ptr = libcurl.curl_multi_strerror(code);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public static void ValidateSetOptResult(in CURLcode code)
        {
            if (code == CURLcode.OK)
                return;

            CurlEx("curl_easy_setopt", code);
        }

        public static void ValidateGetInfoResult(in CURLcode code)
        {
            if (code == CURLcode.OK)
                return;

            CurlEx("curl_easy_getinfo", code);
        }

        public static void ValidateMultiResult(CURLMcode code)
        {
            if (code == CURLMcode.OK)
                return;

            throw new CurlException($"curl_multi_setopt returned: {code} ~ {CurlModule.CurlMultiStrErr(code)}");
        }
    }
}
