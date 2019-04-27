using LibCyStd.IO;
using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace LibCyStd
{
    public static class StreamUtils
    {
#if NETSTANDARD2_0
        public static int Read(this Stream stream, in Span<byte> bytes)
        {
            using var memOwner = MemoryPool<byte>.Shared.Rent(bytes.Length);
            bytes.CopyTo(memOwner.Memory.Span);
            ReadOnlyMemory<byte> buffer = memOwner.Memory.Slice(0, bytes.Length);
            var arraySeg = buffer.AsArraySeg();
            var cnt = stream.Read(
                arraySeg.Array,
                arraySeg.Offset,
                arraySeg.Count
            );
            buffer.Span.CopyTo(bytes);
            return cnt;
        }

        public static void Write(this Stream stream, in ReadOnlySpan<byte> bytes)
        {
            using var memOwner = MemoryPool<byte>.Shared.Rent(bytes.Length);
            bytes.CopyTo(memOwner.Memory.Span);
            ReadOnlyMemory<byte> buffer = memOwner.Memory.Slice(0, bytes.Length);
            var arraySeg = buffer.AsArraySeg();
            stream.Write(arraySeg.Array, arraySeg.Offset, arraySeg.Count);
        }

        public static async Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            var arraySeg = bytes.AsArraySeg();
            await stream.WriteAsync(
                arraySeg.Array,
                arraySeg.Offset,
                arraySeg.Count,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public static async Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> bytes)
        {
            await stream.WriteAsync(bytes, CancellationToken.None).ConfigureAwait(false);
        }

        public static async Task<int> ReadAsync(this Stream stream, Memory<byte> bytes, CancellationToken cancellationToken)
        {
            var arraySeg = ReadOnlyMemoryModule.AsArraySeg<byte>(bytes);
            return await stream.ReadAsync(
                arraySeg.Array,
                arraySeg.Offset,
                arraySeg.Count,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public static async Task<int> ReadAsync(this Stream stream, Memory<byte> bytes)
        {
            var arraySeg = ReadOnlyMemoryModule.AsArraySeg<byte>(bytes);
            return await stream.ReadAsync(
                arraySeg.Array,
                arraySeg.Offset,
                arraySeg.Count,
                CancellationToken.None
            ).ConfigureAwait(false);
        }
#endif
    }

    public static class CompressionModule
    {
        public static ReadOnlyMemory<byte> GZipDecompress(in ReadOnlySpan<byte> input)
        {
            using var memOwner = MemoryPool<byte>.Shared.Rent(input.Length);
            input.CopyTo(memOwner.Memory.Span);
            ReadOnlyMemory<byte> buffer = memOwner.Memory.Slice(0, input.Length);
            using var inputStream = new MemoryStream(buffer.AsArraySeg().Array);
            using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            gzip.CopyTo(outputStream, 8192);
            return outputStream.ToArray();
        }

        public static ReadOnlyMemory<byte> GZipCompress(in ReadOnlySpan<byte> input)
        {
            using var memOwner = MemoryPool<byte>.Shared.Rent(input.Length);
            input.CopyTo(memOwner.Memory.Span);
            ReadOnlyMemory<byte> buffer = memOwner.Memory.Slice(0, input.Length);
            using var output = new MemoryStream();
            var arraySeg = buffer.AsArraySeg();
            var gzip = new GZipStream(output, CompressionMode.Compress);
            try { gzip.Write(arraySeg.Array, arraySeg.Offset, arraySeg.Count); }
            finally { gzip.Dispose(); }
            return output.ToArray();
        }
    }
}