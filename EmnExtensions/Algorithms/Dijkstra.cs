﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace EmnExtensions.Algorithms
{
    public static class Dijkstra
    {
        public struct DistanceTo : IComparable<DistanceTo> {
            public int targetNode;
            public float distance;


            public int CompareTo(DistanceTo other) {return distance.CompareTo(other.distance);}
        }

        public static void FindShortestPath(Func<int, IEnumerable<DistanceTo> > graph, int nodeCount, int startNode, out float[] distance, out int[] comeFrom  ) {
            int[] nodeIndex = new int[nodeCount];
            comeFrom = new int[nodeCount];
            distance = new float[nodeCount];
            for (int i = 0; i < nodeCount; i++) {
                nodeIndex[i] = -1;
                distance[i] = float.PositiveInfinity;
                comeFrom[i] = -1;
            }
            Heap<DistanceTo> toProcess = new Heap<DistanceTo>( (node,newIndex) =>{nodeIndex[node.targetNode] = newIndex;});

            toProcess.Add(new DistanceTo{ distance = 0.0f, targetNode=startNode});
            distance[startNode] = 0;
            comeFrom[startNode] = startNode;
            DistanceTo current;
            while (toProcess.RemoveTop(out current)) {
                nodeIndex[current.targetNode] = -2;//i.e. processed
                foreach (DistanceTo outEdge in graph(current.targetNode)) {
                    if (nodeIndex[outEdge.targetNode] == -2) continue; //this edge is already processed.
                    else {//OK, next goes to outEdge...
                        float newLength = current.distance + outEdge.distance;
                        if (newLength < distance[outEdge.targetNode]) {
                            distance[outEdge.targetNode] = newLength;
                            comeFrom[outEdge.targetNode] = current.targetNode;
                            if (nodeIndex[outEdge.targetNode] != -1) //we need to remove it first
                                toProcess.Delete(nodeIndex[outEdge.targetNode]);
                            toProcess.Add(new DistanceTo { distance = newLength, targetNode = outEdge.targetNode });
                        }
                    }
                }
            }
        }
    }
}
