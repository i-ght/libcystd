using System;
using System.Buffers;
using System.Security.Cryptography;

namespace LibCyStd
{
    public static class CryptoModule
    {
        public static Span<byte> CalcMd5(in Span<byte> input)
        {
            using var memOwner = MemoryPool<byte>.Shared.Rent(input.Length);
            input.CopyTo(memOwner.Memory.Span);
            ReadOnlyMemory<byte> tmp = memOwner.Memory.Slice(0, input.Length);
            var arraySeg = tmp.AsArraySeg();
            using var md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(arraySeg.Array, arraySeg.Offset, arraySeg.Count);
        }

        public static string CalcMd5Hex(in Span<byte> input)
        {
            var hash = CalcMd5(input);
            return hash.ToHex();
        }
    }
}
