using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace EamonExtensionsLinq {
    public static class FuncUtil {
        public static IEnumerable<IEnumerable<T>> SplitWhen<T>(this IEnumerable<T> iter, Func<T,bool> splitMark) {
            var queue = new Queue<T>();
            foreach(T t in iter) {
                if(splitMark(t)) {
                    if(queue.Count != 0 ) {
                        yield return queue;
                    }
                    queue = new Queue<T>();
                }
                queue.Enqueue(t);
            }
            if(queue.Count != 0) {
                yield return queue;
            }
        }
        public static T Swallow<T>(Func<T> trial, Func<T> error) {
            try {
                return trial();
            } catch (Exception) {
                return error();
            }
        }

        //toy function:
        public static IEnumerable<T> Concat2<T>(IEnumerable<T> a,IEnumerable<T> b) {
            return IE<T>.E + a + b;
        }

    }

    public struct IE<T>:IEnumerable<T> {
        IEnumerable<T> wrapped;
        public static IE<T> E {  get {  return new IE<T>(empty()); }    }
        private static IEnumerable<T> empty() {yield break;}
        private IE(IEnumerable<T> toWrap) {
            wrapped= toWrap;
        }

        //public static explicit operator IE<T>(IEnumerable<T> a){return new IE<T>(a);}

        public static IE<T> operator +(IE<T> a,IEnumerable<T> b) {
            return new IE<T>(Concat(a,b));
        }
        private static IEnumerable<T> Concat(IE<T> a,IEnumerable<T> b) {
            foreach(T t in a) yield return t;
            foreach(T t in b) yield return t;
        }
        public IEnumerator<T> GetEnumerator() {
            return wrapped.GetEnumerator();
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return ((System.Collections.IEnumerable) wrapped).GetEnumerator();
        }
    }
}
