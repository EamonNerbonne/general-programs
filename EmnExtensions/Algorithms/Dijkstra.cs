using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace EmnExtensions.Algorithms
{
    public static class DijkstraInt
    {
        public struct DistanceTo : IComparable<DistanceTo> {
            public int targetNode;
            public float distance;


            public int CompareTo(DistanceTo other) {return distance.CompareTo(other.distance);}
        }

        public static float[] Dijkstra(Func<int, IEnumerable<DistanceTo> > graph, int nodeCount, int startNode  ) {
            int[] nodeIndex = new int[nodeCount];
            float[] distance = new float[nodeCount];
            for (int i = 0; i < nodeCount; i++) {
                nodeIndex[i] = -1;
                distance[i] = float.PositiveInfinity;
            }
            Heap<DistanceTo> toProcess = new Heap<DistanceTo>( (node,newIndex) =>{nodeIndex[node.targetNode] = newIndex;});

            toProcess.Add(new DistanceTo{ distance = 0.0f, targetNode=startNode});
            DistanceTo next;
            while (toProcess.RemoveTop(out next)) {
                nodeIndex[next.targetNode] = -2;//i.e. processed
                foreach (DistanceTo outEdge in graph(next.targetNode)) {
                    if (nodeIndex[outEdge.targetNode] == -2) continue; //this edge is already processed.
                    else {//OK, next goes to outEdge...
                        float newLength = next.distance + outEdge.distance;
                        if (newLength < distance[outEdge.targetNode]) {
                            distance[outEdge.targetNode] = newLength;
                            if (nodeIndex[outEdge.targetNode] != -1) //we need to remove it first
                                toProcess.Delete(nodeIndex[outEdge.targetNode]);
                            toProcess.Add(new DistanceTo { distance = newLength, targetNode = outEdge.targetNode });
                        }
                    }
                }
            }

            return distance;

        }
    }
}
