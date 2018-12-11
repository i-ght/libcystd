using Optional;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibCyStd
{
    /// <summary>
    /// <see cref="Http"/> utility functions.
    /// </summary>
    public static class HttpUtils
    {
        private const string UnreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        /// <summary>
        /// Url encodes input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UrlEncode(string input)
        {
            var sb = new StringBuilder(input.Length + 50);
            foreach (var ch in input)
            {
                if (UnreservedChars.IndexOf(ch) == -1) sb.Append("%").AppendFormat("{0:X2}", (int)ch);
                else sb.Append(ch);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Url encodes sequence of <see cref="ValueTuple"/>s;.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static string UrlEncodeSeq(IEnumerable<(string key, string value)> sequence) =>
            string.Join("&", sequence.Select(kvp => $"{UrlEncode(kvp.key)}={UrlEncode(kvp.value)}"));

        public static List<(string, string)> NewList() => new List<(string, string)>();
    }

    /// <summary>
    /// <see cref="CookieContainer"/> utility functions.
    /// </summary>
    public static class CookieContainerUtils
    {
        /// <summary>
        /// Creates a cookie container from a <see cref="Cookie"/> sequence.
        /// </summary>
        /// <param name="cookies"></param>
        /// <returns></returns>
        public static CookieContainer OfSeq(IEnumerable<Cookie> cookies)
        {
            var cont = new CookieContainer();
            foreach (var c in cookies) cont.Add(c);
            return cont;
        }
    }

#if NETCOREAPP2_1

    /// <summary>
    /// Contains a sequence of <see cref="ValueTuples"/> with two strings that later gets url encoded and convereted to <see cref="ReadOnlyMemory"/> to send with http web request.
    /// </summary>
    public class EncodedFormValuesHttpContent : HttpContent
    {
        private readonly ReadOnlyMemory<byte> _content;

        /// <summary>
        /// Creates a new MemoryHttpContentBody that contains a sequence of <see cref="ValueTuples"/> with two strings that later gets url encoded and convereted to <see cref="ReadOnlyMemory"/> to send with http web request.
        /// </summary>
        public EncodedFormValuesHttpContent(
            IEnumerable<(string, string)> sequence)
        {
            _content = ReadOnlyMemoryUtils.OfString(HttpUtils.UrlEncodeSeq(sequence));
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await stream.WriteAsync(_content).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }
    }

#elif NETSTANDARD2_0

    public abstract class HttpMethod
    {
        //cyclic FeelsBadMan

        public static readonly HttpHead Head;
        public static readonly HttpGet Get;
        public static readonly HttpPost Post;
        public static readonly HttpPut Put;
        public static readonly HttpDelete Delete;

        static HttpMethod()
        {
            Head = new HttpHead();
            Get = new HttpGet();
            Post = new HttpPost();
            Put = new HttpPut();
            Delete = new HttpDelete();
        }
    }

    public class HttpHead : HttpMethod
    {
        public override string ToString() => "HEAD";
    }

    public class HttpGet : HttpMethod
    {
        private const string s = "GET";

        public override string ToString() => s;
    }

    public class HttpPost : HttpMethod
    {
        private const string s = "POST";

        public override string ToString() => s;
    }

    public class HttpPut : HttpMethod
    {
        private const string s = "PUT";

        public override string ToString() => s;
    }

    public class HttpDelete
    {
        private const string s = "DELETE";

        public override string ToString() => s;
    }

    public abstract class HttpContent
    {
        public ReadOnlyMemory<byte> Content { get; }

        protected HttpContent(ReadOnlyMemory<byte> content)
        {
            Content = content;
        }
    }

    public class ReadOnlyMemoryHttpContent : HttpContent
    {
        public ReadOnlyMemoryHttpContent(ReadOnlyMemory<byte> content)
            : base(content) { }
    }

    public class StringHttpContent : HttpContent
    {
        private StringHttpContent(ReadOnlyMemory<byte> content)
            : base(content) { }

        public StringHttpContent(string str)
            : this(ReadOnlyMemoryUtils.OfString(str)) { }
    }

    /// <summary>
    /// Contains a sequence of <see cref="ValueTuples"/> with two strings that later gets url encoded and convereted to <see cref="ReadOnlyMemory"/> to send with http web request.
    /// </summary>
    public class EncodedFormValuesHttpContent : HttpContent
    {
        private EncodedFormValuesHttpContent(ReadOnlyMemory<byte> content)
            : base(content) { }

        public EncodedFormValuesHttpContent(IEnumerable<(string, string)> sequence)
            : this(ReadOnlyMemoryUtils.OfString(HttpUtils.UrlEncodeSeq(sequence))) { }
    }

#endif

    /// <summary>
    /// Cookie equality comparer to avoid duplicate cookies in <see cref="Cookie"/> <see cref="HashSet{T}"/>s.
    /// </summary>
    public class CookieEqualityComparer : IEqualityComparer<Cookie>
    {
        public bool Equals(Cookie x, Cookie y) =>
            x.Name == y.Name && x.Value == y.Value && x.Domain == y.Domain && x.Path == y.Path && x.Expires == y.Expires;

        public int GetHashCode(Cookie obj)
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + obj.Name.GetHashCode();
                hash = (hash * 23) + obj.Value.GetHashCode();
                hash = (hash * 23) + obj.Domain.GetHashCode();
                hash = (hash * 23) + obj.Path.GetHashCode();
                return (hash * 23) + obj.Expires.GetHashCode();
            }
        }
    }

    /// <summary>
    /// Contains http request information
    /// </summary>
    public class HttpReq
    {
        public HttpMethod HttpMethod { get; }
        public Uri Uri { get; }
        public IEnumerable<(string key, string value)> Headers { get; set; }
        public Option<HttpContent> ContentBody { get; set; }
        public Option<WebProxy> Proxy { get; set; }
        public DecompressionMethods AcceptEncoding { get; set; }
        public TimeSpan Timeout { get; set; }
        public IEnumerable<Cookie> Cookies { get; set; }
        public bool KeepAlive { get; set; }
        public bool AutoRedirect { get; set; }
        public bool ProxyRequired { get; set; }
        public Version ProtocolVersion { get; set; }

        private void SetDefaults()
        {
            Headers = ReadOnlyCollectionUtils.OfSeq(new List<(string, string)>());
            ContentBody = Option.None<HttpContent>();
            Proxy = Option.None<WebProxy>();
            AcceptEncoding = DecompressionMethods.None;
            Timeout = TimeSpan.FromSeconds(30.0);
            Cookies = new HashSet<Cookie>(new CookieEqualityComparer());
            KeepAlive = true;
            AutoRedirect = true;
            ProxyRequired = true;
            ProtocolVersion = HttpVersion.Version11;
        }

        public HttpReq(HttpMethod method, Uri uri)
        {
            HttpMethod = method;
            Uri = uri;
            SetDefaults();
        }

#pragma warning disable RCS1139
        /// <exception cref="ArgumentException"/>
#pragma warning restore RCS1139

        public HttpReq(
            HttpMethod method,
            string uri)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var ruri)) ExnUtils.InvalidArg($"{uri} is not a valid Uri.", uri);
            HttpMethod = method;
            Uri = ruri;
            SetDefaults();
        }
    }

    /// <summary>
    /// Contains http response information.
    /// </summary>
    public class HttpResp
    {
        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }
        public Uri Uri { get; }
        public IReadOnlyDictionary<string, ReadOnlyCollection<string>> Headers { get; }
        public ReadOnlyCollection<Cookie> Cookies { get; }
        public ReadOnlyMemory<byte> ContentData { get; }
        public string ContentBody { get; }

        public HttpResp(
            HttpStatusCode statusCode,
            string statusDescription,
            Uri uri,
            IReadOnlyDictionary<string, ReadOnlyCollection<string>> headers,
            ReadOnlyCollection<Cookie> cookies,
            ReadOnlyMemory<byte> contentData,
            string contentBody)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
            Uri = uri;
            Headers = headers;
            Cookies = cookies;
            ContentData = contentData;
            ContentBody = contentBody;
        }
    }

    /// <summary>
    /// <see cref="HttpResp"/> utility functions.
    /// </summary>
    public static class HttpRespUtils
    {
        public static bool RespIsExpected(HttpResp resp, HttpStatusCode stat, string chars) =>
            resp.StatusCode == stat && resp.ContentBody.InvariantContains(chars);

        public static bool RespIsExpected(HttpResp resp, HttpStatusCode stat) =>
            resp.StatusCode == stat;

        public static void CheckExpected(HttpResp resp, Func<HttpResp, bool> predicate, Action onUnexpected)
        {
            if (!predicate(resp)) onUnexpected();
        }
    }

    public static class Http
    {
        static Http()
        {
            ServicePointManager.DefaultConnectionLimit = 123;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = false;
        }

#if NETCOREAPP2_1

        private static void SetupHandler(HttpReq req, SocketsHttpHandler handler)
        {
            req.Proxy.MatchSome(proxy => handler.Proxy = proxy);
            handler.CookieContainer = CookieContainerUtils.OfSeq(req.Cookies);
            handler.AutomaticDecompression = req.AcceptEncoding;
            handler.ConnectTimeout = req.Timeout;
            handler.AllowAutoRedirect = req.AutoRedirect;
#if DEBUG
            handler.SslOptions.RemoteCertificateValidationCallback = (_, __, ___, ____) => true;
#endif
        }

        private static bool TryAddHeader(string key, string value, HttpRequestMessage reqMsg)
        {
            if (key.InvariantEquals("CoNtEnT-TypE"))
            {
                if (reqMsg.Content == null) reqMsg.Content = new ReadOnlyMemoryContent(ReadOnlyMemory<byte>.Empty);
                return reqMsg.Content.Headers.TryAddWithoutValidation(key, value);
            }
            else
            {
                return reqMsg.Headers.TryAddWithoutValidation(key, value);
            }
        }

        private static void AddHeader(string key, string value, HttpRequestMessage reqMsg)
        {
            if (!TryAddHeader(key, value, reqMsg)) ExnUtils.InvalidOp($"Failed to add header '{key}: {value}'");
        }

        private static void AddHeaders(HttpReq req, HttpRequestMessage reqMsg)
        {
            foreach (var (key, value) in req.Headers) AddHeader(key, value, reqMsg);
        }

        private static int BufferSize(HttpResponseMessage resp)
        {
            var cl = resp.Content.Headers.ContentLength;
            if (!cl.HasValue) return 8192;
            else if (cl.Value < 0L) return 8192;
            else if (cl.Value <= 8192) return (int)cl.Value;
            else return 8192;
        }

        private static async Task<ReadOnlyMemory<byte>> RetrieveRespContent(Stream stream, int bufferSize)
        {
            using (var strem = new MemoryStream(bufferSize))
            {
                await stream.CopyToAsync(strem, bufferSize).ConfigureAwait(false);
                return ReadOnlyMemoryUtils.OfSeq(strem.ToArray());
            }
        }

        private static IReadOnlyDictionary<string, ReadOnlyCollection<string>> RespHeaders(HttpResponseMessage respMsg)
        {
            // C# list initialization.  Really makes you think.
            var headers = ReadOnlyCollectionUtils.OfSeq(new List<IEnumerable<KeyValuePair<string, IEnumerable<string>>>>
            {
                respMsg.Headers,
                respMsg.Content.Headers
            });
            var list = new List<(string, ReadOnlyCollection<string>)>(respMsg.Headers.Len() + respMsg.Content.Headers.Len());
            foreach (var kvps in headers)
                foreach (var kvp in kvps) list.Add((kvp.Key, ReadOnlyCollectionUtils.OfSeq(kvp.Value)));
            return ReadOnlyDictUtils.OfSeq(list, StringComparer.OrdinalIgnoreCase);
        }

        private static ReadOnlyCollection<Cookie> RespCookies(
            SocketsHttpHandler handler, HttpRequestMessage reqMsg, HttpReq req)
        {
            // HttpResponseMessage is really cool in that it doesn't have a good way to access a response's Set-Cookie headers as a Cookie sequence.
            HashSet<Cookie> Set() => new HashSet<Cookie>(new CookieEqualityComparer());
            var (reqCookies, respCookies, cookies) = (Set(), Set(), handler.CookieContainer.GetCookies(reqMsg.RequestUri).Cast<Cookie>());
            foreach (var c in req.Cookies) reqCookies.Add(c);
            foreach (var c in cookies) if (!reqCookies.Contains(c)) respCookies.Add(c);
            return ReadOnlyCollectionUtils.OfSeq(respCookies);
        }

        /// <summary>
        /// Retrieves the <see cref="HttpResp"/> for the specified <see cref="HttpReq"/>.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="HttpResp"/></returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="HttpRequestException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        public static async Task<HttpResp> RetrRespAsync(HttpReq req, CancellationToken cancellationToken)
        {
            try
            {
                if (req.ProxyRequired && !req.Proxy.HasValue)
                    ExnUtils.InvalidArg("Proxy is requred to have a value to send this request because ProxyRequired property is true.", nameof(req));

                using (var handler = new SocketsHttpHandler())
                {
                    SetupHandler(req, handler);
                    using (var reqMsg = new HttpRequestMessage(req.HttpMethod, req.Uri) { Version = req.ProtocolVersion })
                    {
                        req.ContentBody.MatchSome(c =>
                        {
                            reqMsg.Content = c;
                            reqMsg.Content.Headers.ContentType = null; //System.Net.Http.HttpContent automatically adds Content-Type for you. bothersome.
                        });
                        using (var client = new HttpClient(handler))
                        {
                            client.Timeout = req.Timeout;
                            AddHeaders(req, reqMsg);
                            using (var respMsg = await client.SendAsync(reqMsg, cancellationToken).ConfigureAwait(false))
                            using (var stream = await respMsg.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            {
                                var (memoriez, headers, cookies) =
                                    (await RetrieveRespContent(stream, BufferSize(respMsg)).ConfigureAwait(false), RespHeaders(respMsg), RespCookies(handler, reqMsg, req));
                                return new HttpResp(
                                    respMsg.StatusCode,
                                    respMsg.ReasonPhrase,
                                    respMsg.RequestMessage.RequestUri,
                                    headers,
                                    cookies,
                                    memoriez,
                                    StringUtils.OfReadOnlyMemory(memoriez));
                            }
                        }
                    }
                }
            }
            finally
            {
                req.ContentBody.MatchSome(c => c.Dispose());
            }
        }

        /// <summary>
        /// Retrieves the <see cref="HttpResp"/> for the specified <see cref="HttpReq"/>.
        /// </summary>
        /// <param name="req"></param>
        /// <returns><see cref="HttpResp"/></returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="HttpRequestException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="TimeoutException"/>
        public static async Task<HttpResp> RetrRespAsync(HttpReq req)
        {
            return await RetrRespAsync(req, CancellationToken.None).ConfigureAwait(false);
        }

        // .net framework should ref netstd2.0 version. core app should ref netcoreapp2.1 version.
#elif NETSTANDARD2_0

        private static void AddHeaders(HttpWebRequest httpWebReq, HttpReq req)
        {
            void ClHeader(string value)
            {
                if (!long.TryParse(value, out var cl)) ExnUtils.InvalidOp("failed to parse content-length header value to long.");
                httpWebReq.ContentLength = cl;
            }

            foreach (var (key, value) in req.Headers)
            {
                if (key.InvariantEquals("accept")) httpWebReq.Accept = value;
                else if (key.InvariantEquals("content-type")) httpWebReq.ContentType = value;
                else if (key.InvariantEquals("user-agent")) httpWebReq.UserAgent = value;
                else if (key.InvariantEquals("referer")) httpWebReq.Referer = value;
                else if (key.InvariantEquals("content-length")) ClHeader(value);
                else httpWebReq.Headers.Add(key, value);
            }
        }

        private static HttpWebRequest HttpWebReq(HttpReq req)
        {
            var httpWebReq = WebRequest.CreateHttp(req.Uri);
            httpWebReq.Method = req.HttpMethod.ToString();
            httpWebReq.KeepAlive = req.KeepAlive;
            httpWebReq.ProtocolVersion = req.ProtocolVersion;
            httpWebReq.Timeout = (int)req.Timeout.TotalMilliseconds;
            httpWebReq.ReadWriteTimeout = (int)req.Timeout.TotalMilliseconds;
            httpWebReq.AllowAutoRedirect = req.AutoRedirect;
            httpWebReq.AutomaticDecompression = req.AcceptEncoding;
            httpWebReq.CookieContainer = CookieContainerUtils.OfSeq(req.Cookies);
#if DEBUG
            httpWebReq.ServerCertificateValidationCallback = (_, __, ___, ____) => true;
#endif

            req.Proxy.MatchSome(p => httpWebReq.Proxy = p);

            AddHeaders(httpWebReq, req);

            return httpWebReq;
        }

        private static int BufferSize(HttpWebResponse httpWebResp)
        {
            var cl = httpWebResp.ContentLength;
            if (cl < 0L) return 8192;
            else if (cl <= 8192) return (int)cl;
            else return 8192;
        }

        private static async Task<ReadOnlyMemory<byte>> ContentData(Stream respStream, int bufferSize)
        {
            if (bufferSize == 0) return new byte[0];
            using (var memStrem = new MemoryStream(bufferSize))
            {
                await respStream.CopyToAsync(memStrem, bufferSize).ConfigureAwait(false);
                return ReadOnlyMemoryUtils.OfSeq(memStrem.ToArray());
            }
        }

        private static async Task<HttpResp> Resp(HttpWebResponse httpWebResp, CancellationToken cancellationToken)
        {
            using (var respStream = httpWebResp.GetResponseStream())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var content = await ContentData(respStream, BufferSize(httpWebResp)).ConfigureAwait(false);

                return new HttpResp(
                    httpWebResp.StatusCode,
                    httpWebResp.StatusDescription,
                    httpWebResp.ResponseUri,
                    ReadOnlyDictUtils.OfSeq(
                        httpWebResp.Headers.AllKeys
                        .Select(k => (k, ReadOnlyCollectionUtils.OfSeq(httpWebResp.Headers.GetValues(k)))), StringComparer.OrdinalIgnoreCase),
                    ReadOnlyCollectionUtils.OfSeq(httpWebResp.Cookies.Cast<Cookie>()),
                    content,
                    StringUtils.OfReadOnlyMemory(content)
                );
            }
        }

        private static async Task<HttpResp> Req(HttpReq req, HttpWebRequest httpWebReq, CancellationToken cancellationToken)
        {
            Action cancel = cancellationToken.ThrowIfCancellationRequested;

            async Task Write(HttpContent content)
            {
                cancel();
                using (var reqStream = await httpWebReq.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    cancel();
                    await reqStream.WriteAsync(content.Content).ConfigureAwait(false);
                }
            }

            cancel();
            await req.ContentBody.MatchSomeAsync(Write).ConfigureAwait(false);

            cancel();
            using (var httpWebResp = (HttpWebResponse)await httpWebReq.GetResponseAsync().ConfigureAwait(false))
            {
                cancel();
                return await Resp(httpWebResp, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<HttpResp> RetrRespAsync(HttpReq req, CancellationToken cancellationToken)
        {
            if (req.ProxyRequired && !req.Proxy.HasValue)
                ExnUtils.InvalidArg("Proxy is requred to have a value to send this request because ProxyRequired property is true.", nameof(req));

            var httpWebReq = HttpWebReq(req);
            var retr =
                req.Timeout == Timeout.InfiniteTimeSpan
                ? Req(req, httpWebReq, cancellationToken)
                : Req(req, httpWebReq, cancellationToken).TimeoutAfter(req.Timeout);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await retr.ConfigureAwait(false);
            }
            catch (WebException e) when (e.Response != null)
            {
                using (var httpWebResp = (HttpWebResponse)e.Response)
                    return await Resp(httpWebResp, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<HttpResp> RetrRespAsync(HttpReq req)
        {
            return await RetrRespAsync(req, CancellationToken.None).ConfigureAwait(false);
        }

#endif
    }
}