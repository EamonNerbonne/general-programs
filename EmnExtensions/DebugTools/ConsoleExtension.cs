using System;
using System.Collections.Generic;
using System.Text;

namespace EamonExtensions.DebugTools {
    public static class ConsoleExtension {
        public static void PrintAllDebug<T>(this IEnumerable<T> source) {
            foreach (var item in source)
                Console.WriteLine(item);
        }

        public static T PrintDebug<T>(this T obj) {
            Console.WriteLine(obj);
            Console.ReadKey();
            return obj;
        }
    }
}
