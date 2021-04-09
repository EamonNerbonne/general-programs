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
    public class SymmetricDistanceMatrixGen<T>
    {
        const double resizefactor = 1.5;
        T[] distances = new T[0];
        int elementCount = 0;

        public T DefaultElement { get; set; }


        public int ElementCount {
            set {
                int oldMatSize = matSize(elementCount);
                elementCount = value;
                int newMatSize = matSize(elementCount);

                if (distances.Length < newMatSize) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                    for (int i = oldMatSize; i < newMatSize; i++)
                        distances[i] = DefaultElement;
                } else if (distances.Length > (int)(newMatSize * resizefactor * resizefactor + 0.9)) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                }
            }
            get {
                return elementCount;
            }
        }

        public int DistCount { get { return matSize(elementCount); } }

        public void TrimCapacityToFit() {
            Array.Resize(ref distances, matSize(elementCount));
        }

        public IEnumerable<T> Values { get { return distances.Take(DistCount); } }

        public SymmetricDistanceMatrixGen() { }

        static int matSize(int elemCount) { return elemCount * (elemCount - 1) >> 1; }
        int calcOffset(int i, int j) {
            if (i > j) {
                if (i > elementCount) throw new ArgumentOutOfRangeException("i" ,"i is out of range");
                int tmp = i;
                i = j;
                j = tmp;
            } else if (i == j) {
                return -1;
            } else if (j > elementCount) throw new ArgumentOutOfRangeException("j","j is out of range");
            return i + ((j * (j - 1)) >> 1);
        }

        /// <summary>
        /// Returns the internal array used for storage.  This array can safely by used until the matrix is resized
        /// with a call to TrimCapacityToFit or by setting ElementCount.
        /// Access to this array is read/write.  element i,j is at location i + ((j * (j - 1)) >> 1) given that i is less than j.
        /// </summary>
        /// <returns></returns>
        public T[] DirectArrayAccess() {
            return distances;
        }

        /// <summary>
        /// It is an error to access the diagonal which must be 0!
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional")]
        public T this[int i, int j] {
            get {
                return distances[calcOffset(i, j)];
            }
            set {
                distances[calcOffset(i, j)] = value;
            }
        }

        /// <summary>
        /// Returns default(T) == 0.0 when accessing the diagonal.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public T GetDist(int i, int j) {
            int ind = calcOffset(i, j);
            return ind < 0 ? default(T) : distances[ind];
        }
    }



    public class SymmetricDistanceMatrix : SymmetricDistanceMatrixGen<float>
    {
        public void WriteTo(BinaryWriter writer) {
            writer.Write(ElementCount);
            writer.Write(DistCount);
            foreach (var f in Values) {
                writer.Write((float)f);
            }
        }
        public SymmetricDistanceMatrix(BinaryReader reader) {
            ElementCount = reader.ReadInt32();
            int distCount = reader.ReadInt32();
            float[] distances = DirectArrayAccess();
            for (int i = 0; i < distCount; i++)
                distances[i] = reader.ReadSingle();
        }
        public SymmetricDistanceMatrix() { }

        public void FloydWarshall(Action<double> progress) {
            for (int k = 0; k < this.ElementCount; k++) {
                progress(k / (double)this.ElementCount);
                for (int i = 0; i < this.ElementCount - 1; i++)
                    if (i != k)
                        for (int j = i + 1; j < this.ElementCount; j++)
                            if (j != k)
                                this[i, j] = Math.Min(this[i, j], this[i, k] + this[k, j]);
            }
        }

    }

    /// <summary>
    /// Triangular symmetric matrix.  It is not an error to access the matrix's diagonal.
    /// </summary>
    public class TriangularMatrix<T>
    {
        const double resizefactor = 1.5;
        T[] distances = new T[0];
        int elementCount = 0;

        //used when growing the matrix;
        public T DefaultElement { get; set; }

        public int ElementCount {
            set {
                int oldMatSize = matSize(elementCount);
                elementCount = value;
                int newMatSize = matSize(elementCount);

                if (distances.Length < newMatSize) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                    for (int i = oldMatSize; i < newMatSize; i++)
                        distances[i] = DefaultElement;
                } else if (distances.Length > (int)(newMatSize * resizefactor * resizefactor + 0.9)) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                }
            }
            get {
                return elementCount;
            }
        }

        public int DistCount { get { return matSize(elementCount); } }

        public void TrimCapacityToFit() {
            Array.Resize(ref distances, matSize(elementCount));
        }


        public IEnumerable<T> Values { get { return distances.Take(DistCount); } }

        public TriangularMatrix() { }

        static int matSize(int elemCount) { return elemCount * (elemCount + 1) >> 1; }
        int calcOffset(int i, int j) {
            if (i > j) {
                if (i > elementCount) throw new ArgumentOutOfRangeException("i","i is out of range");
                int tmp = i;
                i = j;
                j = tmp;
            } else if (j > elementCount) throw new ArgumentOutOfRangeException("j","j is out of range");
            return i + ((j * (j + 1)) >> 1);
        }

        /// <summary>
        /// Returns the internal array used for storage.  This array can safely by used until the matrix is resized
        /// with a call to TrimCapacityToFit or by setting ElementCount.
        /// Access to this array is read/write.  element i,j is at location i + ((j * (j + 1)) >> 1) given that i is less than j.
        /// </summary>
        /// <returns></returns>
        public T[] DirectArrayAccess() {
            return distances;
        }

        /// <summary>
        /// It is an error to access the diagonal which must be 0!
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional")]
        public T this[int i, int j] {
            get {
                return distances[calcOffset(i, j)];
            }
            set {
                distances[calcOffset(i, j)] = value;
            }
        }
    }

}
