using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmnExtensions.Collections
{

    /// <summary>
    /// Resizable Symmetric distance matrix.  It is an error to access the matrix's diagonal.
    /// </summary>
    public class SymmetricDistanceMatrix
    {
        const float resizefactor=1.5f;
        float[] distances = new float[0];
        int elementCount = 0;
        public int ElementCount {
            set {
                int oldMatSize = matSize(elementCount);
                elementCount = value;
                int newMatSize = matSize(elementCount);

                if (distances.Length < newMatSize) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                    for (int i = oldMatSize; i < newMatSize; i++)
                        distances[i] = float.PositiveInfinity;
                } else if (distances.Length > (int)(newMatSize * resizefactor*resizefactor + 0.9)) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                }
            }
            get {
                return elementCount;
            }
        }

        public void TrimCapacityToFit() {
            Array.Resize(ref distances, matSize(elementCount));
        }

        public void WriteTo(BinaryWriter writer) {
            writer.Write(elementCount);
            writer.Write(matSize(elementCount));
            foreach (var f in distances) {
                writer.Write((float)f);
            }
        }
        public SymmetricDistanceMatrix(BinaryReader reader) {
            elementCount = reader.ReadInt32();
            int distCount = reader.ReadInt32();
            distances = new float[distCount];
            for (int i = 0; i < distCount; i++)
                distances[i] =reader.ReadSingle();
        }

        public IEnumerable<float> Values { get { return distances; } }

        public SymmetricDistanceMatrix() {  }

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

        /// <summary>
        /// Returns the internal array used for storage.  This array can safely by used until the matrix is resized
        /// with a call to TrimCapacityToFit or by setting ElementCount.
        /// Access to this array is read/write.  element i,j is at location i + ((j * (j - 1)) >> 1) given that i is less than j.
        /// </summary>
        /// <returns></returns>
        public float[] DirectArrayAccess() {
            return distances;
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
