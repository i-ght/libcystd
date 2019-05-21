using LibCyStd.Tasks;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LibCyStd.Net
{
    public static class TcpClientModule
    {
        private static readonly ReadOnlyMemory<byte> CrLfCrLf;

        static TcpClientModule() => CrLfCrLf = new byte[] { 13, 10, 13, 10 };

        private static int Recv(
            Stream input,
            Stream output,
            int bufferSize)
        {
            using var memOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
            var span = memOwner.Memory.Span;
            var read = input.Read(span);
            if (read <= 0)
                ExnModule.InvalidOp("stream read op returned 0 bytes.");
            output.Write(span.Slice(0, read));
            return read;
        }

        private static async Task<int> RecvAsync(
            Stream input,
            Stream output,
            int bufferSize)
        {
            using var memOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
            var mem = memOwner.Memory;
            var read = await input.ReadAsync(mem).ConfigureAwait(false);
            if (read <= 0)
                ExnModule.InvalidOp("stream read op returned 0 bytes.");
            output.Write(mem.Slice(0, read).Span);
            return read;
        }

        private static async Task<ReadOnlyMemory<byte>> ReadToCrlfCrlf(
            Stream stream,
            int bufferSize,
            Queue<ReadOnlyMemory<byte>> cache)
        {
            var ttlRead = 0;
            using var meme = new MemoryStream(bufferSize);

            while (true)
            {
                async ValueTask<int> Read()
                {
                    if (cache.Count > 0)
                    {
                        using var tmp = new MemoryStream(bufferSize);
                        while (cache.Count > 0)
                            tmp.Write(cache.Dequeue().Span);
                        return Recv(tmp, meme, bufferSize);
                    }

                    return await RecvAsync(stream, meme, bufferSize).ConfigureAwait(false);
                }

                var read = await Read().ConfigureAwait(false);
                if (read == 0)
                    ExnModule.InvalidOp("http proxy returned 0 bytes after read op.");
                ttlRead += read;

                ReadOnlyMemory<byte> memeBuffer = meme.GetBuffer();
                var data = memeBuffer.Slice(0, ttlRead);
                var indexOfCrLfCrLf = data.Span.IndexOf(CrLfCrLf.Span);
                if (indexOfCrLfCrLf <= -1)
                    continue;

                Option<ReadOnlyMemory<byte>> DataAfterCrLfCrLf()
                {
                    var sizeOfDataAfterCrLfCrLf = data.Length - indexOfCrLfCrLf - 4;
                    if (sizeOfDataAfterCrLfCrLf == 0)
                        return Option.None;
                    return (ReadOnlyMemory<byte>)memeBuffer.Slice(indexOfCrLfCrLf + 4, sizeOfDataAfterCrLfCrLf).ToArray();
                }

                var dataBeforeCrLfCrLf = memeBuffer.Slice(0, indexOfCrLfCrLf).ToArray();
                var dataAfterCrLfCrLf = DataAfterCrLfCrLf();
                if (dataAfterCrLfCrLf.IsSome)
                    cache.Enqueue(dataAfterCrLfCrLf.Value);

                return dataBeforeCrLfCrLf;
            }
        }

        private static ReadOnlyMemory<byte> HttpConnect(Proxy proxy, string host, int port)
        {
            var reqLineCnt = proxy.Credentials.IsSome ? 5 : 4;
            var request = new List<string>(reqLineCnt);
            request.AddRange(new[]
                {
                    $"CONNECT {host}:{port} HTTP/1.1",
                    "Proxy-Connection: Keep-Alive"
                }
            );

            if (proxy.Credentials.IsSome)
                request.Add($"Proxy-Authorization: {proxy.Credentials.Value}");

            request.AddRange(new[] { "", "" });
            var str = string.Join("\r\n", request);
            return ReadOnlyMemoryModule.OfString(str);
        }

        public static async Task ConnectViaHttpProxy(
            this TcpClient client,
            Proxy proxy,
            string host,
            int port,
            TimeSpan timeout)
        {
            async Task Connect()
            {
                await client.ConnectAsync(proxy.Uri.Host, proxy.Uri.Port)
                    .ConfigureAwait(false);

                var connect = HttpConnect(proxy, host, port);
                var stream = client.GetStream();
                await stream.WriteAsync(connect).ConfigureAwait(false);

                var cache = new Queue<ReadOnlyMemory<byte>>();
                var respData = await ReadToCrlfCrlf(stream, 64, cache).ConfigureAwait(false);
                var lines = StringModule.OfMemory(respData).Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0)
                    ExnModule.InvalidOp("proxy server returned malformed http response after sending CONNECT request.");
                if (lines[0].InvariantContains("connection established"))
                    return;
                ExnModule.InvalidOp($"proxy server returned {lines[0]} after sending CONNECT request.");
            }

            if (timeout == Timeout.InfiniteTimeSpan)
                await Connect().ConfigureAwait(false);
            else
                await Connect().TimeoutAfter(timeout).ConfigureAwait(false);
        }
    }

    public class BasicNetworkCredentials
    {
        public string Username { get; }
        public string Password { get; }

        public override string ToString() => $"{Username}:{Password}";

        public BasicNetworkCredentials(in string username, in string password)
        {
            Username = username;
            Password = password;
        }

        public static Option<BasicNetworkCredentials> TryParse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Option.None;

            var sp = input.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length != 2)
                return Option.None;

            var (username, pw) = (sp[0], sp[1]);
            return new BasicNetworkCredentials(username, pw);
        }
    }

    public class Proxy
    {
        public Uri Uri { get; }
        public Option<BasicNetworkCredentials> Credentials { get; }

        public Proxy(in Uri uri, in Option<BasicNetworkCredentials> credentials)
        {
            Uri = uri;
            Credentials = credentials;
        }

        public Proxy(in Uri uri, in BasicNetworkCredentials credentials) : this(uri, new Option<BasicNetworkCredentials>(credentials))
        {
        }

        public Proxy(Uri uri) : this(uri, Option.None)
        {
        }

        public override string ToString()
        {
            return Uri.ToString();
        }

        public static Option<Proxy> TryParse(string input)
        {
            if (!Uri.TryCreate(input, UriKind.Absolute, out var u)
                && !Uri.TryCreate($"http://{input}", UriKind.Absolute, out u))
            {
                return Option.None;
            }

            var cred = BasicNetworkCredentials.TryParse(u.UserInfo);
            return new Proxy(u, cred);
        }
    }
}
