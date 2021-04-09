namespace EmnExtensions.Collections
{
    public static class Edge
    {
        public static Edge<T1, T2> Create<T1, T2>(T1 from, T2 to) { return new Edge<T1, T2>(from, to); }
    }
    public struct Edge<T1, T2>
    {
        public readonly T1 From;
        public readonly T2 To;
        public Edge(T1 from, T2 to) { From = from; To = to; }
        public override int GetHashCode() { return From.GetHashCode() + 137 * To.GetHashCode(); }
    }

}
