using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace EamonExtensionsLinq.DebugTools {
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

        public static T PrintProperties<T>(this T obj, string name) {

            Console.WriteLine("Properties of '" + name + "' typed " + typeof(T).FullName + ":");
            if (obj == null) {
                Console.WriteLine("   --- is null!");
            } else {
                Type runtimeType = obj.GetType();
                Type compileType = typeof(T);
                if (runtimeType != compileType) Console.WriteLine("  runtime-type: " + runtimeType.FullName);
                string stringrep;
                try {
                    stringrep = obj.ToString();
                } catch (Exception e) {
                    stringrep = e.GetType().Name + ": " + e.Message;
                }
                Console.WriteLine("ToString(): " + stringrep);

                HashSet<string> printedvals = new HashSet<string>();
                foreach (var prop in from type in new Type[] { runtimeType, compileType }
                                     from pi in type.GetProperties()
                                     where pi.CanRead && pi.GetIndexParameters().Length == 0
                                     select new { Prop = pi, ContainingType = type }) {
                    string propname = prop.Prop.Name;
                    string val = null;
                    try {
                        val = prop.Prop.GetValue(obj, null).ToStringOrNull() ?? "<null>";
                    } catch (Exception e) {
                        val = e.GetType().Name + ": " + (e.Message ?? "");
                    }
                    string toprint = propname + ": " + val;
                    if (printedvals.Add(toprint)) Console.WriteLine(prop.Prop.DeclaringType.Name + "." + toprint);
                }
                foreach (var field in from type in new Type[] { runtimeType, compileType }
                                      from fi in type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                                      select new { FieldInfo = fi, ContainingType = type }) {
                    string fieldname = field.FieldInfo.Name;
                    string val = null;
                    try {
                        val = field.FieldInfo.GetValue(obj).ToStringOrNull() ?? "<null>";
                    } catch (Exception e) {
                        val = e.GetType().Name + ": " + (e.Message ?? "");
                    }
                    string toprint = fieldname + ": " + val;
                    if (printedvals.Add(toprint)) Console.WriteLine(field.FieldInfo.DeclaringType.Name + "." + toprint);
                }

            }
            return obj;
        }
    }
}
