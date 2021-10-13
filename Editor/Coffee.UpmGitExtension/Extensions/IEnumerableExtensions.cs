using System;
using System.Collections.Generic;
using System.Linq;

namespace Coffee.UpmGitExtension
{
    internal static class IEnumerableExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var set = new HashSet<TKey>();

            foreach (var item in source)
            {
                var key = keySelector(item);
                if (set.Add(key))
                    yield return item;
            }
        }

        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext)
        {
            foreach (var item in source)
                onNext(item);
        }

        public static string Dump<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector)
        {
            return string.Join(", ", source.Select(x => x == null ? "null" : selector(x)?.ToString() ?? "null"));
        }

        public static string Dump<TSource>(this IEnumerable<TSource> source)
        {
            return string.Join(", ", source.Select(x => x?.ToString() ?? "null"));
        }

        public static string Dump(this IEnumerable<string> source)
        {
            return string.Join(", ", source);
        }
    }
}

