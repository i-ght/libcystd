using LibCyStd.Seq;
using Optional;
using Optional.Unsafe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibCyStd
{
    /// <summary>
    /// <see cref="IDisposable"/> utility functions.
    /// </summary>
    public static class DisposableUtils
    {
        public static void Dispose(in IDisposable d) => d.Dispose();
        public static void Dispose(IDisposable d) => Dispose(in d);

        public static void DisposeSeq(in IEnumerable<IDisposable> disposables)
        {
            foreach (var d in disposables)
                d.Dispose();
        }
    }

    public static class SysUtils
    {
        /// <summary>
        /// Identity function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Id<T>(in T value) => value;
        public static T Id<T>(T value) => Id(in value);

        public static string ToString<T>(in T obj) => obj.ToString();
        public static string ToString<T>(T obj) => ToString(in obj);
    }

    /// <summary>
    /// <see cref="Exception"/> utility functions.
    /// </summary>
    public static class ExnUtils
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

            var list = ReadOnlyCollectionUtils.OfSeq(
                ListUtils.OfSeq(
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
    }

    /// <summary>
    /// Thread safe <see cref="Random"/> utility functions.
    /// </summary>
    public static class RandomUtil
    {
        private static readonly Random Rand;

        static RandomUtil()
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
    public static class DateTimeOffsetUtils
    {
        public static readonly DateTimeOffset Epoch;

        static DateTimeOffsetUtils()
        {
            Epoch = new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        public static TimeSpan UnixTimeSpan(in DateTimeOffset input) => input - Epoch;

        public static long UnixMillis() => (long)UnixTimeSpan(DateTimeOffset.Now).TotalMilliseconds;
    }

    public static class MemoryUtils
    {
        public static T[] AsArray<T>(this in ReadOnlyMemory<T> memory)
        {
            if (!MemoryMarshal.TryGetArray(memory, out var segment))
                ExnUtils.InvalidOp("memory did not contain array");
            return segment.Array;
        }
    }

    /// <summary>
    /// <see cref="string"/> utility functions.
    /// </summary>
    public static class StringUtils
    {
        public static string[] SplitRemoveEmpty(this string input, in char delimter)
            => input.Split(new[] { delimter }, StringSplitOptions.RemoveEmptyEntries);
        

        public static string[] SplitRemoveEmpty(this string input, in string delimter)
            => input.Split(delimter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        public static string Trim(in string input) => input.Trim();
        public static string Trim(string input) => Trim(in input);

        public static Option<(string key, string val)> TryParseKvp(in string input, in string delimter)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Option.None<(string key, string val)>();

            var sp = SplitRemoveEmpty(input, delimter);
            if (sp.Length != 2)
                return Option.None<(string key, string val)>();

            return (sp[0], sp[1]).Some();
        }

        public static Option<(string key, string val)> TryParseKvp(in string input, in char delimter)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Option.None<(string key, string val)>();

            var sp = SplitRemoveEmpty(input, delimter);
            if (sp.Length != 2)
                return Option.None<(string key, string val)>();

            return (sp[0], sp[1]).Some();
        }

        public static ReadOnlySpan<byte> ToBytesSpan(this string str) =>
            Encoding.UTF8.GetBytes(str).AsSpan();

        public static string OfReadOnlyMemory(in ReadOnlyMemory<byte> bytes) =>
            Encoding.UTF8.GetString(bytes.AsArray());

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

    /// <summary>
    /// <see cref="Option"/> utility functions.
    /// </summary>
    public static class OptionUtils
    {
        public static bool HasValue<T>(this in Option<T> opt) => opt.HasValue;
        public static bool HasValue<T>(this Option<T> opt) => HasValue(in opt);

        public static T Value<T>(this in Option<T> opt) => opt.ValueOrFailure();
        public static T Value<T>(this Option<T> opt) => Value(in opt);

        public static Task MatchSomeAsync<T>(this in Option<T> option, in Func<T, Task> action)
        {
            if (option.HasValue) return action(option.ValueOrFailure());
            else return Task.CompletedTask;
        }

        public static Task MatchAsync<T>(this in Option<T> option, in Func<T, Task> some, in Func<Task> none)
        {
            if (option.HasValue) return some(option.ValueOrFailure());
            else return none();
        }

        public static Task MatchAsync<T>(this in Option<T> option, in Func<T, Task> some, in Action none)
        {
            if (option.HasValue)
            {
                return some(option.ValueOrFailure());
            }
            else
            {
                none();
                return Task.CompletedTask;
            }
        }

        public static Task<TResult> MatchAsync<TResult, T>(this in Option<T> option, in Func<T, Task<TResult>> someAction, Func<Task<TResult>> noneAction)
        {
            return
                option.HasValue
                ? someAction(option.ValueOrFailure())
                : noneAction();
        }
    }

    /// <summary>
    /// <see cref="ReadOnlyMemory{T}"/> utility functions
    /// </summary>
    public static class ReadOnlyMemoryUtils
    {
        public static ReadOnlyMemory<byte> OfString(in string s) =>
            new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(s));

        public static ReadOnlyMemory<T> OfSeq<T>(in IEnumerable<T> seq) =>
            new ReadOnlyMemory<T>(ArrayUtils.OfSeq(seq));
    }

    /// <summary>
    /// <see cref="WebProxy"/> utility functions.
    /// </summary>
    public static class WebProxyUtils
    {
        /// <summary>
        /// Tries to parse input to a <see cref="WebProxy"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Option<WebProxy> TryParse(in string input)
        {
            var sp = StringUtils.SplitRemoveEmpty(input, ':');

            Option<WebProxy> TryParse2(in string _input)
            {
                try { return Option.Some(new WebProxy(_input)); }
                catch (Exception e) when (e is ArgumentException || e is UriFormatException)
                {
                    return Option.None<WebProxy>();
                }
            }

            Option<WebProxy> TryParse4()
            {
                var (hostPort, username, pw) = ($"{sp[0]}:{sp[1]}", sp[2], sp[3]);
                var cred = new NetworkCredential(username, pw);

                try { return Option.Some(new WebProxy(hostPort) { Credentials = cred }); }
                catch (Exception e) when (e is ArgumentException || e is UriFormatException)
                {
                    return Option.None<WebProxy>();
                }
            }

            if (sp.Length == 2 && StringUtils.AllNotEmptyOrWhiteSpace(sp)) return TryParse2(input);
            else if (sp.Length == 4 && StringUtils.AllNotEmptyOrWhiteSpace(sp)) return TryParse4();
            else return Option.None<WebProxy>();
        }
    }
}
