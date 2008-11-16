using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider;
using EmnExtensions.Collections;
using EmnExtensions.MathHelpers;
using EmnExtensions;
namespace SimilarityMdsLib
{
    public class EmbedNonLandmarks
    {
        public struct DistanceToLandmark
        {
            public int LandmarkIndex;
            public float[] DistanceToAllSongs;
        }
        private EmbedNonLandmarks() { }
        public static double[,] Triangulate(IProgressManager progress, int allCount, SymmetricDistanceMatrix landmarkDistanceMatrix, double[,] landmarkPos, IEnumerable<DistanceToLandmark> distLandmarkToAll,double maxdist) {
            EmbedNonLandmarks computeEngine = new EmbedNonLandmarks();
            computeEngine.distMat = landmarkDistanceMatrix;
            computeEngine.mappedPos = landmarkPos;
            computeEngine.distLandmarkToAll = distLandmarkToAll;
            computeEngine.TriangulateUnmapped(progress, allCount,maxdist *10);
            return computeEngine.allPoses;
        }

        IEnumerable<DistanceToLandmark> distLandmarkToAll;
        SymmetricDistanceMatrix distMat;
        double[,] mappedPos; //mappedPos[pointIndex,dimension]
        double[] eigvals; //eigvals[dimension];
        double[] Du;//mean of sqr'd distances to each mapped element.
        double[,] allPoses;

        private void MeanCenter() {
            int dimCount = mappedPos.GetLength(1);
            int pCount = mappedPos.GetLength(0);

            for (int dim = 0; dim < mappedPos.GetLength(1); dim++) {
                double sum = 0.0;
                for (int pi = 0; pi < pCount; pi++) {
                    sum += mappedPos[pi, dim];
                }
                double mean = sum / pCount;
                for (int pi = 0; pi < pCount; pi++) {
                    mappedPos[pi, dim] -= mean;
                }
            }
        }


        static double Sqr(double x) { return x * x; }
        void FindEigvals(IProgressManager prog) {
            int dimCount = mappedPos.GetLength(1);
            int pCount = mappedPos.GetLength(0);
            eigvals = new double[dimCount];

            for (int dim = 0; dim < dimCount; dim++) {
                double eig_sum_temp_num = 0.0;
                double eig_sum_temp_denum = 0.0;
                for (int pi = 0; pi < pCount; pi++) {
                    if((dim * pCount + pi)%1000==0)
                    prog.SetProgress((dim * (double)pCount + pi) / dimCount / pCount);
                    eig_sum_temp_denum += Sqr(mappedPos[pi, dim]);
                    for (int pj = 0; pj < pCount; pj++) {
                        if (pi != pj) {
                            eig_sum_temp_num += mappedPos[pi, dim] * mappedPos[pj, dim] * Sqr(distMat.GetDist(pi, pj));
                        }
                    }
                }
                eigvals[dim] = -0.5 * eig_sum_temp_num / eig_sum_temp_denum;
            }
        }
        void CompDu(IProgressManager prog) {
            int dimCount = mappedPos.GetLength(1);
            int pCount = mappedPos.GetLength(0);
            Du = new double[pCount];
            for (int pi = 0; pi < pCount; pi++) {
                prog.SetProgress(pi / (double)pCount);
                for (int pj = pi + 1; pj < pCount; pj++) {
                    double dist = distMat[pi, pj];
                    Du[pj] += dist * dist;
                    Du[pi] += dist * dist;
                }
                Du[pi] = Du[pi] / pCount;
            }
        }

        double CalcShearFactor() {
            int dimCount = mappedPos.GetLength(1);
            int pCount = mappedPos.GetLength(0);
            double[,] cov = new double[dimCount, dimCount];
            for (int dim = 0; dim < dimCount; dim++) {

                for (int dim2 = dim; dim2 < dimCount; dim2++) {
                    double sum = 0;
                    for (int pi = 0; pi < pCount; pi++) {
                        sum += mappedPos[pi, dim] * mappedPos[pi, dim2];
                    }
                    if (dim2 == dim) {
                        Console.WriteLine("Dim {0}<->{0} sqrLen: {1}, length:{2}, eig:{3}", dim, sum, Math.Sqrt(sum), eigvals[dim]);
                        //           eigvals[dim] = Math.Sqrt(sum);
                    } else {
                        Console.WriteLine("Dim {0}<->{1} dotprod: {2}", dim, dim2, sum);
                    }
                    cov[dim, dim2] = cov[dim2, dim] = sum;
                }
            }
            return  (dimCount * (dimCount - 1) / 2) / (
                from dim in 0.To(dimCount)
                from dim2 in 0.To(dim)
                select cov[dim, dim2] / Math.Sqrt(cov[dim, dim] * cov[dim2, dim2])
                ).Sum();
        }

        public void TriangulateUnmapped(IProgressManager prog, int allCount,double replacedist) {
            int dimCount = mappedPos.GetLength(1);
            int pCount = mappedPos.GetLength(0);
            prog.NewTask("Mean Centering");
            MeanCenter();
            prog.NewTask("Finding Eigenvalues");
            FindEigvals(prog);
            prog.NewTask("Computing mean squared distance to all landmarks");
            CompDu(prog);

            Console.WriteLine("SHEAR FACTOR: {0}", CalcShearFactor());
            prog.NewTask("Embedding");
            allPoses = new double[allCount, dimCount];
            int progI = 0;
            foreach(var distToL in distLandmarkToAll) {
                prog.SetProgress(progI++ / (double)pCount);
                var distsFromLandmark = distToL.DistanceToAllSongs;
                int pi = distToL.LandmarkIndex;
                for (int unmP = 0; unmP < allCount; unmP++) {
                    double dist = distsFromLandmark[unmP];//what if this isn't finite?
                    if (!dist.IsFinite()) dist = replacedist; // replace it.
                    double netDiffp = dist * dist - Du[pi];
                    for (int dim = 0; dim < dimCount; dim++) {
                        allPoses[unmP,dim] += mappedPos[pi, dim] * netDiffp;
                    }
                }
            }
            if (progI != Du.Length)
                Console.WriteLine("whoops: progI != Du.Length");
            prog.NewTask("Finishing up...");
            for (int unmP = 0; unmP < allCount; unmP++) {
                if(unmP%1000==0)
                    prog.SetProgress(unmP / (double)allCount);
                for (int dim = 0; dim < dimCount; dim++) {
                    allPoses[unmP, dim] = -0.5 * allPoses[unmP, dim] / eigvals[dim];
                }
            }
            prog.Done();
        }
    }
}
