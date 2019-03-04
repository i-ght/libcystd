using LibCyStd.Seq;
using OneOf;
using OneOf.Types;
//using Optional;
//using Optional.Unsafe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LibCyStd
{
    public struct Option<TValue> : IEquatable<Option<TValue>>
    {
        private readonly OneOf<TValue, None> _value;

        public TValue Value
        {
            get
            {
                if (_value.IsT0)
                    return _value.AsT0;
                throw new InvalidOperationException("no value associated with this option.");
            }
        }

        public bool IsSome => _value.IsT0;
        public bool IsNone => _value.IsT1;

        public (bool success, TValue value) TryGetValue()
        {
            return _value.IsT0 ? (true, _value.AsT0) : ((bool success, TValue value))(false, default!);
        }

        public Option(in TValue value) => _value = value;
        public Option(in None none) => _value = none;

        public void Switch(Action<TValue> f0, Action<None> f1) => _value.Switch(f0, f1);
        public TResult Match<TResult>(Func<TValue, TResult> f0, Func<None, TResult> f1) => _value.Match(f0, f1);

        public override bool Equals(object obj)
        {
            return !(obj is Option<TValue> opt) ? false : Equals(opt);
        }

        public override int GetHashCode()
        {
            var hashCode = 2067055381;
            hashCode = hashCode * -1521134295 + EqualityComparer<OneOf<TValue, None>>.Default.GetHashCode(_value);
            hashCode = hashCode * -1521134295 + EqualityComparer<TValue>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + IsSome.GetHashCode();
            hashCode = hashCode * -1521134295 + IsNone.GetHashCode();
            return hashCode;
        }

        public bool Equals(Option<TValue> other)
        {
            return EqualityComparer<OneOf<TValue, None>>.Default.Equals(_value, other._value) &&
                   EqualityComparer<TValue>.Default.Equals(Value, other.Value) &&
                   IsSome == other.IsSome &&
                   IsNone == other.IsNone;
        }

        public override string ToString()
        {
            return _value.IsT0 ? $"Some {_value.AsT0}" : "None";
        }
        public static implicit operator Option<TValue>(in TValue value) => new Option<TValue>(value);
        public static implicit operator Option<TValue>(in None none) => new Option<TValue>(none);
    }

    public static class OptionModule
    {
        public static bool IsSome<TValue>(in Option<TValue> option) => option.IsSome;
        public static bool IsSome<TValue>(Option<TValue> option) => IsSome(in option);
        public static bool IsNone<TValue>(in Option<TValue> option) => option.IsNone;
        public static bool IsNone<TValue>(Option<TValue> option) => IsNone(in option);
        public static TValue Value<TValue>(in Option<TValue> option) => option.Value;
        public static TValue Value<TValue>(Option<TValue> option) => Value(in option);
        public static Option<TValue> Some<TValue>(in TValue value) => new Option<TValue>(value);
        public static Option<TValue> None<TValue>() => new Option<TValue>(OneOf.Types.None.Value);
    }

    /// <summary>
    /// <see cref="IDisposable"/> utility functions.
    /// </summary>
    public static class DisposableModule
    {
        public static void Dispose(in IDisposable d) => d.Dispose();
        public static void Dispose(IDisposable d) => Dispose(in d);

        public static void DisposeSeq(in IEnumerable<IDisposable> disposables) =>
            disposables.Iter(Dispose);
    }

    public static class SysModule
    {
        /// <summary>
        /// Identity function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Id<T>(in T value) => value;
        public static T Id<T>(T value) => Id(in value);

        public static string ToString<T>(in T obj) => obj!.ToString();

        public static string ToString<T>(T obj) => ToString(in obj);
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
        public static void InvalidOp(in string message)
        {
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// throws a new <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public static void InvalidOp(in string message, in Exception inner)
        {
            throw new InvalidOperationException(message, inner);
        }

        /// <summary>
        /// throws a new <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="paramName"></param>
        public static void InvalidArg(in string message, in string paramName)
        {
            throw new ArgumentException(message, paramName);
        }

        /// <summary>
        /// throws a new <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="paramName"></param>
        public static void NullArg(in string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Throws a new <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="message"></param>
        public static void Timeout(in string message)
        {
            throw new TimeoutException(message);
        }

        /// <summary>
        /// Throws a new <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public static void Timeout(in string message, in Exception inner)
        {
            throw new TimeoutException(message, inner);
        }

        /// <summary>
        /// Reraises <see cref="Exception"/> with stack trace intact.
        /// </summary>
        /// <param name="e"></param>
        public static void Reraise(in Exception e) => ExceptionDispatchInfo.Capture(e).Throw();

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

            var list = ReadOnlyCollectionModule.OfSeq(
                ListModule.OfSeq(
                    new[]
                    {
                        src,
                        @type,
                        date,
                        time,
                        msg,
                        stack
                    }
                )
            );

            return string.Join(Environment.NewLine, list);
        }

        public static void NotImpl()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Thread safe <see cref="Random"/> utility functions.
    /// </summary>
    public static class RandomModule
    {
        private static readonly Random Rand;

        static RandomModule()
        {
            Rand = new Random();
        }

        public static int Next(in int min, in int max)
        {
            lock (Rand) return Rand.Next(min, max);
        }

        public static int Next(in int max)
        {
            lock (Rand) return Rand.Next(max);
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
        public static TimeZoneInfo UsMountain { get; }
        public static TimeZoneInfo UsWest { get; }

        static DateTimeOffsetModule()
        {
            Epoch = new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            UsEast = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            UsCentral = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            UsMountain = TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");
            UsWest = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        }

        public static TimeSpan UnixTimeSpan(in DateTimeOffset input) => input - Epoch;

        public static long UnixMillis() => (long)UnixTimeSpan(DateTimeOffset.Now).TotalMilliseconds;

        public static long UnixSeconds() => (long)UnixTimeSpan(DateTimeOffset.Now).TotalSeconds;

        public static DateTimeOffset DateTimeInTimeZone(in DateTimeOffset dt, in TimeZoneInfo tzInfo)
            => TimeZoneInfo.ConvertTime(dt, tzInfo);
    }

    public static class ReadOnlyMemoryModule
    {
        public static string ToHex(this in ReadOnlyMemory<byte> bytes)
        {
            if (bytes.IsEmpty)
                return "";
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes.Span)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static ReadOnlyMemory<byte> OfHex(in string hex)
        {
            var h = Regex.Replace(hex, @"\s+", "");
            return
                Enumerable.Range(0, h.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(h.Substring(x, 2), 16))
                .ToArray();
        }

        public static ReadOnlyMemory<byte> OfString(in string s) => Encoding.UTF8.GetBytes(s);

        public static ReadOnlyMemory<T> OfSeq<T>(in IEnumerable<T> seq) => ArrayModule.OfSeq(seq);

        public static (bool success, ArraySegment<T> arrSeg) TryGetArraySegment<T>(this in ReadOnlyMemory<T> memory)
            => MemoryMarshal.TryGetArray(memory, out var segment) ? (true, segment) : (false, segment);

        public static ArraySegment<T> AsArraySeg<T>(this in ReadOnlyMemory<T> memory)
        {
            var (success, seg) = TryGetArraySegment(memory);
            if (!success)
                ExnModule.InvalidOp("memory did not contain array");
            return seg;
        }

        public static T[] AsArray<T>(this in ReadOnlyMemory<T> memory)
        {
            var seg = AsArraySeg(memory);
            return seg.Array;
        }

        public static string ToBase64EncodedString(this in ReadOnlyMemory<byte> bytes)
            => Convert.ToBase64String(AsArray(bytes));

        public static ReadOnlyMemory<byte> OfBase64EncodedString(in string input)
            => Convert.FromBase64String(input);
    }

    /// <summary>
    /// <see cref="string"/> utility functions.
    /// </summary>
    public static class StringModule
    {
        public static string[] SplitRemoveEmpty(this string input, in char delimter)
            => input.Split(new[] { delimter }, StringSplitOptions.RemoveEmptyEntries);

        public static string[] SplitRemoveEmpty(this string input, in string delimter)
            => input.Split(delimter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        public static string Trim(in string input) => input.Trim();
        public static string Trim(string input) => Trim(in input);

        public static Option<(string key, string val)> TryParseKvp(in string input, in string delimter)
        {
            if (string.IsNullOrWhiteSpace(input))
                return None.Value;

            var sp = SplitRemoveEmpty(input, delimter);
            return sp.Length != 2 ? (Option<(string key, string val)>)None.Value : (Option<(string key, string val)>)(sp[0], sp[1]);
        }

        public static Option<(string key, string val)> TryParseKvp(in string input, in char delimter)
        {
            if (string.IsNullOrWhiteSpace(input))
                return None.Value;

            var sp = SplitRemoveEmpty(input, delimter);
            return sp.Length != 2 ? (Option<(string key, string val)>)None.Value : (Option<(string key, string val)>)(sp[0], sp[1]);
        }

        public static unsafe string OfSpan(in ReadOnlySpan<byte> bytes)
        {
            fixed (byte* b = bytes)
            {
                return Encoding.UTF8.GetString(b, bytes.Length);
            }
        }

        public static unsafe string OfMemory(in ReadOnlyMemory<byte> bytes)
            => OfSpan(bytes.Span);

        public static bool InvariantEquals(this string str1, in string str2) =>
            string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);

        public static bool InvariantStartsWith(this string input, in string value) =>
            input.StartsWith(value, StringComparison.OrdinalIgnoreCase);

        public static bool InvariantEndsWith(this string input, in string value) =>
            input.EndsWith(value, StringComparison.OrdinalIgnoreCase);

        public static bool InvariantContains(this string input, in string value) =>
            input.IndexOf(value, StringComparison.OrdinalIgnoreCase) > -1;

        public static bool AllNotEmptyOrWhiteSpace(in IEnumerable<string> strings) =>
            strings.All(s => !string.IsNullOrWhiteSpace(s));

        public static bool AnyEmptyOrWhiteSpace(in IEnumerable<string> strings) =>
            strings.Any(string.IsNullOrWhiteSpace);
    }

    ///// <summary>
    ///// <see cref="Option"/> utility functions.
    ///// </summary>
    //public static class OptionUtils
    //{
    //    public static bool HasValue<T>(this in Option<T> opt) => opt.HasValue;
    //    public static bool HasValue<T>(this Option<T> opt) => HasValue(in opt);

    //    public static T Value<T>(this in Option<T> opt) => opt.ValueOrFailure();
    //    public static T Value<T>(this Option<T> opt) => Value(in opt);

    //    public static Task MatchSomeAsync<T>(this in Option<T> option, in Func<T, Task> action)
    //    {
    //        if (option.HasValue) return action(option.ValueOrFailure());
    //        else return Task.CompletedTask;
    //    }

    //    public static Task MatchAsync<T>(this in Option<T> option, in Func<T, Task> some, in Func<Task> none)
    //    {
    //        if (option.HasValue) return some(option.ValueOrFailure());
    //        else return none();
    //    }

    //    public static Task MatchAsync<T>(this in Option<T> option, in Func<T, Task> some, in Action none)
    //    {
    //        if (option.HasValue)
    //        {
    //            return some(option.ValueOrFailure());
    //        }
    //        else
    //        {
    //            none();
    //            return Task.CompletedTask;
    //        }
    //    }

    //    public static Task<TResult> MatchAsync<TResult, T>(this in Option<T> option, in Func<T, Task<TResult>> someAction, Func<Task<TResult>> noneAction)
    //    {
    //        return
    //            option.HasValue
    //            ? someAction(option.ValueOrFailure())
    //            : noneAction();
    //    }
    //}

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
        public static Option<WebProxy> TryParse(in string input)
        {
            var sp = input.SplitRemoveEmpty(':');

            Option<WebProxy> TryParse2(in string _input)
            {
                try { return new WebProxy(_input); }
                catch (Exception e) when (e is ArgumentException || e is UriFormatException)
                {
                    return None.Value;
                }
            }

            Option<WebProxy> TryParse4()
            {
                var (hostPort, username, pw) = ($"{sp[0]}:{sp[1]}", sp[2], sp[3]);
                var cred = new NetworkCredential(username, pw);

                try { return new WebProxy(hostPort) { Credentials = cred }; }
                catch (Exception e) when (e is ArgumentException || e is UriFormatException)
                {
                    return None.Value;
                }
            }

            if (sp.Length == 2 && StringModule.AllNotEmptyOrWhiteSpace(sp)) return TryParse2(input);
            else return sp.Length == 4 && StringModule.AllNotEmptyOrWhiteSpace(sp) ? TryParse4() : (Option<WebProxy>)None.Value;
        }
    }
}
