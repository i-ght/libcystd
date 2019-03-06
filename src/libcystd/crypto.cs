using System;
using System.Security.Cryptography;

namespace LibCyStd
{
    public static class CryptoModule
    {
        public static ReadOnlyMemory<byte> CalcMd5(in ReadOnlyMemory<byte> input)
        {
            using var md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(input.AsArray());
        }

        public static string CalcMd5Hex(in ReadOnlyMemory<byte> input)
        {
            var hash = CalcMd5(input);
            return hash.ToHex();
        }
    }
}
