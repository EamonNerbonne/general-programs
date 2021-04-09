using System.Collections.Generic;
using EmnExtensions.Collections;

namespace EmnExtensions.Algorithms
{
    public static class SortedIntersectionAlgorithm
    {
        public static IEnumerable<int> SortedIntersection(IEnumerable<int>[] inorderLists)
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
                var lastYield = gens.Count > 0 ? gens.Top().Cost - 1 : 0; //anything but equal!
                var matchCount = 0;

                while (gens.Count > 0) {
                    var current = gens.Top();

                    if (current.Cost != lastYield) {
                        lastYield = current.Cost;
                        matchCount = 1;
                    } else {
                        matchCount++;
                        if (matchCount == generators.Length) {
                            yield return lastYield;
                        }
                    }

                    if (current.Item.MoveNext()) {
                        gens.TopCostChanged(current.Item.Current);
                    } else {
                        gens.RemoveTop();
                    }
                }
            } finally {
                SortedUnionAlgorithm.DisposeAll(generators, 0);
            }
        }

        public static IEnumerable<int> SortedZipIntersect(IEnumerable<int> a, IEnumerable<int> b)
        {
            using var enumA = a.GetEnumerator();
            using var enumB = b.GetEnumerator();

            if (!enumA.MoveNext() || !enumB.MoveNext()) {
                yield break;
            }

            var elA = enumA.Current;
            var elB = enumB.Current;
            while (true) {
                if (elA == elB) {
                    yield return elA;
                    while (elA == elB && enumB.MoveNext()) {
                        elB = enumB.Current;
                    }

                    if (elA == elB) {
                        yield break;
                    }

                    if (!enumA.MoveNext()) {
                        yield break;
                    }
                } else if (elA < elB) {
                    if (!enumA.MoveNext()) {
                        yield break;
                    }

                    elA = enumA.Current;
                } else {
                    if (!enumB.MoveNext()) {
                        yield break;
                    }

                    elB = enumB.Current;
                }
            }
        }
    }
}
