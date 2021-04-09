using System;
using System.Collections.Generic;
using EmnExtensions.Collections;

namespace EmnExtensions.Algorithms
{
    public static class SortedUnionAlgorithm
    {
        public static IEnumerable<int> SortedUnion(IEnumerable<int>[] inorderLists)
        {
            var generators = new IEnumerator<int>[inorderLists.Length];

            try {
                for (var i = 0; i < inorderLists.Length; i++) {
                    generators[i] = inorderLists[i].GetEnumerator();
                }

                var gens = new CostHeap<IEnumerator<int>>();


                foreach (var gen in generators) {
                    if (gen.MoveNext()) {
                        gens.Add(gen, gen.Current);
                    }
                }
                //the costs *are* the current enumerator value
                var lastYield = gens.Count > 0 ? gens.Top().Cost - 1 : 0;//anything but equal!

                while (gens.Count > 0) {
                    var current = gens.Top();

                    if (current.Cost != lastYield) {
                        yield return lastYield = current.Cost;
                    }

                    if (current.Item.MoveNext()) {
                        gens.TopCostChanged(current.Item.Current);
                    } else {
                        gens.RemoveTop();
                    }
                }
            } finally {
                DisposeAll(generators, 0);
            }
        }

        internal static void DisposeAll<T>(T[] disposables, int startAt) where T : IDisposable
        {
            for (var i = startAt; i < disposables.Length; i++) {
                try {
                    if (disposables[i] != null) {
                        disposables[i].Dispose();
                    }
                } catch {
                    DisposeAll(disposables, i + 1);
                    throw;
                }
            }
        }

        public static IEnumerable<int> ZipMerge(IEnumerable<int> a, IEnumerable<int> b)
        {
            var enumA = a.GetEnumerator();
            var enumB = b.GetEnumerator();
            if (enumA.MoveNext()) {
                if (enumB.MoveNext()) {
                    var elA = enumA.Current;
                    var elB = enumB.Current;
                    while (true) {
                        if (elA < elB) {
                            yield return elA;
                            if (enumA.MoveNext()) {
                                elA = enumA.Current;
                            } else {//no more a's
                                yield return elB;
                                while (enumB.MoveNext()) {
                                    yield return enumB.Current;
                                }

                                break;
                            }
                        } else {
                            yield return elB;
                            if (enumB.MoveNext()) {
                                elB = enumB.Current;
                            } else {//no more b's!
                                yield return elA;
                                while (enumA.MoveNext()) {
                                    yield return enumA.Current;
                                }

                                break;
                            }
                        }
                    }
                } else {
                    yield return enumA.Current;
                    while (enumA.MoveNext()) {
                        yield return enumA.Current;
                    }
                }
            } else {
                while (enumB.MoveNext()) {
                    yield return enumB.Current;
                }
            }
        }

        public static IEnumerable<int> RemoveDup(IEnumerable<int> orderedList)
        {
            var orderedEnum = orderedList.GetEnumerator();
            if (!orderedEnum.MoveNext()) {
                yield break;
            }

            var current = orderedEnum.Current;
            yield return current;
            while (orderedEnum.MoveNext()) {
                var newVal = orderedEnum.Current;
                if (newVal != current) {
                    current = newVal;
                    yield return current;
                }
            }
        }
    }
}
