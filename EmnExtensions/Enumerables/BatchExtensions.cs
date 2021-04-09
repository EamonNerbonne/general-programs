using System;
using System.Collections.Generic;

namespace EmnExtensions.Enumerables
{
    public static class BatchExtension
    {
        static IEnumerable<T[]> Batch<T>(this IEnumerable<T> list, int batchSize)
        {
            var i = 0;
            var arr = new T[batchSize];
            foreach (var t in list) {
                arr[i++] = t;
                if (i == batchSize) {
                    yield return arr;
                    i = 0;
                    arr = new T[batchSize];
                }
            }
            if (i > 0) {
                Array.Resize(ref arr, i);
                yield return arr;
            }
        }
    }
}
