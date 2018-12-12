using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibCyStd
{
    public static class StreamUtils
    {
        public static async Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            var array = bytes.AsArray();
            await stream.WriteAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false);
        }

        public static async Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> bytes)
        {
            await stream.WriteAsync(bytes, CancellationToken.None).ConfigureAwait(false);
        }
    }
}