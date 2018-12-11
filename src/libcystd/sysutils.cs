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
    public static class IDisposableUtils
    {
        public static void DisposeSeq(IEnumerable<IDisposable> disposables)
        {
            foreach (var d in disposables) d.Dispose();
        }
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
        public static void InvalidOp(string message)
        {
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// throws a new <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public static void InvalidOp(string message, Exception inner)
        {
            throw new InvalidOperationException(message, inner);
        }

        /// <summary>
        /// throws a new <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="paramName"></param>
        public static void InvalidArg(string message, string paramName)
        {
            throw new ArgumentException(message, paramName);
        }

        /// <summary>
        /// throws a new <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="paramName"></param>
        public static void NullArg(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Throws a new <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="message"></param>
        public static void Timeout(string message)
        {
            throw new TimeoutException(message);
        }

        /// <summary>
        /// Throws a new <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public static void Timeout(string message, Exception inner)
        {
            throw new TimeoutException(message, inner);
        }

        /// <summary>
        /// Reraises <see cref="Exception"/> with stack trace intact.
        /// </summary>
        /// <param name="e"></param>
        public static void Reraise(Exception e) => ExceptionDispatchInfo.Capture(e).Throw();

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
            var stack = $"STack trace: {ex.StackTrace}";

            var list = ReadOnlyCollectionUtils.OfSeq(new List<string>
            {
                src,
                @type,
                date,
                time,
                msg,
                stack
            });

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

        public static int Next(int min, int max)
        {
            lock (Rand) return Rand.Next(min, max);
        }

        public static int Next(int max)
        {
            lock (Rand) return Rand.Next(max);
        }
    }

    /// <summary>
    /// <see cref="DateTimeOffset"/> utility functions.
    /// </summary>
    public static class DateTimeOffsetUtils
    {
        public static DateTimeOffset Epoch { get; }

        static DateTimeOffsetUtils()
        {
            Epoch = new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        public static TimeSpan UnixTimeSpan(DateTimeOffset input) => input - Epoch;

        public static long UnixMillis() => (long)UnixTimeSpan(DateTimeOffset.Now).TotalMilliseconds;
    }

    public static class MemoryUtils
    {
        public static T[] AsArray<T>(this ReadOnlyMemory<T> memory)
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
        public static ReadOnlySpan<byte> ToBytesSpan(this string str) =>
            Encoding.UTF8.GetBytes(str).AsSpan();

        public static string OfReadOnlyMemory(ReadOnlyMemory<byte> bytes) =>
            Encoding.UTF8.GetString(bytes.AsArray());

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
    /// <see cref="Option"/> utility functions.
    /// </summary>
    public static class OptionUtils
    {
        public static async Task MatchSomeAsync<T>(this Option<T> option, Func<T, Task> action)
        {
            if (option.HasValue) await action(option.ValueOrFailure()).ConfigureAwait(false);
        }

        public static async Task MatchAsync<T>(this Option<T> option, Func<T, Task> some, Func<Task> none)
        {
            if (option.HasValue) await some(option.ValueOrFailure()).ConfigureAwait(false);
            else await none().ConfigureAwait(false);
        }

        public static async Task MatchAsync<T>(this Option<T> option, Func<T, Task> some, Action none)
        {
            if (option.HasValue) await some(option.ValueOrFailure()).ConfigureAwait(false);
            else none();
        }

        public static async Task<TResult> MatchAsync<TResult, T>(this Option<T> option, Func<T, Task<TResult>> someAction, Func<Task<TResult>> noneAction)
        {
            return
                option.HasValue
                ? await someAction(option.ValueOrFailure()).ConfigureAwait(false)
                : await noneAction().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// <see cref="ReadOnlyMemory{T}"/> utility functions
    /// </summary>
    public static class ReadOnlyMemoryUtils
    {
        public static ReadOnlyMemory<byte> OfString(string s) =>
            new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(s));

        public static ReadOnlyMemory<T> OfSeq<T>(IEnumerable<T> seq) =>
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
        public static Option<WebProxy> TryParse(string input)
        {
            var sp = input.Split(':');

            Option<WebProxy> TryParse2()
            {
                try { return Option.Some(new WebProxy(input)); }
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

            if (sp.Length == 2 && StringUtils.AllNotEmptyOrWhiteSpace(sp)) return TryParse2();
            else if (sp.Length == 4 && StringUtils.AllNotEmptyOrWhiteSpace(sp)) return TryParse4();
            else return Option.None<WebProxy>();
        }
    }
}