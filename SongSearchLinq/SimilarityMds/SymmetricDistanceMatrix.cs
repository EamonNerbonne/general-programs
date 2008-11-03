using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimilarityMds
{

    /// <summary>
    /// Resizable Symmetric distance matrix.  It is an error to access the matrix's diagonal.
    /// </summary>
    public class SymmetricDistanceMatrix
    {
        List<float> distances = new List<float>();
        int elementCount = 0;
        public int ElementCount {
            set {
                elementCount = value;
                int newMatSize = matSize(elementCount);
                if (distances.Count < newMatSize)
                    distances.AddRange(Enumerable.Repeat(0.0f, newMatSize - distances.Count));
                else
                    distances.RemoveRange(newMatSize, distances.Count - newMatSize);
            }
            get {
                return elementCount;
            }
        }
        
        public SymmetricDistanceMatrix(int elemCount) {            this.ElementCount = elemCount;        }

        static int matSize(int elemCount) { return elemCount * (elemCount - 1) >> 1; }
        int calcOffset(int i, int j) {
            if (i > j) {
                if (i > elementCount) throw new IndexOutOfRangeException("i is out of range");
                int tmp = i;
                i = j;
                j = tmp;
            } else if (i == j) {
                return -1;
            } else if (j > elementCount) throw new IndexOutOfRangeException("j is out of range");
            return i + ((j * (j - 1)) >> 1);
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
            for (int k = 0; k < elementCount; k++)
                for (int i = 0; i < elementCount - 1; i++)
                    if (i != k)
                        for (int j = i + 1; j < elementCount; j++)
                            if (j != k)
                                this[i, j] = Math.Min(this[i, j], this[i, k] + this[k, j]);
        }
    }
}
