using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Collections
{

    public class Heap<T>
        where T : IComparable<T>
    {
        T[] backingStore = new T[8];
        int backingCount = 0;
        Action<T, int> indexSet;
        public Heap(Action<T, int> indexSet) {
            this.indexSet = indexSet;
        }
        public void Add(T elem) {
            int newIndex = backingCount;
            if (backingCount == backingStore.Length)
                Array.Resize(ref backingStore, backingStore.Length * 2);
            backingStore[backingCount++]=elem;
            Bubble(newIndex, elem);
        }


        public int Count { get { return backingCount; } }

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
            if (backingCount == 0) {
                retval = default(T);
                return false;
            }
            retval = backingStore[0];
            Delete(0);

            return true;
        }
        public void Delete(int indexOfItem) {
            T toSink = backingStore[--backingCount];
            if(backingCount>indexOfItem)
            Sink(indexOfItem, toSink);
        }

        private void Sink(int p, T elem) {
            while (2 * p + 1 < backingCount) {
                int kid = 2 * p + 2 < backingCount ?
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
