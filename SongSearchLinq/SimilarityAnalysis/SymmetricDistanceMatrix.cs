using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimilarityAnalysis
{
    public class SymmetricDistanceMatrix
    {
        readonly float[] distances;
        public int ElementCount { get; private set; }
        public SymmetricDistanceMatrix(int elemCount) {
            this.ElementCount = elemCount;
            int matsize = ElementCount * (ElementCount - 1) / 2;
            distances = new float[matsize];
        }

        int calcOffset(int i, int j) {
            if(i>j) {
                int tmp=i;
                i=j;
                j=tmp;
            } else if(i==j) {
                return -1;
            } 
            return j-1 + (ElementCount*2 -i-3)*i/2;
        }

        public float this[int i, int j] {
            get {
                return distances[calcOffset(i, j)];
            }
            set {
                distances[calcOffset(i, j)] = value;
            }
        }

        public void FloydWarshall() {
            for (int k = 0; k < ElementCount; k++)
                for (int i = 0; i < ElementCount - 1; i++)
                    if (i != k)
                        for (int j = i + 1; j < ElementCount; j++)
                            if (j != k)
                                this[i, j] = Math.Min(this[i, j], this[i, k] + this[k, j]);
        }
    }
}
