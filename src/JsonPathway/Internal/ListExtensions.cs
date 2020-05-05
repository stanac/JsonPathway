using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal
{
    internal static class ListExtensions
    {
        public static IEnumerable<T> GetByIndexes<T>(this List<T> list, int[] indexes)
        {
            if (list.Count == 0) yield break;

            List<int> normalizedIndexes = new List<int>();

            foreach (var i in indexes)
            {
                if (i >= 0 && i < list.Count) normalizedIndexes.Add(i);

                if (i < 0)
                {
                    var index = i;
                    while (index < 0)
                    {
                        index += list.Count;
                    }
                    normalizedIndexes.Add(index);
                }
            }

            foreach (var i in normalizedIndexes.Distinct())
            {
                yield return list[i];
            }
        }

        public static List<T> GetSlice<T>(this List<T> list, int? start, int? end, int? step)
            => GetSliceInner(list, start ?? 0, end ?? list.Count, step ?? 1);

        private static List<T> GetSliceInner<T>(List<T> list, int start, int end, int step)
        {
            if (step < 1) throw new ArgumentException("step must be > 0");

            while (start < 0) start += list.Count;
            while (end < 0) end += list.Count;

            List<T> result = new List<T>();

            for (int i = start; i < end; i += step)
            {
                if (i >= 0 && i < list.Count)
                {
                    result.Add(list[i]);
                }
            }

            return result;
        }
    }
}
