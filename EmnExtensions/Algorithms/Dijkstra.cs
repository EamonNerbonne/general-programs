using System;
using System.Collections.Generic;
using EmnExtensions.Collections;

namespace EmnExtensions.Algorithms
{
    public static class Dijkstra
    {
        public struct DistanceTo : IComparable<DistanceTo>
        {
            public int targetNode;
            public float distance;

            public int CompareTo(DistanceTo other)
                => distance.CompareTo(other.distance);
        }

        public static void FindShortestPath(Func<int, IEnumerable<DistanceTo>> graph, int nodeCount, IEnumerable<int> startNodes, out float[] distance, out int[] comeFrom)
        {
            var nodeIndex = new int[nodeCount];
            comeFrom = new int[nodeCount];
            distance = new float[nodeCount];
            for (var i = 0; i < nodeCount; i++) {
                nodeIndex[i] = -1;
                distance[i] = float.PositiveInfinity;
                comeFrom[i] = -1;
            }

            var toProcess = Heap.Factory<DistanceTo>().Create(
                (node, newIndex) => {
                    nodeIndex[node.targetNode] = newIndex;
                }
            );
            var noStartNodes = true;
            foreach (var startNode in startNodes) {
                toProcess.Add(new() { distance = 0.0f, targetNode = startNode });
                distance[startNode] = 0;
                comeFrom[startNode] = startNode;
                noStartNodes = false;
            }

            if (noStartNodes) {
                throw new ArgumentException("startNodes must contain at least one node");
            }

            while (toProcess.RemoveTop(out var current)) {
                nodeIndex[current.targetNode] = -2; //i.e. processed
                foreach (var outEdge in graph(current.targetNode)) {
                    if (nodeIndex[outEdge.targetNode] == -2) { } else { //OK, next goes to outEdge...
                        var newLength = current.distance + outEdge.distance;
                        if (newLength < distance[outEdge.targetNode]) {
                            distance[outEdge.targetNode] = newLength;
                            comeFrom[outEdge.targetNode] = current.targetNode;
                            if (nodeIndex[outEdge.targetNode] != -1) //we need to remove it first
                            {
                                toProcess.Delete(nodeIndex[outEdge.targetNode]);
                            }

                            toProcess.Add(new() { distance = newLength, targetNode = outEdge.targetNode });
                        }
                    }
                }
            }
        }
    }
}
