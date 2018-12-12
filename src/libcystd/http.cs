using CurlThin;
using CurlThin.Enums;
using CurlThin.SafeHandles;
using LibCyStd.Net;
using LibCyStd.Seq;
using Optional;
using Optional.Unsafe;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibCyStd.Http
{
    // HTTP status codes as per RFC 2616.
    public enum HttpStatusCode
    {
        // Informational 1xx
        Continue = 100,
        SwitchingProtocols = 101,
        Processing = 102,
        EarlyHints = 103,

        // Successful 2xx
        OK = 200,
        Created = 201,
        Accepted = 202,
        NonAuthoritativeInformation = 203,
        NoContent = 204,
        ResetContent = 205,
        PartialContent = 206,
        MultiStatus = 207,
        AlreadyReported = 208,

        IMUsed = 226,

        // Redirection 3xx
        MultipleChoices = 300,
        Ambiguous = 300,
        MovedPermanently = 301,
        Moved = 301,
        Found = 302,
        Redirect = 302,
        SeeOther = 303,
        RedirectMethod = 303,
        NotModified = 304,
        UseProxy = 305,
        Unused = 306,
        TemporaryRedirect = 307,
        RedirectKeepVerb = 307,
        PermanentRedirect = 308,

        // Client Error 4xx
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NotAcceptable = 406,
        ProxyAuthenticationRequired = 407,
        RequestTimeout = 408,
        Conflict = 409,
        Gone = 410,
        LengthRequired = 411,
        PreconditionFailed = 412,
        RequestEntityTooLarge = 413,
        RequestUriTooLong = 414,
        UnsupportedMediaType = 415,
        RequestedRangeNotSatisfiable = 416,
        ExpectationFailed = 417,
        // From the discussion thread on #4382:
        // "It would be a mistake to add it to .NET now. See golang/go#21326,
        // nodejs/node#14644, requests/requests#4238 and aspnet/HttpAbstractions#915".
        // ImATeapot = 418

        MisdirectedRequest = 421,
        UnprocessableEntity = 422,
        Locked = 423,
        FailedDependency = 424,

        UpgradeRequired = 426,

        PreconditionRequired = 428,
        TooManyRequests = 429,

        RequestHeaderFieldsTooLarge = 431,

        UnavailableForLegalReasons = 451,

        // Server Error 5xx
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
        HttpVersionNotSupported = 505,
        VariantAlsoNegotiates = 506,
        InsufficientStorage = 507,
        LoopDetected = 508,

        NotExtended = 510,
        NetworkAuthenticationRequired = 511
    }

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
        public static string UrlEncode(in string input)
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
        public static string UrlEncodeSeq(in IEnumerable<(string key, string value)> sequence) =>
            string.Join("&", sequence.Select(kvp => $"{UrlEncode(kvp.key)}={UrlEncode(kvp.value)}"));

        public static List<(string, string)> EmptyHeadersList() => new List<(string, string)>();
    }

    public class Cookie : IEquatable<Cookie>
    {
        public string Name { get; }
        public string Value { get; }
        public string Path { get; }
        public string Domain { get; }
        public DateTimeOffset Expires { get; }
        public bool HttpOnly { get; }
        public bool Secure { get; }

        public Cookie(
            in string name,
            in string value,
            in string path,
            in string domain,
            in DateTimeOffset expires,
            in bool httpOnly,
            in bool secure)
        {
            Name = name;
            Value = value;
            Path = path;
            Domain = domain;
            Expires = expires;
            HttpOnly = httpOnly;
            Secure = secure;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + Name.GetHashCode();
                hash = (hash * 23) + Value.GetHashCode();
                hash = (hash * 23) + Domain.GetHashCode();
                hash = (hash * 23) + Path.GetHashCode();
                return (hash * 23) + Expires.GetHashCode();
            }
        }

        public bool Equals(Cookie other)
        {
            return Name == other.Name && Value == other.Value && Path == other.Path && Domain == other.Domain && Expires == other.Expires && Secure == other.Secure && HttpOnly == other.HttpOnly;
        }

        public override string ToString() => $"{Name}={Value}";

        private static string TryParseInp(in string input)
        {
            var tmp = input;
            if (input.InvariantStartsWith("Set-Cookie:")) return tmp.Substring(11).Trim();
            else return tmp;
        }

        private static Option<(string key, string val)> TryParseCookieNvp(in string input)
        {
            var s = input.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (s.Length >= 2)
            {
                var key = s[0];
                var val = string.Concat(s.Skip(1).Take(s.Length - 1));
                return (key, val).Some();
            }
            else
            {
                return Option.None<(string key, string val)>();
            }
        }

        private static Cookie ParseAttribs(in string name, in string value, in string defDomain, in IEnumerable<string> attributes)
        {
            var (expires, domain, path, secure, httpOnly) =
                (DateTimeOffset.MaxValue, defDomain, "/", false, false);

            foreach (var attrib in attributes)
            {
                if (attrib.InvariantStartsWith("expires"))
                {
                    var kvpOpt = TryParseCookieNvp(attrib);
                    if (kvpOpt.HasValue && DateTimeOffset.TryParse(kvpOpt.ValueOrFailure().val, out var exp))
                        expires = exp;
                }
                else if (attrib.InvariantStartsWith("domain"))
                {
                    var kvpOpt = TryParseCookieNvp(attrib);
                    if (kvpOpt.HasValue)
                        domain = kvpOpt.ValueOrFailure().val;
                }
                else if (attrib.InvariantStartsWith("path"))
                {
                    var kvpOpt = TryParseCookieNvp(attrib);
                    if (kvpOpt.HasValue)
                        path = kvpOpt.ValueOrFailure().val;
                }
                else if (attrib.InvariantStartsWith("secure"))
                {
                    secure = true;
                }
                else if (attrib.InvariantStartsWith("httponly"))
                {
                    httpOnly = true;
                }
            }

            return new Cookie(name, value, path, domain, expires, httpOnly, secure);
        }

        public static Option<Cookie> TryParse(in string input, in string defaultDomain)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Option.None<Cookie>();

            var inp = TryParseInp(input);
            var sp = inp.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var kvpStr = sp[0];
            var kvpOpt = TryParseCookieNvp(kvpStr);
            if (!kvpOpt.HasValue)
                return Option.None<Cookie>();

            var (name, value) = kvpOpt.ValueOrFailure();
            if (sp.Length >= 3)
            {
                var attribs =
                    sp
                    .Skip(1)
                    .Take(sp.Length - 1)
                    .Select(s => s.Trim());
                return Option.Some(
                    ParseAttribs(name, value, defaultDomain, attribs)
                );
            }
            else
            {
                return Option.Some(
                    new Cookie(
                        name,
                        value,
                        "/",
                        defaultDomain,
                        DateTimeOffset.MaxValue,
                        false,
                        false
                    )
                );
            }
        }
    }

    public abstract class HttpContent
    {
        public ReadOnlyMemory<byte> Content { get; }

        protected HttpContent(in ReadOnlyMemory<byte> content)
        {
            Content = content;
        }
    }

    public class ReadOnlyMemoryHttpContent : HttpContent
    {
        public ReadOnlyMemoryHttpContent(in ReadOnlyMemory<byte> content)
            : base(content) { }
    }

    public class StringHttpContent : HttpContent
    {
        private StringHttpContent(in ReadOnlyMemory<byte> content)
            : base(content) { }

        public StringHttpContent(in string str)
            : this(ReadOnlyMemoryUtils.OfString(str)) { }
    }

    /// <summary>
    /// Contains a sequence of <see cref="ValueTuples"/> with two strings that later gets url encoded and convereted to <see cref="ReadOnlyMemory"/> to send with http web request.
    /// </summary>
    public class EncodedFormValuesHttpContent : HttpContent
    {
        private EncodedFormValuesHttpContent(in ReadOnlyMemory<byte> content)
            : base(content) { }

        public EncodedFormValuesHttpContent(in IEnumerable<(string, string)> sequence)
            : this(ReadOnlyMemoryUtils.OfString(HttpUtils.UrlEncodeSeq(sequence))) { }
    }

    public static class HttpVersion
    {
        public static readonly Version Http11;
        public static readonly Version Http2;

        static HttpVersion()
        {
            Http11 = new Version("1.1");
            Http2 = new Version("2.0");
        }
    }

    /// <summary>
    /// Contains http request information
    /// </summary>
    public class HttpReq
    {
        public string HttpMethod { get; }
        public Uri Uri { get; }
        public IEnumerable<(string key, string value)> Headers { get; set; }
        public Option<HttpContent> ContentBody { get; set; }
        public Option<Proxy> Proxy { get; set; }
        public TimeSpan Timeout { get; set; }
        public IEnumerable<Cookie> Cookies { get; set; }
        public bool KeepAlive { get; set; }
        public bool AutoRedirect { get; set; }
        public bool ProxyRequired { get; set; }
        public Version ProtocolVersion { get; set; }
        //public int MaxRetries { get; set; }

        private void SetDefaults()
        {
            Headers = ReadOnlyCollectionUtils.OfSeq(new List<(string, string)>());
            ContentBody = Option.None<HttpContent>();
            Proxy = Option.None<Proxy>();
            Timeout = TimeSpan.FromSeconds(30.0);
            Cookies = new HashSet<Cookie>();
            KeepAlive = true;
            AutoRedirect = true;
            ProxyRequired = true;
            ProtocolVersion = HttpVersion.Http11;
            //MaxRetries = 3;
        }

        public HttpReq(in string method, in Uri uri)
        {
            if (!uri.Scheme.InvariantStartsWith("uri")) ExnUtils.InvalidArg($"{uri} is not a http/https uri.", nameof(uri));
            HttpMethod = method;
            Uri = uri;
            SetDefaults();
        }

#pragma warning disable RCS1139
        /// <exception cref="ArgumentException"/>
#pragma warning restore RCS1139

        public HttpReq(
            in string method,
            in string uri)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var ruri)) ExnUtils.InvalidArg($"{uri} is not a valid Uri.", nameof(uri));
            if (!ruri.Scheme.InvariantStartsWith("uri")) ExnUtils.InvalidArg($"{uri} is not a http/https uri.", nameof(uri));
            HttpMethod = method;
            Uri = ruri;
            SetDefaults();
        }

        public override string ToString()
        {
            return $"{HttpMethod} {Uri} HTTP/{ProtocolVersion}";
        }
    }

    /// <summary>
    /// Contains http response information.
    /// </summary>
    public class HttpResp
    {
        public HttpStatusCode StatusCode { get; }
        public Uri Uri { get; }
        public ReadOnlyDictionary<string, ReadOnlyCollection<string>> Headers { get; }
        public ReadOnlyCollection<Cookie> Cookies { get; }
        public ReadOnlyMemory<byte> ContentData { get; }

        public HttpResp(
            in HttpStatusCode statusCode,
            in Uri uri,
            in ReadOnlyDictionary<string, ReadOnlyCollection<string>> headers,
            in ReadOnlyCollection<Cookie> cookies,
            in ReadOnlyMemory<byte> contentData)
        {
            StatusCode = statusCode;
            Uri = uri;
            Headers = headers;
            Cookies = cookies;
            ContentData = contentData;
        }

        public string Content()
        {
            return StringUtils.OfReadOnlyMemory(ContentData);
        }
    }

    /// <summary>
    /// <see cref="HttpResp"/> utility functions.
    /// </summary>
    public static class HttpRespUtils
    {
        public static bool RespIsExpected(in HttpResp resp, in HttpStatusCode stat, in string chars) =>
            resp.StatusCode == stat && resp.Content().InvariantContains(chars);

        public static bool RespIsExpected(in HttpResp resp, in HttpStatusCode stat) =>
            resp.StatusCode == stat;

        public static void CheckExpected(in HttpResp resp, in Func<HttpResp, bool> predicate, in Action onUnexpected)
        {
            if (!predicate(resp)) onUnexpected();
        }
    }

    internal class HttpStatusInfo
    {
        public Version Version { get; }
        public HttpStatusCode StatusCode { get; }

        public HttpStatusInfo(in Version version, in HttpStatusCode statusCode)
        {
            Version = version;
            StatusCode = statusCode;
        }

        private static string VersionStr(in string input)
        {
            if (input == "2") return "2.0";
            else return input;
        }

        public override string ToString()
        {
            return $"HTTP/{Version} {StatusCode}";
        }

        public static Option<HttpStatusInfo> TryParse(in string input)
        {
            if (!input.InvariantStartsWith("http"))
                Option.None<HttpStatusInfo>();

            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 1)
                return Option.None<HttpStatusInfo>();

            var httpVer = words[0].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (httpVer.Length != 2)
                return Option.None<HttpStatusInfo>();
            var versionStr = VersionStr(httpVer[1]);
            if (!Version.TryParse(versionStr, out var version))
                return Option.None<HttpStatusInfo>();

            var statusStr = words[1];
            if (!Enum.TryParse<HttpStatusCode>(statusStr, out var httpStatCode))
                return Option.None<HttpStatusInfo>();

            return new HttpStatusInfo(version, httpStatCode).Some();
        }
    }

    internal class HttpMsgHeader
    {
        public HttpStatusInfo HttpStatusInfo { get; }
        public IReadOnlyDictionary<string, ReadOnlyCollection<string>> Headers { get; }

        public HttpMsgHeader(HttpStatusInfo httpStatusInfo, IReadOnlyDictionary<string, ReadOnlyCollection<string>> headers)
        {
            HttpStatusInfo = httpStatusInfo;
            Headers = headers;
        }

        public static Option<HttpMsgHeader> TryParse(in string input)
        {
            if (string.IsNullOrEmpty(input))
                return Option.None<HttpMsgHeader>();

            var sp = input.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length < 2)
                return Option.None<HttpMsgHeader>();

            Option<HttpMsgHeader> TryParseWithInfo(in HttpStatusInfo info, in IEnumerable<string> headers)
            {
                var h = new Dictionary<string, List<string>>(headers.Count());
                var kvps = headers.Choose(s => StringUtils.TryParseKvp(s, ':'));
                foreach ((var key, var val) in kvps)
                {
                    if (!h.ContainsKey(key))
                        h.Add(key, new List<string>(1));
                    h[key].Add(val);
                }

                var h2 = new Dictionary<string, ReadOnlyCollection<string>>(h.Count);
                foreach (var kvp in h)
                    h2.Add(kvp.Key, ReadOnlyCollectionUtils.OfSeq(kvp.Value));
                var readonlyD = ReadOnlyDictUtils.OfDict(h2);

                return new HttpMsgHeader(info, readonlyD).Some();
            }

            var statInfoOpt = HttpStatusInfo.TryParse(sp[0]);
            return statInfoOpt.Match(
                some: info => TryParseWithInfo(info, sp.Skip(1)),
                none: Option.None<HttpMsgHeader>
            );
        }
    }

    public static class HttpClient
    {
        private class HttpReqState : IDisposable
        {
            private readonly List<HttpStatusInfo> _statuses;
            private readonly Dictionary<string, List<string>> _headers;
            private Option<MemoryStream> _contentMemeStream;

            public HttpReq Req { get; }
            public CurlNative.Easy.UnsafeDataHandler HeaderDataHandler { get; }
            public CurlNative.Easy.UnsafeDataHandler ContentDataHandler { get; }
            public SafeSlistHandle HeadersSlist { get; }
            public ReadOnlyCollection<HttpStatusInfo> Statuses { get; }
            public TaskCompletionSource<HttpResp> Tcs { get; }
            public int Attempts { get; set; }

            public ReadOnlyDictionary<string, ReadOnlyCollection<string>> Headers
            {
                get
                {
                    var tmp = new Dictionary<string, ReadOnlyCollection<string>>(_headers.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in _headers)
                        tmp.Add(kvp.Key, ReadOnlyCollectionUtils.OfSeq(kvp.Value));
                    return ReadOnlyDictUtils.OfDict(tmp);
                }
            }

            public ReadOnlyMemory<byte> Content
            {
                get
                {
                    return _contentMemeStream.Match(
                        some: meme =>
                        {
                            var arr = meme.ToArray();
                            return new ReadOnlyMemory<byte>(arr);
                        },
                        none: () => new ReadOnlyMemory<byte>(new byte[0])
                    );
                }
            }

            private static SafeSlistHandle CreateSList(in HttpReq req)
            {
                var slist = SafeSlistHandle.Null;
                foreach (var (key, val) in req.Headers)
                    slist = CurlNative.Slist.Append(slist, $"{key}: {val}");

                return slist;
            }

            private static unsafe ulong Write(byte* data, in ulong size, in ulong nmemb, Stream stream)
            {
                var len = size * nmemb;
                var buffer = ArrayPool<byte>.Shared.Rent((int)len);
                try
                {
                    var dataSpan = new ReadOnlySpan<byte>(data, (int)len);
                    dataSpan.CopyTo(buffer);
#if DEBUG
                    var tmp = Encoding.UTF8.GetString(data, (int)len);
                    Console.WriteLine(tmp);
#endif
                    stream.Write(buffer, 0, (int)len);
                    return len;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, true);
                }
            }

            private unsafe ulong HandleHeaderLine(byte* data, ulong size, ulong nmemb, void* _)
            {
                var len = size * nmemb;
                var buffer = ArrayPool<byte>.Shared.Rent((int)len);
                try
                {
                    var dataSpan = new ReadOnlySpan<byte>(data, (int)len);
                    dataSpan.CopyTo(buffer);

                    string str;
                    fixed (byte* bytes = buffer)
                    {
                        str = Encoding.UTF8.GetString(bytes, (int)len);
                        if (string.IsNullOrWhiteSpace(str))
                            return len;
                    }

                    var input = str.Trim();
                    var statInfoOpt = HttpStatusInfo.TryParse(input);
                    if (statInfoOpt.HasValue)
                    {
                        _statuses.Add(statInfoOpt.ValueOrFailure());
                        return len;
                    }

                    var kvpOpt = StringUtils.TryParseKvp(input, ':');
                    if (kvpOpt.HasValue)
                    {
                        var (key, val) = kvpOpt.ValueOrFailure();
                        if (!Headers.ContainsKey(key))
                            _headers.Add(key, new List<string>(1));
                        _headers[key].Add(val);
                    }

                    return len;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, true);
                }
            }

            private static int ContentLen(in IDictionary<string, List<string>> headers)
            {
                if (headers.ContainsKey("content-length"))
                {
                    var clValues = headers["content-length"];
                    var clS = headers["content-length"][clValues.Count - 1];
                    if (int.TryParse(clS, out var cl)) return cl;
                    else return 256;
                }
                else
                {
                    return 256;
                }
            }

            private unsafe ulong HandleContent(byte* data, ulong size, ulong nmemb, void* _)
            {
                var memeStream =
                    _contentMemeStream.Match(
                        some: m => m,
                        none: () =>
                        {
                            var cl = ContentLen(_headers);
                            var meme = new MemoryStream(cl);
                            _contentMemeStream = meme.Some();
                            return meme;
                        }
                    );
                return Write(data, size, nmemb, memeStream);
            }

            public void Dispose()
            {
                _contentMemeStream.MatchSome(
                    meme => meme.Dispose()
                );
                HeadersSlist.Dispose();
            }

            public unsafe HttpReqState(in HttpReq req)
            {
                _contentMemeStream = Option.None<MemoryStream>();
                _statuses = new List<HttpStatusInfo>(1);
                _headers = new Dictionary<string, List<string>>(8, StringComparer.OrdinalIgnoreCase);

                Req = req;
                HeaderDataHandler = HandleHeaderLine;
                ContentDataHandler = HandleContent;
                HeadersSlist = CreateSList(req);
                Statuses = ReadOnlyCollectionUtils.OfSeq(_statuses);
                Tcs = new TaskCompletionSource<HttpResp>();
            }
        }

        private const int HttpVersion11 = 2;
        private const int HttpVersion20 = 4;

        private static readonly CurlMultiAgent<HttpReqState> _agent;

        private static string CurlCodeStrErr(in CURLcode code)
        {
            var ptr = CurlNative.Easy.StrError(code);
            return Marshal.PtrToStringAnsi(ptr);
        }

        static HttpClient()
        {
            var initResult = CurlNative.Init();
            if (initResult != CURLcode.OK)
                Environment.FailFast($"curl_global_init returned {initResult} ~ {CurlCodeStrErr(initResult)}");
            try
            {
                _agent = new CurlMultiAgent<HttpReqState>(100);
            }
            catch (InvalidOperationException e)
            {
                Environment.FailFast($"failed to create curlmultiagent. {e.GetType().Name} ~ {e.Message}");
            }

            CACertInfo.Init();
        }

        private static void CheckSetOpt(in CURLcode code, in HttpReq req)
        {
            if (code == CURLcode.OK)
                return;

            ExnUtils.InvalidOp(
                $"curl_easy_setopt returned {code} ~ {CurlCodeStrErr(code)} for req: {req}"
            );
        }

        private static void CheckGetInfo(in CURLcode code, in HttpReq req)
        {
            if (code == CURLcode.OK)
                return;

            ExnUtils.InvalidOp(
                $"curl_easy_getinfo returned {code} ~ {CurlCodeStrErr(code)} for req: {req}"
            );
        }

        private static void ConfigureEz(SafeEasyHandle ez, HttpReqState state)
        {
            try
            {
                var httpReq = state.Req;
                CheckSetOpt(
                    CurlNative.Easy.SetOpt(ez, CURLoption.CUSTOMREQUEST, state.Req.HttpMethod),
                    httpReq
                );
                CheckSetOpt(
                    CurlNative.Easy.SetOpt(ez, CURLoption.URL, httpReq.Uri.ToString()),
                    httpReq
                );
                CheckSetOpt(
                    CurlNative.Easy.SetOpt(ez, CURLoption.TIMEOUT_MS, (int)httpReq.Timeout.TotalMilliseconds),
                    httpReq
                );
                CheckSetOpt(
                    CurlNative.Easy.SetOpt(ez, CURLoption.HEADERFUNCTION, state.HeaderDataHandler),
                    httpReq
                );
                CheckSetOpt(
                    CurlNative.Easy.SetOpt(ez, CURLoption.WRITEFUNCTION, state.ContentDataHandler),
                    httpReq
                );

                var fi = new FileInfo("curl-ca-bundle.crt");
                CheckSetOpt(
                    CurlNative.Easy.SetOpt(ez, CURLoption.CAINFO, fi.FullName),
                    httpReq
                );

#if DEBUG
                CheckSetOpt(
                    CurlNative.Easy.SetOpt(ez, CURLoption.SSL_VERIFYPEER, 0),
                    httpReq
                );
#endif

                var acceptEncodingOpt = httpReq.Headers.TryFind(
                    valueTup =>
                    {
                        var (key, _) = valueTup;
                        return key.InvariantEquals("accept-encoding");
                    }
                );
                acceptEncodingOpt.MatchSome(
                    valueTup =>
                    {
                        var (_, acceptEncoding) = valueTup;
                        CheckSetOpt(
                            CurlNative.Easy.SetOpt(ez, CURLoption.ACCEPT_ENCODING, acceptEncoding),
                            httpReq
                        );
                    }
                );

                CheckSetOpt(
                    CurlNative.Easy.SetOpt(ez, CURLoption.HTTPHEADER, state.HeadersSlist.DangerousGetHandle()),
                    httpReq
                );

                if (httpReq.Cookies.Any())
                {
                    var cookiesStr = string.Join("; ", httpReq.Cookies.Select(c => c.ToString()));
                    CheckSetOpt(
                        CurlNative.Easy.SetOpt(ez, CURLoption.COOKIE, cookiesStr),
                        httpReq
                    );
                }

                void SetProxy(Proxy proxy)
                {
                    CheckSetOpt(
                        CurlNative.Easy.SetOpt(ez, CURLoption.PROXY, proxy.Uri.ToString()),
                        httpReq
                    );
                    proxy.Credentials.MatchSome(cred =>
                    {
                        CheckSetOpt(
                            CurlNative.Easy.SetOpt(ez, CURLoption.PROXYUSERPWD, cred.ToString()),
                            httpReq
                        );
                    });
                }

                httpReq.Proxy.MatchSome(SetProxy);

                if (httpReq.AutoRedirect)
                {
                    CheckSetOpt(
                        CurlNative.Easy.SetOpt(ez, CURLoption.FOLLOWLOCATION, 1),
                        httpReq
                    );
                    CheckSetOpt(
                        CurlNative.Easy.SetOpt(ez, CURLoption.MAXREDIRS, 10),
                        httpReq
                    );
                }

                if (httpReq.ProtocolVersion.Major == 1)
                {
                    CheckSetOpt(
                        CurlNative.Easy.SetOpt(ez, CURLoption.HTTP_VERSION, HttpVersion11),
                        httpReq
                    );
                }
                else if (httpReq.ProtocolVersion.Major == 2)
                {
                    CheckSetOpt(
                        CurlNative.Easy.SetOpt(ez, CURLoption.HTTP_VERSION, HttpVersion20),
                        httpReq
                    );
                }

                void SetContent(HttpContent content)
                {
                    var bytes = content.Content.AsArray();
                    CheckSetOpt(
                        CurlNative.Easy.SetOpt(ez, CURLoption.POSTFIELDSIZE, bytes.Length),
                        httpReq
                    );
                    CheckSetOpt(
                        CurlNative.Easy.SetOpt(ez, CURLoption.COPYPOSTFIELDS, bytes),
                        httpReq
                    );
                }

                httpReq.ContentBody.MatchSome(SetContent);
            }
            catch (InvalidOperationException e)
            {
                state.Tcs.SetException(e);
            }
        }

        private static void ParseResp(in SafeEasyHandle ez, in HttpReqState state)
        {
            if (state.Statuses.Count == 0)
                ExnUtils.InvalidOp("malformed http response received. no status info parsed.");

            var httpReq = state.Req;
            CheckGetInfo(
                CurlNative.Easy.GetInfo(ez, CURLINFO.EFFECTIVE_URL, out IntPtr ptr),
                httpReq
            );

            var uriStr = Marshal.PtrToStringAnsi(ptr);
            if (string.IsNullOrWhiteSpace(uriStr))
                ExnUtils.InvalidOp("failed to get uri from curl easy.");
            if (!Uri.TryCreate(uriStr, UriKind.Absolute, out var uri))
                ExnUtils.InvalidOp("failed to parse uri from curl easy.");

            var cookz = new List<Cookie>();
            if (state.Headers.ContainsKey("set-cookie"))
            {
                foreach (var cook in state.Headers["set-cookie"])
                {
                    var cookOpt = Cookie.TryParse(cook, $".{uri.Authority}");
                    cookOpt.MatchSome(cookz.Add);
                }
            }

            var resp = new HttpResp(
                state.Statuses[state.Statuses.Count - 1].StatusCode,
                uri,
                state.Headers,
                ReadOnlyCollectionUtils.OfSeq(cookz),
                state.Content
            );
            state.Tcs.SetResult(resp);
        }

        private static ReqOpCompletedAction HandleResp(SafeEasyHandle ez, HttpReqState state, CURLcode result)
        {
            using (_ = state)
            {
                try
                {
                    if (result == CURLcode.OK)
                    {
                        ParseResp(ez, state);
                    }
                    else if (result == CURLcode.OPERATION_TIMEDOUT)
                    {
                        ExnUtils.Timeout(
                            $"timeout error occured after trying to retrieve response for request {state.Req}. {result} ~ {CurlCodeStrErr(result)}."
                        );
                    }
                    else
                    {
                        ExnUtils.InvalidOp(
                            $"Error occured trying to retrieve response for request {state.Req}. {result} ~ {CurlCodeStrErr(result)}."
                        );
                    }
                }
                catch (Exception e) when (e is InvalidOperationException || e is TimeoutException)
                {
                    state.Tcs.SetException(e);
                }

                return ReqOpCompletedAction.ResetHandleAndNext;
            }

            //if (state.Attempts++ >= state.Req.MaxRetries)
            //{
            //    return ReqOpCompletedAction.ReuseHandleAndRetry;
            //}
            //else
            //{
            //    state.Dispose();
            //    return ReqOpCompletedAction.ResetHandleAndNext;
            //}
        }

        public static Task<HttpResp> RetrRespAsync(in HttpReq req)
        {
            if (req.ProxyRequired && !req.Proxy.HasValue)
                throw new ArgumentException("Proxy is required for this request.", nameof(req));

            var state = new HttpReqState(req);
            var reqCtx = new ReqCtx<HttpReqState>(
                state,
                ConfigureEz,
                HandleResp
            );
            _agent.ExecReq(reqCtx);
            return state.Tcs.Task;
        }
    }
}