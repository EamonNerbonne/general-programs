using System.Collections.Generic;
using System.Linq;

namespace EmnExtensions.Collections
{
    public class HashMultiMap<T1, T2> : IMultiMap<T1, T2>
    {
        readonly Dictionary<T1, HashSet<T2>> forwardLookup = new();
        readonly Dictionary<T2, HashSet<T1>> reverseLookup = new();
        public HashMultiMap() { }

        public HashMultiMap(IEnumerable<Edge<T1, T2>> edges)
        {
            foreach (var edge in edges) {
                AddEdge(edge.From, edge.To);
            }
        }

        static IEnumerable<Y> LookIn<X, Y>(Dictionary<X, HashSet<Y>> dict, X key)
        {
            if (dict.ContainsKey(key)) {
                return dict[key];
            }

            return Enumerable.Empty<Y>();
        }

        public void AddNodeFrom(T1 from)
        {
            if (!forwardLookup.ContainsKey(from)) {
                forwardLookup[from] = new();
            }
        }

        public void AddNodeTo(T2 to)
        {
            if (!reverseLookup.ContainsKey(to)) {
                reverseLookup[to] = new();
            }
        }

        public void AddEdge(T1 from, T2 to)
        {
            AddNodeFrom(from);
            AddNodeTo(to);
            forwardLookup[from].Add(to);
            reverseLookup[to].Add(from);
        }

        public bool ContainsEdge(T1 from, T2 to)
            => forwardLookup.ContainsKey(from) && forwardLookup[from].Contains(to);

        public IEnumerable<Edge<T1, T2>> Edges
            =>
                from edS in forwardLookup
                from edE in edS.Value
                select Edge.Create(edS.Key, edE);

        public IEnumerable<T1> NodesFrom
        {
            get {
                foreach (var n in forwardLookup.Keys) {
                    yield return n;
                }
            }
        }

        public IEnumerable<T2> NodesTo
        {
            get {
                foreach (var n in reverseLookup.Keys) {
                    yield return n;
                }
            }
        }

        public IEnumerable<T2> ReachableFrom(T1 node)
            => LookIn(forwardLookup, node);

        public IEnumerable<T1> ReachesTo(T2 node)
            => LookIn(reverseLookup, node);

        public void RemoveEdge(T1 from, T2 to)
        {
            if (forwardLookup.TryGetValue(from, out var reachableFrom) && reverseLookup.TryGetValue(to, out var reachesTo)) {
                reachableFrom.Remove(to);
                reachesTo.Remove(from);
            }
        }

        public void RemoveNodeFrom(T1 from)
        {
            if (!forwardLookup.ContainsKey(from)) {
                return;
            }

            foreach (var to in forwardLookup[from]) {
                reverseLookup[to].Remove(from);
            }

            forwardLookup.Remove(from);
        }

        public void RemoveNodeTo(T2 to)
        {
            if (!reverseLookup.ContainsKey(to)) {
                return;
            }

            foreach (var from in reverseLookup[to]) {
                forwardLookup[from].Remove(to);
            }

            reverseLookup.Remove(to);
        }
    }
}
