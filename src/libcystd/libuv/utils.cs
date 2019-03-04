using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibCyStd.LibUv
{
    public static class UvUtils
    {
        public static void UvEx(in string msg) => throw new UvException(msg);

        public static void ValidateResult(in string funcName, in uv_err_code result)
        {
            if (result == uv_err_code.UV_OK)
                return;
            UvEx($"{funcName} returned {result}. {Marshal.PtrToStringAnsi(libuv.uv_strerror(result))}");
        }
    }
}
