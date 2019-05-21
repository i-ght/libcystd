using LibCyStd.LibCurl;
using LibCyStd.LibOneOf.Types;
using LibCyStd.Net;
using LibCyStd.Seq;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
#pragma warning disable RCS1234 // Duplicate enum value.
        Ambiguous = 300,
#pragma warning restore RCS1234 // Duplicate enum value.
        MovedPermanently = 301,
#pragma warning disable RCS1234 // Duplicate enum value.
        Moved = 301,
#pragma warning restore RCS1234 // Duplicate enum value.
        Found = 302,
#pragma warning disable RCS1234 // Duplicate enum value.
        Redirect = 302,
#pragma warning restore RCS1234 // Duplicate enum value.
        SeeOther = 303,
#pragma warning disable RCS1234 // Duplicate enum value.
        RedirectMethod = 303,
#pragma warning restore RCS1234 // Duplicate enum value.
        NotModified = 304,
        UseProxy = 305,
        Unused = 306,
        TemporaryRedirect = 307,
#pragma warning disable RCS1234 // Duplicate enum value.
        RedirectKeepVerb = 307,
#pragma warning restore RCS1234 // Duplicate enum value.
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
        ImATeapot = 418,

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
    /// <see cref="LibCyStd.Http"/> utility functions.
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
            void Append(char ch)
            {
                if (UnreservedChars.IndexOf(ch) == -1) sb.Append("%").AppendFormat("{0:X2}", (int)ch);
                else sb.Append(ch);
            }
            foreach (var ch in input)
                Append(ch);
            return sb.ToString();
        }

        public static string UrlEncodeKvp((string key, string val) kvp)
        {
            var (key, val) = kvp;
            return $"{UrlEncode(key)}={UrlEncode(val)}";
        }


        /// <summary>
        /// Url encodes sequence of <see cref="ValueTuple"/>s;.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static string UrlEncodeSeq(IEnumerable<(string key, string value)> sequence) =>
            string.Join("&", sequence.Select(UrlEncodeKvp));

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
            string name,
            string value,
            string path,
            string domain,
            DateTimeOffset expires,
            bool httpOnly,
            bool secure)
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

        private static string TryParseInp(string input)
        {
            var tmp = input;
            return input.InvariantStartsWith("Set-Cookie:") ? tmp.Substring(11).Trim() : tmp;
        }

        private static Option<(string key, string val)> TryParseCookieNvp(string input)
        {
            var s = input.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (s.Length >= 2)
            {
                var key = s[0];
                var val = string.Concat(s.Skip(1).Take(s.Length - 1));
                return (key, val);
            }
            else
            {
                return Option.None;
            }
        }

        private static Cookie ParseAttribs(string name, string value, string defDomain, IEnumerable<string> attributes)
        {
            var (expires, domain, path, secure, httpOnly) =
                (DateTimeOffset.MaxValue, defDomain, "/", false, false);

            foreach (var attrib in attributes)
            {
                if (attrib.InvariantStartsWith("expires"))
                {
                    var kvpOpt = TryParseCookieNvp(attrib);
                    if (kvpOpt.IsSome && DateTimeOffset.TryParse(kvpOpt.Value.val, out var exp))
                        expires = exp;
                }
                else if (attrib.InvariantStartsWith("domain"))
                {
                    var kvpOpt = TryParseCookieNvp(attrib);
                    if (kvpOpt.IsSome)
                        domain = kvpOpt.Value.val;
                }
                else if (attrib.InvariantStartsWith("path"))
                {
                    var kvpOpt = TryParseCookieNvp(attrib);
                    if (kvpOpt.IsSome)
                        path = kvpOpt.Value.val;
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

        public static Option<Cookie> TryParse(string input, string defaultDomain)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Option.None;

            var inp = TryParseInp(input);
            var sp = inp.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var kvpStr = sp[0];
            var kvpOpt = TryParseCookieNvp(kvpStr);
            if (!kvpOpt.IsSome)
                return Option.None;

            var (name, value) = kvpOpt.Value;
            if (sp.Length >= 3)
            {
                var attribs =
                    sp
                    .Skip(1)
                    .Take(sp.Length - 1)
                    .Select(StringModule.Trim);
                return ParseAttribs(name, value, defaultDomain, attribs);
            }
            else
            {
                return new Cookie(
                    name,
                    value,
                    "/",
                    defaultDomain,
                    DateTimeOffset.MaxValue,
                    false,
                    false
                );
            }
        }
    }

    public abstract class HttpContent : IDisposable
    {
        private bool _disposed;
        private readonly IMemoryOwner<byte> _memOwner;

        public ReadOnlyMemory<byte> Content { get; }

        protected HttpContent(in ReadOnlySpan<byte> content)
        {
            _memOwner = MemoryPool<byte>.Shared.Rent(content.Length);
            content.CopyTo(_memOwner.Memory.Span);
            Content = _memOwner.Memory.Slice(0, content.Length);
        }

        public override string ToString() => StringModule.OfMemory(Content);

        public void Dispose()
        {
            if (_disposed) return;
            _memOwner.Dispose();
            _disposed = true;
        }
    }

    public class ReadOnlyMemoryHttpContent : HttpContent
    {
        public ReadOnlyMemoryHttpContent(in ReadOnlySpan<byte> content) : base(content) { }
        public override string ToString() => Content.ToString();
    }

    public class StringHttpContent : HttpContent
    {
        private readonly string _str;
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        private StringHttpContent(in ReadOnlySpan<byte> content) : base(content) { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        public StringHttpContent(string str) : this(ReadOnlySpanModule.OfString(str)) { _str = str; }
        public override string ToString() => _str;
    }

    /// <summary>
    /// Contains a sequence of <see cref="ValueTuples"/> with two strings that later gets url encoded and convereted to <see cref="ReadOnlyMemory"/> to send with http web request.
    /// </summary>
    public class EncodedFormValuesHttpContent : HttpContent
    {
        private EncodedFormValuesHttpContent(in ReadOnlySpan<byte> content) : base(content) { }
        public EncodedFormValuesHttpContent(IEnumerable<(string, string)> sequence) : this(ReadOnlySpanModule.OfString(HttpUtils.UrlEncodeSeq(sequence))) { }
    }

    public static class HttpVersion
    {
        public static Version Http11 { get; }
        public static Version Http2 { get; }

        static HttpVersion()
        {
            Http11 = new Version("1.1");
            Http2 = new Version("2.0");
        }
    }

    /// <summary>
    /// Contains http request information
    /// </summary>
    [DebuggerDisplay("{HttpMethod,nq} {Uri.ToString(),nq} HTTP/{ProtocolVersion.ToString(),nq}")]
    public class HttpReq
    {
        public string HttpMethod { get; }
        public Uri Uri { get; }
        public IEnumerable<(string key, string val)> Headers { get; set; }
        public Option<HttpContent> ContentBody { get; set; }
        public Option<Proxy> Proxy { get; set; }
        public TimeSpan Timeout { get; set; }
        public IEnumerable<Cookie> Cookies { get; set; }
        public bool AutoRedirect { get; set; }
        public bool ProxyRequired { get; set; }
        public Version ProtocolVersion { get; set; }
        public bool KeepAlive { get; set; }
        public int MaxRetries { get; set; }

        private void InitDef()
        {
            Headers = SeqModule.Empty<(string key, string val)>();
            ContentBody = Option.None;
            Proxy = Option.None;
            Timeout = TimeSpan.FromSeconds(30.0);
            Cookies = SeqModule.Empty<Cookie>();
            AutoRedirect = true;
            ProxyRequired = true;
            ProtocolVersion = HttpVersion.Http11;
            KeepAlive = true;
        }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public HttpReq(string method, Uri uri)
        {
            if (!uri.Scheme.InvariantStartsWith("http"))
                ExnModule.InvalidArg($"{uri} is not a http/https uri.", nameof(uri));

            HttpMethod = method;
            Uri = uri;
            InitDef();
        }

#pragma warning disable RCS1139
        /// <exception cref="ArgumentException"/>
#pragma warning restore RCS1139

        public HttpReq(
            string method,
            string uri)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var ruri))
                ExnModule.InvalidArg($"{uri} is not a valid Uri.", nameof(uri));
            if (!ruri.Scheme.InvariantStartsWith("http"))
                ExnModule.InvalidArg($"{uri} is not a http/https uri.", nameof(uri));
            HttpMethod = method;
            Uri = ruri;
            InitDef();
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        private static string Http1ReqToStr(HttpReq req)
        {
            var sb = new StringBuilder(512);
            sb.Append(req.HttpMethod)
                .Append(" ")
                .Append(req.Uri.PathAndQuery)
                .Append(" HTTP/")
                .Append(req.ProtocolVersion)
                .Append("Host: ")
                .Append(req.Uri.Authority);

            foreach (var (key, val) in req.Headers)
                sb.Append(key).Append(": ").AppendLine(val);

            if (req.Cookies.Any())
            {
                var cookz = string.Join("; ", req.Cookies);
                sb.Append("Cookie: ").AppendLine(cookz);
            }

            if (req.ContentBody.IsSome)
                sb.AppendLine().Append(req.ContentBody.Value.ToString());

            return sb.AppendLine().ToString();
        }

        private static string Http2ReqToStr(HttpReq req)
        {
            var sb = new StringBuilder(512);
            sb.Append(":method: ").AppendLine(req.HttpMethod);
            sb.Append(":path: ").AppendLine(req.Uri.PathAndQuery);
            sb.Append(":scheme: ").AppendLine(req.Uri.Scheme);
            sb.Append(":authority: ").AppendLine(req.Uri.Authority);

            foreach (var (key, val) in req.Headers)
                sb.Append(key).Append(": ").AppendLine(val);

            if (req.Cookies.Any())
                sb.Append("cookie: ").AppendLine(string.Join("; ", req.Cookies));

            if (req.ContentBody.IsSome)
                sb.AppendLine().Append(req.ContentBody.Value.ToString());

            return sb.AppendLine().ToString();
        }

        public override string ToString()
        {
            if (ProtocolVersion.Major == 2)
                return Http2ReqToStr(this);

            return Http1ReqToStr(this);
        }
    }

    /// <summary>
    /// Contains http response information.
    /// </summary>
    public class HttpResp : IDisposable
    {
        private bool _disposed;
        private Option<IMemoryOwner<char>> _strMemOwner;
        private Option<string> _s;
        private readonly IMemoryOwner<byte> _memOwner;

        public HttpStatusCode StatusCode { get; }
        public Uri Uri { get; }
        public ReadOnlyDictionary<string, ReadOnlyCollection<string>> Headers { get; }
        public ReadOnlyCollection<Cookie> Cookies { get; }
        public ReadOnlyMemory<byte> ContentData { get; }

        public unsafe string Content
        {
            get
            {
                if (ContentData.Length == 0)
                    return "";

                if (_s.IsSome)
                    return _s.Value;

                _strMemOwner = Option.Some(MemoryPool<char>.Shared.Rent(ContentData.Length));
                var s = StringModule.OfMemory(ContentData);
                s.AsMemory().CopyTo(_strMemOwner.Value.Memory);

                fixed (char* ptr = _strMemOwner.Value.Memory.Span)
                    _s = new string(ptr, 0, ContentData.Length);

                return _s.Value;
            }
        }

        public HttpResp(
            HttpStatusCode statusCode,
            Uri uri,
            ReadOnlyDictionary<string, ReadOnlyCollection<string>> headers,
            ReadOnlyCollection<Cookie> cookies,
            in ReadOnlySpan<byte> contentData)
        {
            StatusCode = statusCode;
            Uri = uri;
            Headers = headers;
            Cookies = cookies;

            _s = Option.None;
            _memOwner = MemoryPool<byte>.Shared.Rent(contentData.Length);
            contentData.CopyTo(_memOwner.Memory.Span);
            _strMemOwner = Option.None;
            ContentData = _memOwner.Memory.Slice(0, contentData.Length);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_strMemOwner.IsSome)
                _strMemOwner.Value.Dispose();
            _memOwner.Dispose();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(StatusCode).Append(" - ").Append(Uri).AppendLine();
            foreach (var kvp in Headers)
                sb.Append(kvp.Key).Append(": ").Append(string.Join(", ", kvp.Value));

            return sb.ToString();
        }
    }

    /// <summary>
    /// <see cref="HttpResp"/> utility functions.
    /// </summary>
    public static class HttpRespModule
    {
        public static bool RespIsExpected(HttpResp resp, HttpStatusCode stat, string chars) =>
            resp.StatusCode == stat && resp.Content.InvariantContains(chars);

        public static bool RespIsExpected(HttpResp resp, HttpStatusCode stat) =>
            resp.StatusCode == stat;

        public static void CheckExpect(HttpResp resp, Func<HttpResp, bool> predicate, Action onUnexpected)
        {
            if (!predicate(resp)) onUnexpected();
        }
    }

