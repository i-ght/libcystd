using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace LibCyStd
{
#if NETSTANDARD2_0
    public static class StreamUtils
    {
        public static async Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            var (success, arraySeg) = bytes.TryGetArraySegment();
            if (!success)
                ExnModule.InvalidOp("failed to get array from memory.");
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
    }
#endif

    public static class CompressionModule
    {
        public static ReadOnlyMemory<byte> GZipDecompress(in ReadOnlyMemory<byte> input)
        {
            using (var inputStream = new MemoryStream(input.AsArray()))
            using (var gzip = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzip.CopyTo(outputStream, 8192);
                return outputStream.ToArray();
            }
        }

        public static ReadOnlyMemory<byte> GZipCompress(in ReadOnlyMemory<byte> input)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                    gzip.Write(input.AsArray(), 0, input.Length);
                return output.ToArray();
            }
        }
    }
}