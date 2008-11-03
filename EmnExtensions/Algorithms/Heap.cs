using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Algorithms
{

    public class Heap<T>
        where T : IComparable<T>
    {
        List<T> backingStore = new List<T>();
        Action<T, int> indexSet;
        public Heap(Action<T, int> indexSet) {
            this.indexSet = indexSet;
        }
        public void Add(T elem) {
            int newIndex = backingStore.Count;
            backingStore.Add(elem);
            Bubble(newIndex, elem);
        }
        public int Count { get { return backingStore.Count; } }

        private void Bubble(int newIndex, T elem) {
            int parIndex = (newIndex - 1) / 2;
            while (newIndex != 0 && 0 < backingStore[parIndex].CompareTo(elem)) {
                backingStore[newIndex] = backingStore[parIndex];
                indexSet(backingStore[newIndex], newIndex);
                newIndex = parIndex;
                parIndex = (newIndex - 1) / 2;
            }
            backingStore[newIndex] = elem;
            indexSet(backingStore[newIndex], newIndex);
        }

        public bool RemoveTop(out T retval) {
            if (backingStore.Count == 0) {
                retval = default(T);
                return false;
            }
            retval = backingStore[0];
            Delete(0);

            return true;
        }
        public void Delete(int indexOfItem) {
            T toSink = backingStore[backingStore.Count - 1];
            backingStore.RemoveAt(backingStore.Count - 1);
            if(backingStore.Count>indexOfItem)
            Sink(indexOfItem, toSink);
        }

        private void Sink(int p, T elem) {
            while (2 * p + 1 < backingStore.Count) {
                int kid = 2 * p + 2 < backingStore.Count ?
                    (0 < backingStore[2 * p + 1].CompareTo(backingStore[2 * p + 2]) ? 2 * p + 2 : 2 * p + 1)  :
                    (2 * p + 1);
                if (0 >= elem.CompareTo(backingStore[kid])) {
                    break;
                } else {//elem isn't smaller than kid
                    indexSet(backingStore[p] = backingStore[kid], p);
                    p = kid;
                }
            }
            indexSet(backingStore[p] = elem, p);
        }

    }
}
