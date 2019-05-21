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
    public static class SeqModule
    {
        public static IEnumerable<T> Empty<T>() => new List<T>(0);

        public static int Len<T>(this IEnumerable<T> seq) => seq.Count();

        public static T Random<T>(this IEnumerable<T> seq)
        {
            var len = seq.Len();
            var tmp = new ReadOnlyCollection<T>(new List<T>(seq));
            return tmp[RandomModule.Next(len)];
        }

        /// <summary>
        /// For each function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <param name="action"></param>
        public static void Iter<T>(this IEnumerable<T> seq, Action<T> action)
        {
            foreach (var value in seq)
                action(value);
        }

        /// <summary>
        /// Applies chooser to each item the sequence. If chooser returns Some, item is added to result. If chooser returns None, item is discarded.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="seq"></param>
        /// <param name="chooser"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> Choose<T, TResult>(
            this IEnumerable<T> seq, 
            Func<T, Option<TResult>> chooser)
        {
            return
                seq
                .Select(chooser)
                .Where(Option.IsSome)
                .Select(Option.Value);
        }

        public static IEnumerable<string> OfFile(string path)
            => File.ReadLines(path);

        public static Option<T> TryFind<T>(this IEnumerable<T> seq, Func<T, bool> predicate)
        {
            foreach (var item in seq)
            {
                if (predicate(item))
                    return item;
            }

            return Option.None;
        }
    }

    /// <summary>
    /// <see cref="List{T}"/> utility functions.
    /// </summary>
    public static class ListModule
    {
        /// <summary>
        /// Attempts to cast the sequence to <see cref="List{T}"/>. If that fails, creates a new <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static List<T> OfSeq<T>(IEnumerable<T> sequence)
        {
            return sequence is List<T> list ? list : sequence.ToList();
        }
    }

    /// <summary>
    /// <see cref="ReadOnlyCollection{T}"/> utility functions.
    /// </summary>
    public static class ReadOnlyCollectionModule
    {
        public static ReadOnlyCollection<T> OfSeq<T>(IEnumerable<T> sequence)
        {
            return sequence is ReadOnlyCollection<T> r ? r : new ReadOnlyCollection<T>(ListModule.OfSeq(sequence));
        }
    }

    /// <summary>
    /// <see cref="IDictionary{TKey, TValue}"/> utility functions
    /// </summary>
    public static class DictModule
    {
        /// <summary>
        /// Creates a new <see cref="Dictionary{TKey, TValue}"/> from sequence of <see cref="ValueTuple"/>s
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="equalityComparer"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> OfSeq<TKey, TValue>(
            IEnumerable<(TKey, TValue)> sequence,
            IEqualityComparer<TKey> equalityComparer)
        {
            var d = new Dictionary<TKey, TValue>(sequence.Len(), equalityComparer);
            foreach (var (key, value) in sequence)
                d.Add(key, value);
            return d;
        }

        public static Dictionary<TKey, TValue> OfSeq<TKey, TValue>(
            IEnumerable<(TKey, TValue)> sequence)
        {
            return OfSeq(sequence, EqualityComparer<TKey>.Default);
        }

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
                dict[key] = value;
            else
                dict.Add(key, value);
        }

        public static IEnumerable<(TKey key, TValue value)> ToSeq<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            var lst = new List<(TKey k, TValue v)>(dict.Count);
            foreach (var kvp in dict)
                lst.Add((kvp.Key, kvp.Value));
            return lst;
        }

        public static Option<TValue> TryGet<TKey, TValue>(
            this IDictionary<TKey, TValue> dict,
            TKey key)
        {
            return dict.TryGetValue(key, out var value) ? new Option<TValue>(value) : Option.None;
        }
    }

    /// <summary>
    /// <see cref="IReadOnlyDictionary{TKey, TValue}{TKey, TValue}"/> utility functions.
    /// </summary>
    public static class ReadOnlyDictModule
    {
        /// <summary>
        /// Creates a new <see cref="Dictionary{TKey, TValue}"/> from sequence of <see cref="ValueTuple"/>s
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="equalityComparer"></param>
        /// <returns></returns>
        public static ReadOnlyDictionary<TKey, TValue> OfSeq<TKey, TValue>(
            IEnumerable<(TKey, TValue)> sequence,
            IEqualityComparer<TKey> equalityComparer
        ) => new ReadOnlyDictionary<TKey, TValue>(DictModule.OfSeq(sequence, equalityComparer));

        public static ReadOnlyDictionary<TKey, TValue> OfSeq<TKey, TValue>(
            IEnumerable<(TKey, TValue)> sequence
        ) => OfSeq(sequence, EqualityComparer<TKey>.Default);

        public static ReadOnlyDictionary<TKey, TValue> OfDict<TKey, TValue>(
            IDictionary<TKey, TValue> d
        ) => new ReadOnlyDictionary<TKey, TValue>(d);

        public static Option<TValue> TryGetValue<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dict,
            TKey key)
        {
            return dict.TryGetValue(key, out TValue value) ? new Option<TValue>(value) : Option.None;
        }
    }

    /// <summary>
    /// <see cref="Queue{T}"/> utility functions
    /// </summary>
    public static class QueueModule
    {
        public static T DequeueEnqueue<T>(this Queue<T> queue)
        {
            var item = queue.Dequeue();
            queue.Enqueue(item);
            return item;
        }

        public static Queue<T> OfSeq<T>(IEnumerable<T> seq) => new Queue<T>(seq);

        public static void Shuffle<T>(this Queue<T> queue)
        {
            var list = queue.ToList();
            queue.Clear();

            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = RandomModule.Next(n + 1);
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
    public static class CollectionModule
    {
        public static void Shuffle<T>(this ICollection<T> collection)
        {
            var list = new List<T>(collection);
            collection.Clear();

            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = RandomModule.Next(n + 1);
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
    public static class ArrayModule
    {
        /// <summary>
        /// Attempts to cast the sequence to <see cref="T:T[]"/>. If that fails, creates a new<see cref="T:T[]"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static T[] OfSeq<T>(IEnumerable<T> sequence)
        {
            return sequence is T[] array ? array : sequence.ToArray();
        }
    }
}
