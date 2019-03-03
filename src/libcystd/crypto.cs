using System;
using System.Security.Cryptography;

namespace LibCyStd
{
    public static class CryptoUtils
    {
        public static ReadOnlyMemory<byte> CalcMd5(in ReadOnlyMemory<byte> input)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                return md5.ComputeHash(ReadOnlyMemoryModule.AsArray(input));
            }
        }

        public static string CalcMd5Hex(in ReadOnlyMemory<byte> input)
        {
            var hash = CalcMd5(input);
            return ReadOnlyMemoryModule.ToHex(hash);
        }
    }

}
