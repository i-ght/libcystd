using Optional;
using Optional.Unsafe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace LibCyStd.Seq
{
    /// <summary>
    /// <see cref="IEnumerable{T}"/> utility functions.
    /// </summary>
    public static class SeqUtils
    {
        public static int Len<T>(this IEnumerable<T> seq) => seq.Count();

        public static T Random<T>(this IEnumerable<T> seq)
        {
            var len = seq.Len();
            var tmp = new ReadOnlyCollection<T>(new List<T>(seq));
            return tmp[RandomUtil.Next(len)];
        }

        /// <summary>
        /// Applies chooser to each item in the sequence. If chooser returns Some, item is added to result. If chooser returns None, item is discarded.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="seq"></param>
        /// <param name="chooser"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> Choose<T, TResult>(
            this IEnumerable<T> seq, in Func<T, Option<TResult>> chooser)
        {
            return
                seq.Select(chooser)
                .Where(item => item.HasValue)
                .Select(opt => opt.ValueOrFailure());
        }

        public static IEnumerable<string> OfFile(in string path)
            => File.ReadLines(path);

        public static Option<T> TryFind<T>(this IEnumerable<T> seq, Func<T, bool> predicate)
        {
            foreach (var item in seq)
            {
                if (predicate(item))
                    return item.Some();
            }

            return Option.None<T>();
        }
    }

    /// <summary>
    /// <see cref="List{T}"/> utility functions.
    /// </summary>
    public static class ListUtils
    {
        /// <summary>
        /// Attempts to cast the sequence to <see cref="List{T}"/>. If that fails, creates a new <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static List<T> OfSeq<T>(in IEnumerable<T> sequence)
        {
            if (sequence is List<T> list) return list;
            else return sequence.ToList();
        }
    }

    /// <summary>
    /// <see cref="ReadOnlyCollection{T}"/> utility functions.
    /// </summary>
    public static class ReadOnlyCollectionUtils
    {
        public static ReadOnlyCollection<T> OfSeq<T>(in IEnumerable<T> sequence)
        {
            if (sequence is ReadOnlyCollection<T> r) return r;
            else return new ReadOnlyCollection<T>(ListUtils.OfSeq(sequence));
        }
    }

    /// <summary>
    /// <see cref="IDictionary{TKey, TValue}"/> utility functions
    /// </summary>
    public static class DictUtils
    {
        /// <summary>
        /// Creates a new <see cref="Dictionary{TKey, TValue}"/> from sequence of <see cref="ValueTuple"/>s
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static IDictionary<TKey, TValue> OfSeq<TKey, TValue>(
            in IEnumerable<(TKey, TValue)> sequence,
            in IEqualityComparer<TKey> equalityComparer)
        {
            var d = new Dictionary<TKey, TValue>(sequence.Len(), equalityComparer);
            foreach (var (key, value) in sequence) d.Add(key, value);
            return d;
        }

        public static IDictionary<TKey, TValue> OfSeq<TKey, TValue>(
            in IEnumerable<(TKey, TValue)> sequence)
        {
            return OfSeq(sequence, EqualityComparer<TKey>.Default);
        }

        public static Option<TValue> TryGetValue<TKey, TValue>(
            this IDictionary<TKey, TValue> dict,
            in TKey key)
        {
            if (dict.TryGetValue(key, out var value)) return Option.Some(value);
            else return Option.None<TValue>();
        }
    }

    /// <summary>
    /// <see cref="IReadOnlyDictionary{TKey, TValue}{TKey, TValue}"/> utility functions.
    /// </summary>
    public static class ReadOnlyDictUtils
    {
        /// <summary>
        /// Creates a new <see cref="Dictionary{TKey, TValue}"/> from sequence of <see cref="ValueTuple"/>s
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static ReadOnlyDictionary<TKey, TValue> OfSeq<TKey, TValue>(
            in IEnumerable<(TKey, TValue)> sequence,
            in IEqualityComparer<TKey> equalityComparer
        ) => new ReadOnlyDictionary<TKey, TValue>(DictUtils.OfSeq(sequence, equalityComparer));

        public static ReadOnlyDictionary<TKey, TValue> OfSeq<TKey, TValue>(
            in IEnumerable<(TKey, TValue)> sequence
        ) => OfSeq(sequence, EqualityComparer<TKey>.Default);

        public static ReadOnlyDictionary<TKey, TValue> OfDict<TKey, TValue>(
            in IDictionary<TKey, TValue> d
        ) => new ReadOnlyDictionary<TKey, TValue>(d);

        public static Option<TValue> TryGetValue<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dict,
            in TKey key)
        {
            if (dict.TryGetValue(key, out var value)) return Option.Some(value);
            else return Option.None<TValue>();
        }
    }

    /// <summary>
    /// <see cref="Queue{T}"/> utility functions
    /// </summary>
    public static class QueueUtils
    {
        public static T DequeueEnqueue<T>(this Queue<T> queue)
        {
            var item = queue.Dequeue();
            queue.Enqueue(item);
            return item;
        }

        public static Queue<T> OfSeq<T>(in IEnumerable<T> seq) => new Queue<T>(seq);

        public static void Shuffle<T>(this Queue<T> queue)
        {
            var list = queue.ToList();
            queue.Clear();

            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = RandomUtil.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            foreach (var item in list)
                queue.Enqueue(item);
        }
    }

    /// <summary>
    /// <see cref="ICollection{T}"/> utility functions.
    /// </summary>
    public static class CollectionUtils
    {
        public static void Shuffle<T>(this ICollection<T> collection)
        {
            var list = new List<T>(collection);
            collection.Clear();

            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = RandomUtil.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            foreach (var item in list)
                collection.Add(item);
        }
    }

    /// <summary>
    /// array[] utility functions.
    /// </summary>
    public static class ArrayUtils
    {
        /// <summary>
        /// Attempts to cast the sequence to <see cref="T:T[]"/>. If that fails, creates a new<see cref="T:T[]"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static T[] OfSeq<T>(in IEnumerable<T> sequence)
        {
            if (sequence is T[] array) return array;
            else return sequence.ToArray();
        }
    }
}