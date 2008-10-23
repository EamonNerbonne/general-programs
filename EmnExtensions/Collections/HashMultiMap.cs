using System.Collections.Generic;
using System.Linq;

namespace EmnExtensions.Collections {
    public class HashMultiMap<T1,T2> :IMultiMap<T1,T2>{

        Dictionary<T1, HashSet<T2>> forwardLookup = new Dictionary<T1, HashSet<T2>>();
        Dictionary<T2, HashSet<T1>> reverseLookup = new Dictionary<T2, HashSet<T1>>();

        public HashMultiMap() { }
        public HashMultiMap(IEnumerable<Edge<T1,T2>> edges) {
            foreach (var edge in edges)
                AddEdge(edge.From, edge.To);
        }
        private static IEnumerable<Y> LookIn<X,Y>(Dictionary<X, HashSet<Y>> dict, X key) {
            if (dict.ContainsKey(key)) return dict[key]; else return Enumerable.Empty<Y>();
        }

        public void AddNodeFrom(T1 from) {
            if (!forwardLookup.ContainsKey(from)) 
            forwardLookup[from] = new HashSet<T2>();
        }
        public void AddNodeTo(T2 to) {
            if (!reverseLookup.ContainsKey(to)) 
            reverseLookup[to] = new HashSet<T1>();
        }
        public void AddEdge(T1 from, T2 to) {
            AddNodeFrom(from);
            AddNodeTo(to);
            forwardLookup[from].Add(to);
            reverseLookup[to].Add(from);
        }
        public bool ContainsEdge(T1 from, T2 to) {
            return forwardLookup.ContainsKey(from) && forwardLookup[from].Contains(to);
        }
        public IEnumerable<Edge<T1, T2>> Edges {
            get {
                return from edS in forwardLookup
                       from edE in edS.Value
                       select  Edge.Create( edS.Key, edE );
            }
        }
        public IEnumerable<T1> NodesFrom { get { foreach (var n in forwardLookup.Keys) yield return n; } }
        public IEnumerable<T2> NodesTo { get { foreach (var n in reverseLookup.Keys) yield return n; } }
        public IEnumerable<T2> ReachableFrom(T1 node) { return LookIn(forwardLookup, node); }
        public IEnumerable<T1> ReachesTo(T2 node) { return LookIn(reverseLookup, node); }
        public void RemoveEdge(T1 from, T2 to) {
            HashSet<T2> reachableFrom;
            HashSet<T1> reachesTo;

            if (forwardLookup.TryGetValue(from, out reachableFrom) && reverseLookup.TryGetValue(to, out reachesTo)) {
                reachableFrom.Remove(to);
                reachesTo.Remove(from);
            }
        }
        public void RemoveNodeFrom(T1 from) {
            if (!forwardLookup.ContainsKey(from)) return;
            foreach (var to in forwardLookup[from]) reverseLookup[to].Remove(from);
            forwardLookup.Remove(from);
        }
        public void RemoveNodeTo(T2 to) {
            if (!reverseLookup.ContainsKey(to)) return;
            foreach (var from in reverseLookup[to]) forwardLookup[from].Remove(to);
            reverseLookup.Remove(to);
        }
    }
}
