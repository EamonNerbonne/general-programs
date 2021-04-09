using System.Collections.Generic;

namespace EmnExtensions.Collections
{
    public interface IMultiMap<T1, T2>
    {
        void AddNodeFrom(T1 from);
        void AddNodeTo(T2 to);
        void AddEdge(T1 from, T2 to);
        bool ContainsEdge(T1 from, T2 to);
        IEnumerable<Edge<T1, T2>> Edges { get; }
        IEnumerable<T1> NodesFrom { get; }
        IEnumerable<T2> NodesTo { get; }
        IEnumerable<T2> ReachableFrom(T1 node);
        IEnumerable<T1> ReachesTo(T2 node);
        void RemoveEdge(T1 from, T2 to);
        void RemoveNodeFrom(T1 from);
        void RemoveNodeTo(T2 to);
    }
}
