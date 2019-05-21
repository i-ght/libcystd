using LibCyStd.LibOneOf;
using LibCyStd.LibOneOf.Types;
using LibCyStd.Seq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LibCyStd
{
    public readonly struct Unit
    {
        public static Unit Value { get; }
        static Unit() => Value = new Unit();
    }

    public readonly struct Option<TValue> : IEquatable<Option<TValue>>
    {
        private readonly bool _initialized;
        private readonly OneOf<TValue, None> _value;

        public TValue Value
        {
            get
            {
                if (_value.IsT0)
                    return _value.T0Value;
                throw new InvalidOperationException("no value associated with this option.");
            }
        }

        public bool IsSome => _initialized && _value.IsT0;
        public bool IsNone => !_initialized || _value.IsT1;

        public (bool success, TValue value) TryGetValue() => _value.IsT0 ? (true, _value.T0Value) : ((bool success, TValue value))(false, default!);

        public Option(TValue value)
        {
            _value = value;
            _initialized = true;
        }

        public Option(None none)
        {
            _value = none;
            _initialized = true;
        }

        public void Deconstruct(out bool isSome)
        {
            isSome = IsSome;
        }

        public void Deconstruct(out bool isSome, out TValue value)
        {
            isSome = IsSome;
            value = IsSome ? _value.T0Value : default!;
        }

        public void Switch(Action<TValue> f0, Action<None> f1) => _value.Switch(f0, f1);
        public TResult Match<TResult>(Func<TValue, TResult> f0, Func<None, TResult> f1) => _value.Match(f0, f1);

        public override bool Equals(object obj)
        {
            return !(obj is Option<TValue> opt) ? false : Equals(opt);
        }

        public override int GetHashCode()
        {
            var hashCode = 2067055381;
            hashCode = (hashCode * -1521134295) + EqualityComparer<OneOf<TValue, None>>.Default.GetHashCode(_value);
            hashCode = (hashCode * -1521134295) + EqualityComparer<TValue>.Default.GetHashCode(Value);
            hashCode = (hashCode * -1521134295) + IsSome.GetHashCode();
            return (hashCode * -1521134295) + IsNone.GetHashCode();
        }

        public bool Equals(Option<TValue> other)
        {
            return EqualityComparer<OneOf<TValue, None>>.Default.Equals(_value, other._value)
                   && EqualityComparer<TValue>.Default.Equals(Value, other.Value)
                   && IsSome == other.IsSome
                   && IsNone == other.IsNone;
        }

        public override string ToString()
        {
            return _value.IsT0 ? $"Some {_value.T0Value}" : "None";
        }

        public static implicit operator Option<TValue>(TValue value) => new Option<TValue>(value);
        public static implicit operator Option<TValue>(None none) => new Option<TValue>(none);
    }

    public static class Option
    {
        public static bool IsSome<TValue>(Option<TValue> option) => option.IsSome;
        public static bool IsNone<TValue>(Option<TValue> option) => option.IsNone;
        public static TValue Value<TValue>(Option<TValue> option) => option.Value;
        public static Option<TValue> Some<TValue>(TValue value) => new Option<TValue>(value);

        public static None None { get; }

        public static Option<T> NoneOpt<T>()
        {
            return new Option<T>(None);
        }

        public static Option<TValue> CreateNone<TValue>() => new Option<TValue>(None);

        static Option() => None = None.Value;
    }

    /// <summary>
    /// <see cref="IDisposable"/> utility functions.
    /// </summary>
    public static class DisposableModule
    {
        public static void Dispose(IDisposable d) => d.Dispose();

        public static void DisposeSeq(IEnumerable<IDisposable> disposables) =>
            disposables.Iter(Dispose);
    }

    public static class SysModule
    {
        public static void TryThrow(Action fn, Action onErr)
        {
            try
            {
                fn();
            }
            catch
            {
                onErr();
                throw;
            }
        }

        public static async Task TryAsync(Func<Task> fn, Action onErr)
        {
            try
            {
                await fn().ConfigureAwait(false);
            }
            catch
            {
                onErr();
                throw;
            }
        }

        /// <summary>
        /// Identity function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Id<T>(T value) => value;

        public static string ToString<T>(T obj) => obj!.ToString();

    }

    /// <summary>
    /// <see cref="Exception"/> utility functions.
    /// </summary>
    public static class ExnModule
    {
        /// <summary>
        /// throws a new <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="message"></param>
        public static Unit InvalidOp(string message)
        {
            throw new InvalidOperationException(message);
        }

        public static Unit ObjDisposed(object o)
        {
            throw new ObjectDisposedException(o.GetType().Name);
        }

        public static Unit ObjDisposed(string objectName)
        {
            throw new ObjectDisposedException(objectName);
        }

        /// <summary>
        /// throws a new <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public static Unit InvalidOp(string message, Exception inner)
        {
            throw new InvalidOperationException(message, inner);
        }

        /// <summary>
        /// throws a new <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="paramName"></param>
        public static Unit InvalidArg(string message, string paramName)
        {
            throw new ArgumentException(message, paramName);
        }

        /// <summary>
        /// throws a new <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="paramName"></param>
        public static Unit NullArg(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Throws a new <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="message"></param>
        public static Unit Timeout(string message)
        {
            throw new TimeoutException(message);
        }

        /// <summary>
        /// Throws a new <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public static Unit Timeout(string message, Exception inner)
        {
            throw new TimeoutException(message, inner);
        }

        /// <summary>
        /// Reraises <see cref="Exception"/> with stack trace intact.
        /// </summary>
        /// <param name="e"></param>
        public static Unit Reraise(Exception e)
        {
            ExceptionDispatchInfo.Capture(e).Throw();
            return Unit.Value;
        }

        /// <summary>
        /// Transforms <see cref="Exception"/> into log message.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string ToLogMessage(this Exception ex)
        {
            var src = $"Source: {ex.Source}";
            var @type = $"Exception type: {ex.GetType().Name}";
            var date = $"Date: {DateTimeOffset.Now.ToString("D")}";
            var time = $"Time: {DateTimeOffset.Now.ToString("T")}";
            var msg = $"Message: {ex.Message}";
            var stack = $"StAcK TrAcE: {ex.StackTrace}";

            var list = ReadOnlyCollectionModule.OfSeq(new[] {
                    src,
                    @type,
                    date,
                    time,
                    msg,
                    stack
                }
            );

            return string.Join(Environment.NewLine, list);
        }

        public static void NotImpl()
        {
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
            throw new NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
        }
    }

    /// <summary>
    /// Thread safe <see cref="Random"/> utility functions.
    /// </summary>
    public static class RandomModule
    {
        private static readonly Random Rand;
        private static readonly RNGCryptoServiceProvider Rng;

        static RandomModule()
        {
            Rand = new Random();
            Rng = new RNGCryptoServiceProvider();
        }

        public static int CryptoNext(int min, int max)
        {
            using var memOwner = MemoryPool<byte>.Shared.Rent(4);
            ReadOnlyMemory<byte> mem = memOwner.Memory;
            var buffer = mem.AsArraySeg().Array;
            lock (Rng)
                Rng.GetBytes(buffer);
            var scale = BitConverter.ToUInt32(buffer, 0);
            return (int)(min + ((max - min) * (scale / (uint.MaxValue + 1.0))));
        }

        public static int Next(int min, int max)
        {
            lock (Rand)
                return Rand.Next(min, max);
        }

        public static int Next(int max)
        {
            lock (Rand)
                return Rand.Next(max);
        }

        public static Span<byte> NextBytes(int len)
        {
            var bytes = new byte[len];
            lock (Rand)
                Rand.NextBytes(bytes);
            return bytes;
        }
    }

    /// <summary>
    /// <see cref="DateTimeOffset"/> utility functions.
    /// </summary>
    public static class DateTimeOffsetModule
    {
        public static DateTimeOffset Epoch { get; }
        public static TimeZoneInfo UsEast { get; }
        public static TimeZoneInfo UsCentral { get; }
        public static TimeZoneInfo UsMounta{ get; }
        public static TimeZoneInfo UsWest { get; }

        static DateTimeOffsetModule()
        {
            Epoch = new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            UsEast = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            UsCentral = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            UsMounta= TimeZoneInfo.FindSystemTimeZoneById("MountaStandard Time");
            UsWest = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        }

        public static TimeSpan UnixTimeSpan(DateTimeOffset input) => input - Epoch;

        public static long UnixMillis() => (long)UnixTimeSpan(DateTimeOffset.Now).TotalMilliseconds;

        public static long UnixSeconds() => (long)UnixTimeSpan(DateTimeOffset.Now).TotalSeconds;

        public static DateTimeOffset DateTimeInTimeZone(DateTimeOffset dt, TimeZoneInfo tzInfo)
            => TimeZoneInfo.ConvertTime(dt, tzInfo);
    }

    public static class ReadOnlySpanModule
    {
        public static ReadOnlySpan<byte> OfString(string s) => Encoding.UTF8.GetBytes(s);

        public static ReadOnlySpan<T> OfSeq<T>(IEnumerable<T> seq) => ArrayModule.OfSeq(seq);

        public static string ToHex(this in ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
                return "";
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    public static class SpanModule
    {
        public static string ToHex(this Span<byte> bytes) => ReadOnlySpanModule.ToHex(bytes);
    }

    public static class MemoryOwnerModule
    {
        public static IMemoryOwner<byte> OfString(string s)
        {
            var buffer = MemoryPool<byte>.Shared.Rent(s.Length);
            Span<byte> bytes = Encoding.UTF8.GetBytes(s);
            bytes.CopyTo(buffer.Memory.Span);
            return buffer;
        }
    }

    public static class ReadOnlyMemoryModule
    {
        public static string HexDump(this in ReadOnlyMemory<byte> mem)
        {
            if (mem.IsEmpty)
                return "";

            const int startOffset = 0;
            var length = mem.Length;
            const bool showHeader = true;

            const string columnPadding = "  ";
            const int leftColumnWidth = 8;
            const string hexLegend = " 0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F";
            const string asciiLegend = "0123456789ABCDEF";

            const string resetColor = "", offsetColor = "", dataColor = "", newlineColor = "";
            var result = new List<string>();

            if (showHeader)
            {
                result.AddRange(new[]
                {
                    "        ",
                    columnPadding,
                    hexLegend,
                    columnPadding,
                    asciiLegend,
                    "\r\n"
                });
            }

            var offset = startOffset;
            for (var bufferOffset = 0; bufferOffset < length; bufferOffset += 16)
            {
                if (bufferOffset != 0) result.Add("\r\n");

                result.AddRange(new[]
                {
                    offsetColor,
                    Convert.ToString(offset, 16).PadLeft(leftColumnWidth, '0'),
                    resetColor,
                    columnPadding
                });

                var asciiChars = new List<string>();
                var lineSize = Math.Min(length - offset, 16);

                for (var lineOffset = 0; lineOffset != lineSize; lineOffset++)
                {
                    var value = mem.Span[offset++];

                    var isNewline = value == 10;

                    var hexPair = Convert.ToString(value, 16).PadLeft(2, '0');
                    if (lineOffset != 0) result.Add(" ");

                    result.AddRange(new[]
                    {
                        isNewline ? newlineColor : dataColor,
                        hexPair,
                        resetColor
                    });

                    asciiChars.AddRange(new[]
                    {
                        isNewline ? newlineColor : dataColor,
                        value >= 32 && value <= 126
                            ? ((char)value).ToString()
                            : ".",
                        resetColor
                    });
                }

                for (var lineOffset = lineSize; lineOffset != 16; lineOffset++)
                {
                    result.Add("   ");
                    asciiChars.Add(" ");
                }

                result.Add(columnPadding);
                result.AddRange(asciiChars);
                //Array.prototype.push.apply(result, asciiChars);
            }

            var trailingSpaceCount = 0;
            for (
                var tailOffset = result.Count - 1;
                tailOffset >= 0 && result[tailOffset] == " ";
                tailOffset--
            )
            {
                trailingSpaceCount++;
            }

            return string.Concat(result.Take(result.Count - trailingSpaceCount)) + $"{Environment.NewLine}len: {mem.Length}";

            //const string colPadding = "  ";
            //const int leftColWidth = 8;
            //const string hexLegend = " 0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F";
            //const string asciiLegend = "0123456789ABCDEF";

            //var result = new List<string>();
            //result.Add("        ");
            //result.Add(colPadding);
            //result.Add(hexLegend);
            //result.Add(colPadding);
            //result.Add(asciiLegend);
            //result.Add(Environment.NewLine);

            //var offset = 0;
            //for (var bufferOffset = 0; bufferOffset < mem.Length; bufferOffset += 16)
            //{
            //    if (bufferOffset != 0)
            //        result.Add(Environment.NewLine);
            //    result.Add(Convert.ToString(offset, 16).PadLeft(leftColWidth, '0'));

            //    var asciiChars = new List<string>();
            //    var lineSize = Math.Min(mem.Length - offset, 16);
            //    for (var lineOffset = 0; lineOffset != lineSize; lineOffset++)
            //    {
            //        var value = mem.Span[offset++];
            //        if (lineOffset != 0)
            //            result.Add(" ");
            //        var hexPair = Convert.ToString(value, 16).PadLeft(2, '0');
            //        result.Add(hexPair);

            //        asciiChars.Add(value >= 32 && value <= 126 ? Encoding.UTF8.GetString(new byte[1] { value }) : ".");
            //    }

            //    for (var lineOffset = lineSize; lineOffset != 16; lineOffset++)
            //    {
            //        result.Add("   ");
            //        asciiChars.Add(" ");
            //    }

            //    result.AddRange(asciiChars);
            //}
            //var trailingSpaceCnt = 0;
            //for (var taillOfset = result.Count - 1; taillOfset >= 0 && result[taillOfset] == " "; taillOfset--)
            //{
            //    trailingSpaceCnt++;
            //}

            //return string.Join("", result.Take(result.Count - trailingSpaceCnt));
        }

        public static string ToHex(this in ReadOnlyMemory<byte> bytes) => bytes.Span.ToHex();

        public static ReadOnlyMemory<byte> OfHex(string hex)
        {
            var h = Regex.Replace(hex, @"\s+", "");
            return
                Enumerable.Range(0, h.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(h.Substring(x, 2), 16))
                .ToArray();
        }

        public static ReadOnlyMemory<byte> OfString(string s) => Encoding.UTF8.GetBytes(s);

        public static ReadOnlyMemory<T> OfSeq<T>(IEnumerable<T> seq) => ArrayModule.OfSeq(seq);

        public static (bool success, ArraySegment<T> arrSeg) TryGetArraySegment<T>(this in ReadOnlyMemory<T> memory)
            => MemoryMarshal.TryGetArray(memory, out var segment) ? (true, segment) : (false, segment);

        public static ArraySegment<T> AsArraySeg<T>(this in ReadOnlyMemory<T> memory)
        {
            var (success, seg) = TryGetArraySegment(memory);
            if (!success)
                ExnModule.InvalidOp("memory did not contaarray");
            return seg;
        }

        public static string ToBase64EncodedString(this in ReadOnlyMemory<byte> bytes)
        {
            var arraySeg = bytes.AsArraySeg();
            return Convert.ToBase64String(arraySeg.Array, arraySeg.Offset, arraySeg.Count);
        }

        public static ReadOnlyMemory<byte> OfBase64EncodedString(string input)
            => Convert.FromBase64String(input);
    }

    public enum Chars
    {
        Digits,
        Letters,
        DigitsAndLetters,
        DigitsLettersAndDashUnderscore
    }

    /// <summary>
    /// <see cref="string"/> utility functions.
    /// </summary>
    public static class StringModule
    {
        public static string Rando(Chars chars, int len)
        {
            var c = chars switch
            {
                Chars.Digits => "0123456789",
                Chars.Letters => "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
                Chars.DigitsAndLetters => "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
                Chars.DigitsLettersAndDashUnderscore => "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_",
                _ => throw new ArgumentOutOfRangeException(nameof(chars))
            };

            var sb = new StringBuilder(len);
            for (var i = 0; i < len; i++)
                sb.Append(c.Random());

            return sb.ToString();
        }

        public static string[] SplitRemoveEmpty(this string input, char delimter)
            => input.Split(new[] { delimter }, StringSplitOptions.RemoveEmptyEntries);

        public static string[] SplitRemoveEmpty(this string input, string delimter)
            => input.Split(delimter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        public static string Trim(string input) => input.Trim();

        public static Option<(string key, string val)> TryParseKvp(string input, string delimter)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Option.None;

            var sp = SplitRemoveEmpty(input, delimter);
            return sp.Length != 2 ? (Option<(string key, string val)>)Option.None : (Option<(string key, string val)>)(sp[0], sp[1]);
        }

        public static Option<(string key, string val)> TryParseKvp(string input, char delimter)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Option.None;

            var sp = SplitRemoveEmpty(input, delimter);
            return sp.Length != 2 ? (Option<(string key, string val)>)Option.None : (Option<(string key, string val)>)(sp[0], sp[1]);
        }

        public static unsafe string OfSpan(ReadOnlySpan<byte> bytes)
        {
            fixed (byte* b = bytes)
                return Encoding.UTF8.GetString(b, bytes.Length);
        }

        public static string OfMemory(in ReadOnlyMemory<byte> bytes) => OfSpan(bytes.Span);

        public static bool InvariantEquals(this string str1, string str2) =>
            string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);

        public static bool InvariantStartsWith(this string input, string value) =>
            input.StartsWith(value, StringComparison.OrdinalIgnoreCase);

        public static bool InvariantEndsWith(this string input, string value) =>
            input.EndsWith(value, StringComparison.OrdinalIgnoreCase);

        public static bool InvariantContains(this string input, string value) =>
            input.IndexOf(value, StringComparison.OrdinalIgnoreCase) > -1;

        public static bool AllNotEmptyOrWhiteSpace(IEnumerable<string> strings) =>
            strings.All(s => !string.IsNullOrWhiteSpace(s));

        public static bool AnyEmptyOrWhiteSpace(IEnumerable<string> strings) =>
            strings.Any(string.IsNullOrWhiteSpace);
    }

    /// <summary>
    /// <see cref="WebProxy"/> utility functions.
    /// </summary>
    public static class WebProxyModule
    {
        /// <summary>
        /// Tries to parse input to a <see cref="WebProxy"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Option<WebProxy> TryParse(string input)
        {
            var sp = input.SplitRemoveEmpty(':');

            Option<WebProxy> TryParse2(string _input)
            {
                try { return new WebProxy(_input); }
                catch (Exception e) when (e is ArgumentException || e is UriFormatException)
                {
                    return Option.None;
                }
            }

            Option<WebProxy> TryParse4()
            {
                var (hostPort, username, pw) = ($"{sp[0]}:{sp[1]}", sp[2], sp[3]);
                var cred = new NetworkCredential(username, pw);

                try { return new WebProxy(hostPort) { Credentials = cred }; }
                catch (Exception e) when (e is ArgumentException || e is UriFormatException)
                {
                    return Option.None;
                }
            }

            if (sp.Length == 2 && StringModule.AllNotEmptyOrWhiteSpace(sp)) return TryParse2(input);
            else return sp.Length == 4 && StringModule.AllNotEmptyOrWhiteSpace(sp) ? TryParse4() : (Option<WebProxy>)Option.None;
        }
    }
}
