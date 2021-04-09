using System;
using System.Collections.Generic;

namespace EmnExtensions
{
    public static class IndexOfExtension
    {
        public static int IndexOf<T>(this IEnumerable<T> list, Func<T, bool> pred)
        {
            var index = 0;
            foreach (var item in list) {
                if (pred(item)) {
                    return index;
                }

                index++;
            }
            return -1;
        }
    }
}
