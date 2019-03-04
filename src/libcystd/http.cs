using LibCyStd.LibCurl;
using LibCyStd.LibOneOf.Types;
using LibCyStd.Net;
using LibCyStd.Seq;
using System;
using System.Buffers;
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
        public static string UrlEncode(in string input)
        {
            var sb = new StringBuilder(input.Length + 50);
            void Append(char ch)
            {
                if (UnreservedChars.IndexOf(ch) == -1) sb.Append("%").AppendFormat("{0:X2}", (int)ch);
                else sb.Append(ch);
            }
            input.Iter(Append);
            return sb.ToString();
        }

        public static string UrlEncodeKvp(in (string key, string val) kvp)
        {
            var (key, val) = kvp;
            return $"{UrlEncode(key)}={UrlEncode(val)}";
        }

        public static string UrlEncodeKvp((string key, string val) kvp) => UrlEncodeKvp(in kvp);

        /// <summary>
        /// Url encodes sequence of <see cref="ValueTuple"/>s;.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static string UrlEncodeSeq(in IEnumerable<(string key, string value)> sequence) =>
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
            return input.InvariantStartsWith("Set-Cookie:") ? tmp.Substring(11).Trim() : tmp;
        }

        private static Option<(string key, string val)> TryParseCookieNvp(in string input)
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
                return None.Value;
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

        public static Option<Cookie> TryParse(in string input, in string defaultDomain)
        {
            if (string.IsNullOrWhiteSpace(input))
                return None.Value;

            var inp = TryParseInp(input);
            var sp = inp.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var kvpStr = sp[0];
            var kvpOpt = TryParseCookieNvp(kvpStr);
            if (!kvpOpt.IsSome)
                return None.Value;

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

        public override string ToString()
        {
            return Content.ToString();
        }
    }

    public class StringHttpContent : HttpContent
    {
        private StringHttpContent(in ReadOnlyMemory<byte> content)
            : base(content) { }

        public StringHttpContent(in string str)
            : this(ReadOnlyMemoryModule.OfString(str)) { }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(Content.AsArray());
        }
    }

    /// <summary>
    /// Contains a sequence of <see cref="ValueTuples"/> with two strings that later gets url encoded and convereted to <see cref="ReadOnlyMemory"/> to send with http web request.
    /// </summary>
    public class EncodedFormValuesHttpContent : HttpContent
    {
        private EncodedFormValuesHttpContent(in ReadOnlyMemory<byte> content)
            : base(content) { }

        public EncodedFormValuesHttpContent(in IEnumerable<(string, string)> sequence)
            : this(ReadOnlyMemoryModule.OfString(HttpUtils.UrlEncodeSeq(sequence))) { }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(Content.AsArray());
        }
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
        //public int MaxRetries { get; set; }

        public HttpReq(in string method, in Uri uri)
        {
            if (!uri.Scheme.InvariantStartsWith("http"))
                ExnModule.InvalidArg($"{uri} is not a http/https uri.", nameof(uri));

            HttpMethod = method;
            Uri = uri;
            Headers = ReadOnlyCollectionModule.OfSeq(SeqModule.Empty<(string key, string val)>());
            ContentBody = None.Value;
            Proxy = None.Value;
            Timeout = TimeSpan.FromSeconds(30.0);
            Cookies = new HashSet<Cookie>();
            AutoRedirect = true;
            ProxyRequired = true;
            ProtocolVersion = HttpVersion.Http11;
        }

#pragma warning disable RCS1139
        /// <exception cref="ArgumentException"/>
