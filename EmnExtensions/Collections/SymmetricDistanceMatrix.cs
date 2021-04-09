using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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


        public int ElementCount
        {
            set {
                var oldMatSize = matSize(elementCount);
                elementCount = value;
                var newMatSize = matSize(elementCount);

                if (distances.Length < newMatSize) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                    for (var i = oldMatSize; i < newMatSize; i++) {
                        distances[i] = DefaultElement;
                    }
                } else if (distances.Length > (int)(newMatSize * resizefactor * resizefactor + 0.9)) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                }
            }
            get => elementCount;
        }

        public int DistCount => matSize(elementCount);

        public void TrimCapacityToFit() => Array.Resize(ref distances, matSize(elementCount));

        public IEnumerable<T> Values => distances.Take(DistCount);

        public SymmetricDistanceMatrixGen() { }

        static int matSize(int elemCount) => elemCount * (elemCount - 1) >> 1;
        int calcOffset(int i, int j)
        {
            if (i > j) {
                if (i > elementCount) {
                    throw new ArgumentOutOfRangeException("i", "i is out of range");
                }

                var tmp = i;
                i = j;
                j = tmp;
            } else if (i == j) {
                return -1;
            } else if (j > elementCount) {
                throw new ArgumentOutOfRangeException("j", "j is out of range");
            }

            return i + ((j * (j - 1)) >> 1);
        }

        /// <summary>
        /// Returns the internal array used for storage.  This array can safely by used until the matrix is resized
        /// with a call to TrimCapacityToFit or by setting ElementCount.
        /// Access to this array is read/write.  element i,j is at location i + ((j * (j - 1)) >> 1) given that i is less than j.
        /// </summary>
        /// <returns></returns>
        public T[] DirectArrayAccess() => distances;

        /// <summary>
        /// It is an error to access the diagonal which must be 0!
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional")]
        public T this[int i, int j]
        {
            get => distances[calcOffset(i, j)];
            set => distances[calcOffset(i, j)] = value;
        }

        /// <summary>
        /// Returns default(T) == 0.0 when accessing the diagonal.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public T GetDist(int i, int j)
        {
            var ind = calcOffset(i, j);
            return ind < 0 ? default(T) : distances[ind];
        }
    }



    public class SymmetricDistanceMatrix : SymmetricDistanceMatrixGen<float>
    {
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ElementCount);
            writer.Write(DistCount);
            foreach (var f in Values) {
                writer.Write((float)f);
            }
        }
        public SymmetricDistanceMatrix(BinaryReader reader)
        {
            ElementCount = reader.ReadInt32();
            var distCount = reader.ReadInt32();
            var distances = DirectArrayAccess();
            for (var i = 0; i < distCount; i++) {
                distances[i] = reader.ReadSingle();
            }
        }
        public SymmetricDistanceMatrix() { }

        public void FloydWarshall(Action<double> progress)
        {
            for (var k = 0; k < ElementCount; k++) {
                progress(k / (double)ElementCount);
                for (var i = 0; i < ElementCount - 1; i++) {
                    if (i != k) {
                        for (var j = i + 1; j < ElementCount; j++) {
                            if (j != k) {
                                this[i, j] = Math.Min(this[i, j], this[i, k] + this[k, j]);
                            }
                        }
                    }
                }
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

        public int ElementCount
        {
            set {
                var oldMatSize = matSize(elementCount);
                elementCount = value;
                var newMatSize = matSize(elementCount);

                if (distances.Length < newMatSize) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                    for (var i = oldMatSize; i < newMatSize; i++) {
                        distances[i] = DefaultElement;
                    }
                } else if (distances.Length > (int)(newMatSize * resizefactor * resizefactor + 0.9)) {
                    Array.Resize(ref distances, (int)(newMatSize * resizefactor + 0.9));
                }
            }
            get => elementCount;
        }

        public int DistCount => matSize(elementCount);

        public void TrimCapacityToFit() => Array.Resize(ref distances, matSize(elementCount));


        public IEnumerable<T> Values => distances.Take(DistCount);

        public TriangularMatrix() { }

        static int matSize(int elemCount) => elemCount * (elemCount + 1) >> 1;
        int calcOffset(int i, int j)
        {
            if (i > j) {
                if (i > elementCount) {
                    throw new ArgumentOutOfRangeException("i", "i is out of range");
                }

                var tmp = i;
                i = j;
                j = tmp;
            } else if (j > elementCount) {
                throw new ArgumentOutOfRangeException("j", "j is out of range");
            }

            return i + ((j * (j + 1)) >> 1);
        }

        /// <summary>
        /// Returns the internal array used for storage.  This array can safely by used until the matrix is resized
        /// with a call to TrimCapacityToFit or by setting ElementCount.
        /// Access to this array is read/write.  element i,j is at location i + ((j * (j + 1)) >> 1) given that i is less than j.
        /// </summary>
        /// <returns></returns>
        public T[] DirectArrayAccess() => distances;

        /// <summary>
        /// It is an error to access the diagonal which must be 0!
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional")]
        public T this[int i, int j]
        {
            get => distances[calcOffset(i, j)];
            set => distances[calcOffset(i, j)] = value;
        }
    }

}