#if DEBUG
    public
#else
    internal
#endif
        class HttpStatusInfo
    {
        public Version Version { get; }
        public HttpStatusCode StatusCode { get; }

        public HttpStatusInfo(Version version, HttpStatusCode statusCode)
        {
            Version = version;
            StatusCode = statusCode;
        }

        private static string VersionStr(string input)
        {
            return input == "2" ? "2.0" : input;
        }

        public override string ToString()
        {
            return $"HTTP/{Version} {StatusCode}";
        }

        public static Option<HttpStatusInfo> TryParse(string input)
        {
            if (!input.InvariantStartsWith("http"))
                return Option.None;

            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 1)
                return Option.None;

            var httpVer = words[0].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (httpVer.Length != 2)
                return Option.None;
            var versionStr = VersionStr(httpVer[1]);
            if (!Version.TryParse(versionStr, out var version))
                return Option.None;

            var statusStr = words[1];
            return !Enum.TryParse<HttpStatusCode>(statusStr, out var httpStatCode) ? (Option<HttpStatusInfo>)Option.None : (Option<HttpStatusInfo>)new HttpStatusInfo(version, httpStatCode);
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

        public static Option<HttpMsgHeader> TryParse(string input)
        {
            if (string.IsNullOrEmpty(input))
                return Option.None;

            var sp = input.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length < 2)
                return Option.None;

            Option<HttpMsgHeader> TryParseWithInfo(HttpStatusInfo info, IEnumerable<string> headers)
            {
                var h = new Dictionary<string, List<string>>(headers.Count(), StringComparer.OrdinalIgnoreCase);
                var kvps = headers.Choose(s => StringModule.TryParseKvp(s, ':'));

                foreach (var (key, val) in kvps)
                {
                    if (!h.ContainsKey(key))
                        h.Add(key, new List<string>(1));
                    h[key].Add(val);
                }

                var h2 = new Dictionary<string, ReadOnlyCollection<string>>(h.Count);
                foreach (var kvp in h)
                    h2.Add(kvp.Key, ReadOnlyCollectionModule.OfSeq(kvp.Value));
                var readonlyD = ReadOnlyDictModule.OfDict(h2);

                return new HttpMsgHeader(info, readonlyD);
            }

            var statInfoOpt = HttpStatusInfo.TryParse(sp[0]);
            return statInfoOpt.Match(
                info => TryParseWithInfo(info, sp.Skip(1)),
                _ => Option.None
            );
        }
    }

    public static class HttpModule
    {
#if DEBUG
        public
#else
        private
#endif
            class HttpReqState : IDisposable
        {
            private readonly List<HttpStatusInfo> _statuses;
            private readonly Dictionary<string, List<string>> _headers;
            private Option<MemoryStream> _contentMemeStream;

            public HttpReq Req { get; }
            public libcurl.unsafe_write_callback HeaderDataHandler { get; }
            public libcurl.unsafe_write_callback ContentDataHandler { get; }
            public CurlSlist HeadersSlist { get; }
            public ReadOnlyCollection<HttpStatusInfo> Statuses { get; }
            public TaskCompletionSource<HttpResp> Tcs { get; }
            public int Redirects { get; set; }

            public ReadOnlyDictionary<string, ReadOnlyCollection<string>> Headers
            {
                get
                {
                    var tmp = new Dictionary<string, ReadOnlyCollection<string>>(_headers.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in _headers)
                        tmp.Add(kvp.Key, ReadOnlyCollectionModule.OfSeq(kvp.Value));
                    return ReadOnlyDictModule.OfDict(tmp);
                }
            }

            public ReadOnlyMemory<byte> Content
            {
                get
                {
                    return _contentMemeStream.Match(
                        meme => new ReadOnlyMemory<byte>(meme.GetBuffer(), 0, (int)meme.Position),
                        _ => ReadOnlyMemory<byte>.Empty
                    );
                }
            }

            private static CurlSlist CreateSList(HttpReq req)
            {
                var slist = libcurl.curl_slist_append(new CurlSlist(IntPtr.Zero), "Expect:");
                slist = libcurl.curl_slist_append(slist, "Accept:");
                foreach (var (key, val) in req.Headers)
                    slist = libcurl.curl_slist_append(slist, $"{key}: {val}");
                return slist;
            }

            private static unsafe ulong Write(byte* data, ulong size, ulong nmemb, Stream stream)
            {
                var len = size * nmemb;
                var intLen = (int)len;
                var dataSpan = new ReadOnlySpan<byte>(data, intLen);
                stream.Write(dataSpan);
                return len;
            }

            private unsafe ulong HandleHeaderLine(byte* data, ulong size, ulong nmemb, void* _)
            {
                var len = size * nmemb;
                var intLen = (int)len;
                var dataSpan = new ReadOnlySpan<byte>(data, intLen);

                var str = StringModule.OfSpan(dataSpan);
                if (string.IsNullOrWhiteSpace(str))
                    return len;

                var input = str.Trim();
                var statInfoOpt = HttpStatusInfo.TryParse(input);
                statInfoOpt.Switch(
                    info => _statuses.Add(info),
                    _ =>
                    {
                        var kvpOpt = StringModule.TryParseKvp(input, ':');
                        if (kvpOpt.IsSome)
                        {
                            var (key, val) = kvpOpt.Value;
                            if (!_headers.ContainsKey(key))
                                _headers.Add(key, new List<string>(1));
                            _headers[key].Add(val.Trim());
                        }
                    }
                );

                return len;
            }

            private static int ContentLen(IDictionary<string, List<string>> headers)
            {
                if (headers.ContainsKey("content-length"))
                {
                    var clS = headers["content-length"].Last();
                    return int.TryParse(clS, out var cl) ? cl : 256;
                }
                else
                {
                    return 256;
                }
            }

            private MemoryStream CreateMemeStream()
            {
                var cl = ContentLen(_headers);
                var meme = new MemoryStream(cl);
                _contentMemeStream = meme;
                return meme;
            }

            private unsafe ulong HandleContent(byte* data, ulong size, ulong nmemb, void* __)
            {
                var memeStream =
                    _contentMemeStream.Match(
                        SysModule.Id,
                        _ => CreateMemeStream()
                    );
                return Write(data, size, nmemb, memeStream);
            }

            public void Dispose()
            {
                if (_contentMemeStream.IsSome)
                    _contentMemeStream.Value.Dispose();
                HeadersSlist.Dispose();
            }

            public unsafe HttpReqState(HttpReq req)
            {
                _contentMemeStream = Option.None;
                _statuses = new List<HttpStatusInfo>(1);
                _headers = new Dictionary<string, List<string>>(1, StringComparer.OrdinalIgnoreCase);

                Req = req;
                HeaderDataHandler = HandleHeaderLine;
                ContentDataHandler = HandleContent;
                HeadersSlist = CreateSList(req);
                Statuses = ReadOnlyCollectionModule.OfSeq(_statuses);
                Tcs = new TaskCompletionSource<HttpResp>();
            }
        }

        private const int HttpVersion11 = 2;
        private const int HttpVersion20 = 4;

#if DEBUG
        public
#else
        private
#endif
            static readonly Queue<CurlMultiAgent<HttpReqState>> Agents;

        private static string CurlCodeStrErr(CURLcode code)
        {
            var ptr = libcurl.curl_easy_strerror(code);
            return Marshal.PtrToStringAnsi(ptr);
        }

        static HttpModule()
        {
            var initResult = libcurl.curl_global_init();
            if (initResult != CURLcode.OK)
                Environment.FailFast($"curl_global_init returned {initResult} ~ {CurlCodeStrErr(initResult)}");
            try
            {
#if DEBUG
                var max = 1;
#else
                var max = 6;
#endif
                var q = new Queue<CurlMultiAgent<HttpReqState>>();
                for (var i = 0; i < max; i++)
                {
                    var agent = new CurlMultiAgent<HttpReqState>(200);
                    q.Enqueue(agent);
                }

                Agents = q;
            }
            catch (InvalidOperationException e)
            {
                Environment.FailFast($"failed to create curlmultiagent. {e.GetType().Name} ~ {e.Message}", e);
            }

            CACertInfo.Init();
        }

        private static void ConfigureEz(CurlEzHandle ez, HttpReqState state)
        {
            try
            {
                //configures curl easy handle. 
                var httpReq = state.Req;
                CurlModule.ValidateSetOptResult(
                    libcurl.curl_easy_setopt(ez, CURLoption.CUSTOMREQUEST, state.Req.HttpMethod)
                );
                CurlModule.ValidateSetOptResult(
                    libcurl.curl_easy_setopt(ez, CURLoption.URL, httpReq.Uri.ToString())
                );
                CurlModule.ValidateSetOptResult(
                    libcurl.curl_easy_setopt(ez, CURLoption.TIMEOUT_MS, (int)httpReq.Timeout.TotalMilliseconds)
                );
                CurlModule.ValidateSetOptResult(
                    libcurl.curl_easy_setopt(ez, CURLoption.HEADERFUNCTION, state.HeaderDataHandler)
                );
                CurlModule.ValidateSetOptResult(
                    libcurl.curl_easy_setopt(ez, CURLoption.WRITEFUNCTION, state.ContentDataHandler)
                );

                var fi = new FileInfo($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}.curl{Path.DirectorySeparatorChar}curl-ca-bundle.crt");
                CurlModule.ValidateSetOptResult(
                    libcurl.curl_easy_setopt(ez, CURLoption.CAINFO, fi.FullName)
                );

#if DEBUG
                CurlModule.ValidateSetOptResult(
                    libcurl.curl_easy_setopt(ez, CURLoption.SSL_VERIFYPEER, 0)
                );
#endif

                var acceptEncodingOpt = httpReq.Headers.TryFind(
                    valueTup =>
                    {
                        var (key, _) = valueTup;
                        return key.InvariantEquals("accept-encoding");
                    }
                );
                if (acceptEncodingOpt.IsSome)
                {
                    var (_, acceptEncoding) = acceptEncodingOpt.Value;
                    CurlModule.ValidateSetOptResult(
                        libcurl.curl_easy_setopt(ez, CURLoption.ACCEPT_ENCODING, acceptEncoding)
                    );
                }

                CurlModule.ValidateSetOptResult(
                    libcurl.curl_easy_setopt(ez, CURLoption.HTTPHEADER, state.HeadersSlist)
                );

                CurlModule.ValidateSetOptResult(
                    libcurl.curl_easy_setopt(ez, CURLoption.COOKIEFILE, "")
                );

                if (httpReq.Cookies.Any())
                {
                    var cookiesStr = string.Join("; ", httpReq.Cookies.Select(SysModule.ToString));
                    CurlModule.ValidateSetOptResult(
                        libcurl.curl_easy_setopt(ez, CURLoption.COOKIE, cookiesStr)
                    );
                }

                void SetProxy(Proxy proxy)
                {
                    CurlModule.ValidateSetOptResult(
                        libcurl.curl_easy_setopt(ez, CURLoption.PROXY, proxy.Uri.ToString())
                    );
                    if (proxy.Credentials.IsSome)
                    {
                        var cred = proxy.Credentials.Value;
                        CurlModule.ValidateSetOptResult(
                            libcurl.curl_easy_setopt(ez, CURLoption.PROXYUSERPWD, cred.ToString())
                        );
                    }
                }

                if (httpReq.Proxy.IsSome)
                    SetProxy(httpReq.Proxy.Value);

                if (httpReq.ProtocolVersion.Major == 1)
                {
                    CurlModule.ValidateSetOptResult(
                        libcurl.curl_easy_setopt(ez, CURLoption.HTTP_VERSION, HttpVersion11)
                    );
                }
                else if (httpReq.ProtocolVersion.Major == 2)
                {
                    CurlModule.ValidateSetOptResult(
                        libcurl.curl_easy_setopt(ez, CURLoption.HTTP_VERSION, HttpVersion20)
                    );
                }

                void SetContent(HttpContent content)
                {
                    var bytes = content.Content.AsArraySeg();
                    CurlModule.ValidateSetOptResult(
                        libcurl.curl_easy_setopt(ez, CURLoption.POSTFIELDSIZE, bytes.Count)
                    );
                    CurlModule.ValidateSetOptResult(
                        libcurl.curl_easy_setopt(ez, CURLoption.COPYPOSTFIELDS, bytes.Array)
                    );
                }

                if (!httpReq.KeepAlive)
                {
                    CurlModule.ValidateSetOptResult(
                        libcurl.curl_easy_setopt(ez, CURLoption.FORBID_REUSE, 1)
                    );
                }

                if (httpReq.ContentBody.IsSome)
                    SetContent(httpReq.ContentBody.Value);
            }
            catch (InvalidOperationException e)
            {
                state.Tcs.SetException(e);
            }
        }

        // parses response from native curl easy handle
        private static void ParseResp(CurlEzHandle ez, HttpReqState state)
        {
            if (state.Statuses.Count == 0)
                CurlModule.CurlEx("malformed http response received. no status info parsed.");

            CurlModule.ValidateGetInfoResult(
                libcurl.curl_easy_getinfo(ez, CURLINFO.EFFECTIVE_URL, out IntPtr ptr)
            );

            var uriStr = Marshal.PtrToStringAnsi(ptr);
            if (string.IsNullOrWhiteSpace(uriStr))
                CurlModule.CurlEx("failed to get uri from curl easy.");
            if (!Uri.TryCreate(uriStr, UriKind.Absolute, out var uri))
                ExnModule.InvalidOp("failed to parse uri from curl easy.");

            var cookz = new List<Cookie>();
            if (state.Headers.ContainsKey("set-cookie"))
            {
                foreach (var cook in state.Headers["set-cookie"])
                {
                    var cookOpt = Cookie.TryParse(cook, $".{uri.Authority}");
                    if (cookOpt.IsSome)
                        cookz.Add(cookOpt.Value);
                }
            }

            var contentMem = state.Content;
            var resp = new HttpResp(
                state.Statuses[state.Statuses.Count - 1].StatusCode,
                uri,
                state.Headers,
                ReadOnlyCollectionModule.OfSeq(cookz),
                contentMem.Span
            );
            state.Tcs.SetResult(resp);
        }

        private static ReqOpCompletedAction HandleResp(CurlEzHandle ez, HttpReqState state, CURLcode result)
        {
            var dispose = true;
            try
            {
                switch (result)
                {
                    case CURLcode.OK:
                        if (state.Req.AutoRedirect && (int)state.Statuses.Last().StatusCode / 100 == 3 && state.Redirects++ < 10)
                        {
                            CurlModule.ValidateGetInfoResult(
                                libcurl.curl_easy_getinfo(ez, CURLINFO.REDIRECT_URL, out IntPtr urlPtr)
                            );
                            if (urlPtr == IntPtr.Zero)
                                CurlModule.CurlEx("http server returned redirect status code with no redirect uri.");

                            var redirect = Marshal.PtrToStringAnsi(urlPtr);
                            CurlModule.ValidateSetOptResult(libcurl.curl_easy_setopt(ez, CURLoption.URL, redirect));
                            dispose = false;
                            return ReqOpCompletedAction.ReuseHandleAndRetry;
                        }
                        ParseResp(ez, state);
                        return ReqOpCompletedAction.ResetHandleAndNext;
                    case CURLcode.OPERATION_TIMEDOUT:
                        CurlModule.CurlEx(
                            $"Timeout error occured after trying to retrieve response for request {state.Req.HttpMethod} {state.Req.Uri} {state.Req.ProtocolVersion}.",
                            result
                        );
                        return ReqOpCompletedAction.ResetHandleAndNext;
                    default:
                        CurlModule.CurlEx(
                            $"Error occured trying to retrieve response for request {state.Req.HttpMethod} {state.Req.Uri} {state.Req.ProtocolVersion}.",
                            result
                        );
                        return ReqOpCompletedAction.ResetHandleAndNext;
                }
            }
            catch (InvalidOperationException e)
            {
                state.Tcs.SetException(e);
                return ReqOpCompletedAction.ResetHandleAndNext;
            }
            finally
            {
                if (dispose)
                    state.Dispose();
            }
        }

        private static CurlMultiAgent<HttpReqState> NextCurlMultiAgent()
        {
            lock (Agents)
            {
                var agent = Agents.Dequeue();
                Agents.Enqueue(agent);
                return agent;
            }
        }

        public static Task<HttpResp> RetrRespAsync(HttpReq req)
        {
            if (req.ProxyRequired && !req.Proxy.IsSome)
                ExnModule.InvalidArg("Proxy is required for this request.", nameof(req));

            var state = new HttpReqState(req);
            var reqCtx = new ReqCtx<HttpReqState>(
                state,
                ConfigureEz,
                HandleResp
            );
            NextCurlMultiAgent().ExecReq(reqCtx);
            return state.Tcs.Task;
        }
    }
}