#pragma warning restore RCS1139

        public HttpReq(
            in string method,
            in string uri)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var ruri))
                ExnModule.InvalidArg($"{uri} is not a valid Uri.", nameof(uri));
            if (!ruri.Scheme.InvariantStartsWith("http"))
                ExnModule.InvalidArg($"{uri} is not a http/https uri.", nameof(uri));
            HttpMethod = method;
            Uri = ruri;
            Headers = ReadOnlyCollectionModule.OfSeq(new List<(string, string)>());
            ContentBody = None.Value;
            Proxy = None.Value;
            Timeout = TimeSpan.FromSeconds(30.0);
            Cookies = new HashSet<Cookie>();
            AutoRedirect = true;
            ProxyRequired = true;
            ProtocolVersion = HttpVersion.Http11;
        }

        private static string Http1ReqToStr(in HttpReq req)
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
            {
                sb.AppendLine().Append(req.ContentBody.Value.ToString());
            }

            return sb.AppendLine().ToString();
        }

        private static string Http2ReqToStr(in HttpReq req)
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
    public class HttpResp
    {
        private string _content;

        public HttpStatusCode StatusCode { get; }
        public Uri Uri { get; }
        public ReadOnlyDictionary<string, ReadOnlyCollection<string>> Headers { get; }
        public ReadOnlyCollection<Cookie> Cookies { get; }
        public ReadOnlyMemory<byte> ContentData { get; }

        public string Content
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_content))
                    _content = StringModule.OfMemory(ContentData);
                return _content;
            }
        }

        public HttpResp(
            in HttpStatusCode statusCode,
            in Uri uri,
            in ReadOnlyDictionary<string, ReadOnlyCollection<string>> headers,
            in ReadOnlyCollection<Cookie> cookies,
            in ReadOnlyMemory<byte> contentData)
        {
            _content = "";
            StatusCode = statusCode;
            Uri = uri;
            Headers = headers;
            Cookies = cookies;
            ContentData = contentData;
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
        public static bool RespIsExpected(in HttpResp resp, in HttpStatusCode stat, in string chars) =>
            resp.StatusCode == stat && resp.Content.InvariantContains(chars);

        public static bool RespIsExpected(in HttpResp resp, in HttpStatusCode stat) =>
            resp.StatusCode == stat;

        public static void CheckExpected(in HttpResp resp, in Func<HttpResp, bool> predicate, in Action onUnexpected)
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

        public HttpStatusInfo(in Version version, in HttpStatusCode statusCode)
        {
            Version = version;
            StatusCode = statusCode;
        }

        private static string VersionStr(in string input)
        {
            return input == "2" ? "2.0" : input;
        }

        public override string ToString()
        {
            return $"HTTP/{Version} {StatusCode}";
        }

        public static Option<HttpStatusInfo> TryParse(in string input)
        {
            if (!input.InvariantStartsWith("http"))
                return None.Value;

            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 1)
                return None.Value;

            var httpVer = words[0].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (httpVer.Length != 2)
                return None.Value;
            var versionStr = VersionStr(httpVer[1]);
            if (!Version.TryParse(versionStr, out var version))
                return None.Value;

            var statusStr = words[1];
            return !Enum.TryParse<HttpStatusCode>(statusStr, out var httpStatCode) ? (Option<HttpStatusInfo>)None.Value : (Option<HttpStatusInfo>)new HttpStatusInfo(version, httpStatCode);
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
                return None.Value;

            var sp = input.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length < 2)
                return None.Value;

            Option<HttpMsgHeader> TryParseWithInfo(in HttpStatusInfo info, in IEnumerable<string> headers)
            {
                var h = new Dictionary<string, List<string>>(headers.Count());
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
                none => none
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
                        tmp.Add(kvp.Key, ReadOnlyCollectionModule.OfSeq(kvp.Value));
                    return ReadOnlyDictModule.OfDict(tmp);
                }
            }

            public ReadOnlyMemory<byte> Content
            {
                get
                {
                    return _contentMemeStream.Match(
                        meme => new ReadOnlyMemory<byte>(meme.ToArray()),
                        _ => new ReadOnlyMemory<byte>(new byte[0])
                    );
                }
            }

            private static SafeSlistHandle CreateSList(in HttpReq req)
            {
                var slist = libcurl.curl_slist_append(SafeSlistHandle.Null, "Expect:");
                foreach (var (key, val) in req.Headers)
                    slist = libcurl.curl_slist_append(slist, $"{key}: {val}");
                return slist;
            }

            private static unsafe ulong Write(byte* data, in ulong size, in ulong nmemb, Stream stream)
            {
                var len = size * nmemb;
                var intLen = (int)len;
                var buffer = ArrayPool<byte>.Shared.Rent(intLen);
                try
                {
                    var dataSpan = new ReadOnlySpan<byte>(data, intLen);
                    dataSpan.CopyTo(buffer);
                    stream.Write(buffer, 0, intLen);
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
                var intLen = (int)len;
                var buffer = ArrayPool<byte>.Shared.Rent(intLen);
                try
                {
                    var dataSpan = new ReadOnlySpan<byte>(data, intLen);
                    dataSpan.CopyTo(buffer);

                    var str = Encoding.UTF8.GetString(buffer, 0, intLen);
                    if (string.IsNullOrWhiteSpace(str))
                        return len;

                    var input = str.Trim();
                    var statInfoOpt = HttpStatusInfo.TryParse(input);
                    if (statInfoOpt.IsSome)
                    {
                        _statuses.Add(statInfoOpt.Value);
                        return len;
                    }

                    var kvpOpt = StringModule.TryParseKvp(input, ':');
                    if (kvpOpt.IsSome)
                    {
                        var (key, val) = kvpOpt.Value;
                        if (!_headers.ContainsKey(key))
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

            public unsafe HttpReqState(in HttpReq req)
            {
                _contentMemeStream = None.Value;
                _statuses = new List<HttpStatusInfo>(1);
                _headers = new Dictionary<string, List<string>>(8, StringComparer.OrdinalIgnoreCase);

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
            static readonly CurlMultiAgent<HttpReqState> Agent;

        private static string CurlCodeStrErr(in CURLcode code)
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
                Agent = new CurlMultiAgent<HttpReqState>(500);
            }
            catch (InvalidOperationException e)
            {
                Environment.FailFast($"failed to create curlmultiagent. {e.GetType().Name} ~ {e.Message}", e);
            }

            CACertInfo.Init();
        }

        private static void CheckSetOpt(in CURLcode code, in HttpReq req)
        {
            if (code == CURLcode.OK)
                return;

            throw new CurlException(
                $"curl_easy_setopt returned {code} ~ {CurlCodeStrErr(code)} for req: {req}"
            );
        }

        private static void CheckGetInfo(in CURLcode code, in HttpReq req)
        {
            if (code == CURLcode.OK)
                return;

            throw new CurlException(
                $"curl_easy_getinfo returned {code} ~ {CurlCodeStrErr(code)} for req: {req}"
            );
        }

        private static void ConfigureEz(SafeEasyHandle ez, HttpReqState state)
        {
            try
            {
                //configures curl easy handle. 
                var httpReq = state.Req;
                CheckSetOpt(
                    libcurl.curl_easy_setopt(ez, CURLoption.CUSTOMREQUEST, state.Req.HttpMethod),
                    httpReq
                );
                CheckSetOpt(
                    libcurl.curl_easy_setopt(ez, CURLoption.URL, httpReq.Uri.ToString()),
                    httpReq
                );
                CheckSetOpt(
                    libcurl.curl_easy_setopt(ez, CURLoption.TIMEOUT_MS, (int)httpReq.Timeout.TotalMilliseconds),
                    httpReq
                );
                CheckSetOpt(
                    libcurl.curl_easy_setopt(ez, CURLoption.HEADERFUNCTION, state.HeaderDataHandler),
                    httpReq
                );
                CheckSetOpt(
                    libcurl.curl_easy_setopt(ez, CURLoption.WRITEFUNCTION, state.ContentDataHandler),
                    httpReq
                );

                var fi = new FileInfo($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}.curl{Path.DirectorySeparatorChar}curl-ca-bundle.crt");
                CheckSetOpt(
                    libcurl.curl_easy_setopt(ez, CURLoption.CAINFO, fi.FullName),
                    httpReq
                );

#if DEBUG
                CheckSetOpt(
                    libcurl.curl_easy_setopt(ez, CURLoption.SSL_VERIFYPEER, 0),
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
                if (acceptEncodingOpt.IsSome)
                {
                    var (_, acceptEncoding) = acceptEncodingOpt.Value;
                    CheckSetOpt(
                        libcurl.curl_easy_setopt(ez, CURLoption.ACCEPT_ENCODING, acceptEncoding),
                        httpReq
                    );
                }

                CheckSetOpt(
                    libcurl.curl_easy_setopt(ez, CURLoption.HTTPHEADER, state.HeadersSlist.DangerousGetHandle()),
                    httpReq
                );

                CheckSetOpt(
                    libcurl.curl_easy_setopt(ez, CURLoption.COOKIEFILE, ""),
                    httpReq
                );

                if (httpReq.Cookies.Any())
                {
                    var cookiesStr = string.Join("; ", httpReq.Cookies.Select(SysModule.ToString));
                    CheckSetOpt(
                        libcurl.curl_easy_setopt(ez, CURLoption.COOKIE, cookiesStr),
                        httpReq
                    );
                }

                void SetProxy(in Proxy proxy)
                {
                    CheckSetOpt(
                        libcurl.curl_easy_setopt(ez, CURLoption.PROXY, proxy.Uri.ToString()),
                        httpReq
                    );
                    if (proxy.Credentials.IsSome)
                    {
                        var cred = proxy.Credentials.Value;
                        CheckSetOpt(
                            libcurl.curl_easy_setopt(ez, CURLoption.PROXYUSERPWD, cred.ToString()),
                            httpReq
                        );
                    }
                }

                if (httpReq.Proxy.IsSome)
                    SetProxy(httpReq.Proxy.Value);

                if (httpReq.AutoRedirect)
                {
                    CheckSetOpt(
                        libcurl.curl_easy_setopt(ez, CURLoption.FOLLOWLOCATION, 1),
                        httpReq
                    );
                    CheckSetOpt(
                        libcurl.curl_easy_setopt(ez, CURLoption.MAXREDIRS, 10),
                        httpReq
                    );
                }

                if (httpReq.ProtocolVersion.Major == 1)
                {
                    CheckSetOpt(
                        libcurl.curl_easy_setopt(ez, CURLoption.HTTP_VERSION, HttpVersion11),
                        httpReq
                    );
                }
                else if (httpReq.ProtocolVersion.Major == 2)
                {
                    CheckSetOpt(
                        libcurl.curl_easy_setopt(ez, CURLoption.HTTP_VERSION, HttpVersion20),
                        httpReq
                    );
                }

                void SetContent(in HttpContent content)
                {
                    var bytes = content.Content.AsArray();
                    CheckSetOpt(
                        libcurl.curl_easy_setopt(ez, CURLoption.POSTFIELDSIZE, bytes.Length),
                        httpReq
                    );

                    CheckSetOpt(
                        libcurl.curl_easy_setopt(ez, CURLoption.COPYPOSTFIELDS, bytes),
                        httpReq
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
        private static void ParseResp(in SafeEasyHandle ez, in HttpReqState state)
        {
            if (state.Statuses.Count == 0)
                ExnModule.InvalidOp("malformed http response received. no status info parsed.");

            var httpReq = state.Req;
            CheckGetInfo(
                libcurl.curl_easy_getinfo(ez, CURLINFO.EFFECTIVE_URL, out IntPtr ptr),
                httpReq
            );

            var uriStr = Marshal.PtrToStringAnsi(ptr);
            if (string.IsNullOrWhiteSpace(uriStr))
                ExnModule.InvalidOp("failed to get uri from curl easy.");
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

            var resp = new HttpResp(
                state.Statuses[state.Statuses.Count - 1].StatusCode,
                uri,
                state.Headers,
                ReadOnlyCollectionModule.OfSeq(cookz),
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
                    switch (result)
                    {
                        case CURLcode.OK:
                            ParseResp(ez, state);
                            break;
                        case CURLcode.OPERATION_TIMEDOUT:
                            ExnModule.Timeout(
                                $"timeout error occured after trying to retrieve response for request {state.Req}. {result} ~ {CurlCodeStrErr(result)}."
                            );
                            break;
                        default:
                            ExnModule.InvalidOp(
                                $"Error occured trying to retrieve response for request {state.Req}. {result} ~ {CurlCodeStrErr(result)}."
                            );
                            break;
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
            if (req.ProxyRequired && !req.Proxy.IsSome)
                throw new ArgumentException("Proxy is required for this request.", nameof(req));

            var state = new HttpReqState(req);
            var reqCtx = new ReqCtx<HttpReqState>(
                state,
                ConfigureEz,
                HandleResp
            );
            Agent.ExecReq(reqCtx);
            return state.Tcs.Task;
        }
    }
}
