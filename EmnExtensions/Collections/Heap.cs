using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace EmnExtensions.Collections
{
    public interface IHeap<T>
    {
        void Add(T elem);
        int Count { get; }
        bool RemoveTop(out T retval);
        T RemoveTop();
        T Top();
        void TopChanged();
        T this[int i] { get; }
        void Delete(int indexOfItem);
        IEnumerable<T> ElementsInRoughOrder { get; }
    }

    public interface IHeapIndexSink<T>
    {
        void IndexChanged(T item, int newIndex);
    }

    public interface IHeapIndexed
    {
        void HeapIndexChanged(int newIndex);
    }

    public static class Heap
    {
        public static HeapFactory<T> Factory<T>()
            => new();

        public static IHeap<T> CreateSelfTracked<T>()
            where T : IComparable<T>, IHeapIndexed
            => new Heap<T, SelfSink<T>, ComparableComparer<T>>(new(), new());

        public static IHeap<T> CreateUntracked<T>()
            where T : IComparable<T>
            => new Heap<T, NoSink<T>, ComparableComparer<T>>(new(), new());

        public static IHeap<T> CreateTracked<T>(Action<T, int> indexChanged)
            where T : IComparable<T>
            => new Heap<T, DelegateSink<T>, ComparableComparer<T>>(CreateDelegateSink(indexChanged), new());

        public struct HeapFactory<T>
        {
            public IHeap<T> Create(Action<T, int> indexSet = null)
            {
                if (typeof(T) == typeof(int)) {
                    return (IHeap<T>)new HeapFactory<int>().Create(new IntComparer(), (Action<int, int>)(object)indexSet);
                }

                if (typeof(T) == typeof(long)) {
                    return (IHeap<T>)new HeapFactory<long>().Create(new LongComparer(), (Action<long, int>)(object)indexSet);
                }

                return Create(Comparer<T>.Default, indexSet);
            }

            public IHeap<T> Create<TComp>(TComp customComparer, Action<T, int> indexSet = null)
                where TComp : IComparer<T>
            {
                if (indexSet == null) {
                    return Create(customComparer, new NoSink<T>());
                }

                return Create(customComparer, CreateDelegateSink(indexSet));
            }

            public IHeap<T> Create<TSink, TComp>(TComp customComparer, TSink sink)
                where TComp : IComparer<T>
                where TSink : IHeapIndexSink<T>
                => new Heap<T, TSink, TComp>(sink, customComparer);
        }

        public struct IntComparer : IComparer<int>
        {
            public int Compare(int x, int y)
                => x < y
                    ? -1
                    : x > y
                        ? 1
                        : 0;
        }

        public struct FastIntComparer : IComparer<int>
        {
            public int Compare(int x, int y)
                => x - y;
        }

        public struct LongComparer : IComparer<long>
        {
            public int Compare(long x, long y)
                => x < y
                    ? -1
                    : x > y
                        ? 1
                        : 0;
        }

        public struct ComparableComparer<T> : IComparer<T>
            where T : IComparable<T>
        {
            public int Compare(T x, T y)
                => x.CompareTo(y);
        }

        static DelegateSink<T> CreateDelegateSink<T>(Action<T, int> sink)
            => new(sink);

        struct DelegateSink<T> : IHeapIndexSink<T>
        {
            readonly Action<T, int> sink;

            public DelegateSink(Action<T, int> sink)
                => this.sink = sink;

            public void IndexChanged(T item, int newIndex)
                => sink(item, newIndex);
        }

        struct NoSink<T> : IHeapIndexSink<T>
        {
            public void IndexChanged(T item, int newIndex) { }
        }

        struct SelfSink<T> : IHeapIndexSink<T>
            where T : IHeapIndexed
        {
            public void IndexChanged(T item, int newIndex)
                => item.HeapIndexChanged(newIndex);
        }
    }

    public sealed class Heap<T, TSink, TComp> : IHeap<T>
        where TComp : IComparer<T>
        where TSink : IHeapIndexSink<T>
    {
        readonly TSink indexSet;
        readonly TComp comparer;
        T[] backingStore = new T[8];
        int backingCount;

        public Heap(TSink indexSet, TComp customComparer)
        {
            this.indexSet = indexSet;
            comparer = customComparer;
        }

        public void Add(T elem)
        {
            if (backingCount == backingStore.Length) {
                Array.Resize(ref backingStore, backingStore.Length * 2);
            }

            var newIndex = backingCount++;
            Bubble(newIndex, elem);
        }

        public int Count
            => backingCount;

        void Bubble(int newIndex, T elem)
        {
            var parIndex = (newIndex - 1) / 2;
            while (newIndex != 0 && comparer.Compare(backingStore[parIndex], elem) > 0) {
                backingStore[newIndex] = backingStore[parIndex];
                indexSet.IndexChanged(backingStore[newIndex], newIndex);
                newIndex = parIndex;
                parIndex = (newIndex - 1) / 2;
            }

            backingStore[newIndex] = elem;
            indexSet.IndexChanged(backingStore[newIndex], newIndex);
        }

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
        public bool RemoveTop(out T retval)
        {
            if (backingCount == 0) {
                retval = default(T);
                return false;
            }

            retval = backingStore[0];

            backingCount--;
            if (0 != backingCount) {
                Sink(0, backingStore[backingCount]);
            }

            backingStore[backingCount] = default(T);

            return true;
        }

        public T RemoveTop()
        {
            if (backingCount == 0) {
                throw new InvalidOperationException("Can't remove top; sequence empty");
            }

            var retval = backingStore[0];

            backingCount--;
            if (0 != backingCount) {
                Sink(0, backingStore[backingCount]);
            }

            backingStore[backingCount] = default(T);

            return retval;
        }

        public T Top()
            => this[0];

        public void TopChanged()
            => Sink(0, Top());

        public void Delete(int indexOfItem)
        {
            var tailItem = backingStore[--backingCount];
            //we must delete from the end of the array to avoid holes.
            //but we actually want to delete @ indexOfItem; so we'll insert this last element @ indexOfItem,
            //and if it's cheaper than the original item there, we need to sink it towards the leaves
            //but if it's lighter, we need to bubble as usual.

            if (indexOfItem == backingCount) { } //we deleted the last item.
            else if (comparer.Compare(tailItem, backingStore[indexOfItem]) > 0) {
                Sink(indexOfItem, tailItem); // tail item has higher cost, need to sink it.
            } else {
                Bubble(indexOfItem, tailItem); // tail item has lower cost, need to bubble it.
            }

            backingStore[backingCount] = default(T);
        }

        void Sink(int p, T elem)
        {
            while (2 * p + 2 < backingCount) {
                int idxKidA = 2 * p + 1, idxKidB = idxKidA + 1;
                var kid = comparer.Compare(backingStore[idxKidA], backingStore[idxKidB]) <= 0 ? idxKidA : idxKidB;
                if (comparer.Compare(elem, backingStore[kid]) > 0) { //elem isn't smaller than kid
                    indexSet.IndexChanged(backingStore[p] = backingStore[kid], p);
                    p = kid;
                } else {
                    break;
                }
            }

            var singleKid = 2 * p + 1;
            if (singleKid < backingCount && comparer.Compare(elem, backingStore[singleKid]) > 0) {
                indexSet.IndexChanged(backingStore[p] = backingStore[singleKid], p);
                p = singleKid;
            }

            indexSet.IndexChanged(backingStore[p] = elem, p);
        }

        public T this[int i]
        {
            get {
                if (i >= backingCount) {
                    throw new IndexOutOfRangeException();
                }

                return backingStore[i];
            }
        }

        public IEnumerable<T> ElementsInRoughOrder
            => backingStore.Take(backingCount);
    }

    public sealed class CostHeap<T>
    {
        public struct Entry
        {
            public int Cost;
            public T Item;
        }

        Entry[] backingStore = new Entry[8];

        int backingCount;
        //public Heap() { }

        public void Add(T elem, int cost)
        {
            if (backingCount == backingStore.Length) {
                Array.Resize(ref backingStore, backingStore.Length * 2);
            }

            var newIndex = backingCount++;
            Bubble(newIndex, new() { Cost = cost, Item = elem });
        }

        public int Count
            => backingCount;

        void Bubble(int newIndex, Entry elem)
        {
            var parIndex = (newIndex - 1) / 2;
            while (backingStore[parIndex].Cost > elem.Cost && newIndex != 0) {
                backingStore[newIndex] = backingStore[parIndex];
                //indexSet.IndexChanged(backingStore[newIndex], newIndex);
                newIndex = parIndex;
                parIndex = (newIndex - 1) / 2;
            }

            backingStore[newIndex] = elem;
            //indexSet.IndexChanged(backingStore[newIndex], newIndex);
        }

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
        public bool RemoveTop(out T retval, out int cost)
        {
            if (backingCount == 0) {
                retval = default(T);
                cost = default(int);
                return false;
            }

            retval = backingStore[0].Item;
            cost = backingStore[0].Cost;

            backingCount--;
            if (0 != backingCount) {
                Sink(0, backingStore[backingCount]);
            }

            backingStore[backingCount].Item = default(T); //don't care about Cost.

            return true;
        }

        public T RemoveTop()
        {
            if (backingCount == 0) {
                throw new InvalidOperationException("Can't remove top; sequence empty");
            }

            var retval = backingStore[0].Item;

            backingCount--;
            if (0 != backingCount) {
                Sink(0, backingStore[backingCount]);
            }

            backingStore[backingCount].Item = default(T);

            return retval;
        }

        public Entry Top()
            => this[0];

        public void TopCostChanged(int newCost)
        {
            if (0 == backingCount) {
                throw new IndexOutOfRangeException();
            }

            Sink(0, new() { Item = backingStore[0].Item, Cost = newCost });
        }

        public void Delete(int indexOfItem)
        {
            var tailItem = backingStore[--backingCount];

            if (indexOfItem == backingCount) { } //we deleted the last item.
            else if (tailItem.Cost > backingStore[indexOfItem].Cost) // sink item is heavier than deleted item, need to sink it.
            {
                Sink(indexOfItem, tailItem);
            } else // last item is lighter than deleted item, need to bubble it.
            {
                Bubble(indexOfItem, tailItem);
            }

            backingStore[backingCount].Item = default(T);
        }

        void Sink(int p, Entry elem)
        {
            while (2 * p + 2 < backingCount) {
                int idxKidA = 2 * p + 1, idxKidB = idxKidA + 1;
                var kid = backingStore[idxKidA].Cost <= backingStore[idxKidB].Cost ? idxKidA : idxKidB;
                if (elem.Cost > backingStore[kid].Cost) { //elem isn't smaller than kid
                    backingStore[p] = backingStore[kid];
                    //indexSet.IndexChanged(backingStore[p], p);
                    p = kid;
                } else {
                    break;
                }
            }

            var singleKid = 2 * p + 1;
            if (singleKid < backingCount && elem.Cost > backingStore[singleKid].Cost) {
                backingStore[p] = backingStore[singleKid];
                //indexSet.IndexChanged(backingStore[p], p);
                p = singleKid;
            }

            backingStore[p] = elem;
            //indexSet.IndexChanged(backingStore[p], p);
        }

        public Entry this[int i]
        {
            get {
                if (i >= backingCount) {
                    throw new IndexOutOfRangeException();
                }

                return backingStore[i];
            }
        }

        public IEnumerable<Entry> ElementsInRoughOrder
            => backingStore.Take(backingCount);
    }
}
