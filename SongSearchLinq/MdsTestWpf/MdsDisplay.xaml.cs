using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using hitmds;
using System.Windows.Threading;
using EmnExtensions;
using EmnExtensions.Algorithms;
using EmnExtensions.Collections;
using LastFMspider;
using System.Collections;
using Microsoft.Win32;
using System.IO;
using EmnExtensions.Wpf;

namespace MdsTestWpf
{

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MdsDisplay : Window
    {
        const int res = 100; //80
        const int SUBSET_SIZE = 2000; //1000
        const int GEN = 100; //30
        const int POINT_UPDATE_STYLE = 1;
        const double LEARN_RATE = 2.0;
        const double DIST_LIMIT_AVG = 4.7;
        const double DIST_LIMIT_RAND = 0.5;
        const double DIST_NOISE = 3.0;
        const float INF_REPLACEMENT_FACTOR = 10.0f;
        int IndexFromIJ(int i, int j) {
            return i + res * j;
        }
        static double percentToSigma(double percent) {
            return Math.Log(percent / 100 + 1);
        }
        MdsPoint2D[] origs, calcs;
        int totalCycles = 0;
        List<Dijkstra.DistanceTo>[] distsTo;
        public MdsDisplay() {

            InitializeComponent();
            origs = new MdsPoint2D[res * res];
            distsTo = new List<Dijkstra.DistanceTo>[res * res];
            for (int i = 0; i < res; i++)
                for (int j = 0; j < res; j++) {
                    origs[IndexFromIJ(i, j)] = new MdsPoint2D { x = i, y = j };
                    distsTo[IndexFromIJ(i, j)] = new List<Dijkstra.DistanceTo>();
                }
            totalCycles = origs.Length * GEN;
            mdsProgress.Maximum = 1.0;
            mdsProgress.Minimum = 0;
            Thread t = new Thread(CalcMds) {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
            };
            t.Start();
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
            ShowMdsPoints(origs);
            Console.WriteLine("Noise: " + (DIST_NOISE * 100) + "%");
        }

        Rect BoundMdsPoints(MdsPoint2D[] points) {
            Rect viewRect = new Rect(points[0].ToPoint(), Size.Empty);
            foreach (var point in points) {
                viewRect.Union(point.ToPoint());
            }
            return viewRect;
        }
        float MaxDist = 0.0f;
        int infCount = 0;

        public class MappedDistStruct
        {
            public ArbitraryTrackMapper mapper;
            public List<float[]> distsFromMapped;
            public SymmetricDistanceMatrix distMat;

            public double[,] mappedPos; //mappedPos[pointIndex,dimension]

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
            void FindEigvals() {
                int dimCount = mappedPos.GetLength(1);
                int pCount = mappedPos.GetLength(0);
                eigvals = new double[dimCount];

                for (int dim = 0; dim < mappedPos.GetLength(1); dim++) {
                    double eig_sum_temp_num = 0.0;
                    double eig_sum_temp_denum = 0.0;
                    for (int pi = 0; pi < pCount; pi++) {
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
            void CompDu() {
                int dimCount = mappedPos.GetLength(1);
                int pCount = mappedPos.GetLength(0);
                Du = new double[pCount];
                for (int pi = 0; pi < pCount; pi++) {
                    for (int pj = pi + 1; pj < pCount; pj++) {
                        double dist = distMat[pi, pj];
                        Du[pj] += dist * dist;
                        Du[pi] += dist * dist;
                    }
                    Du[pi] = Du[pi] / pCount;
                }
            }

            double[] eigvals; //eigvals[dimension];
            double[] Du;//mean of sqr'd distances to each mapped element.
            public double[,] allPoses;

            public void TriangulateUnmapped(Action<double> prog, int allCount) {
                int dimCount = mappedPos.GetLength(1);
                int pCount = mappedPos.GetLength(0);
                MeanCenter();
                FindEigvals();
                CompDu();

                for (int dim = 0; dim < dimCount; dim++) {
                    
                    for (int dim2 = dim; dim2 < dimCount; dim2++) {
                        double sum = 0;
                        for (int pi = 0; pi < pCount; pi++) {
                            sum += mappedPos[pi, dim] * mappedPos[pi, dim2];
                        }
                        if (dim2 == dim) {
                            Console.WriteLine("Dim {0}<->{0} sqrLen: {1}, length:{2}, eig:{3}", dim, sum, Math.Sqrt(sum), eigvals[dim]);
                //           eigvals[dim] = Math.Sqrt(sum);
                        } else Console.WriteLine("Dim {0}<->{1} dotprod: {2}", dim, dim2, sum);
                    }
                }
                /*
                for (int dim = 0; dim < dimCount; dim++) {
                    double sum = 0;
                    for (int pi = 0; pi < pCount; pi++) {
                        sum += Sqr(mappedPos[pi, dim]);
                    }
                    double len = Math.Sqrt(sum);
                    for (int pi = 0; pi < pCount; pi++) {
                        mappedPos[pi, dim]*=eigvals[dim]/ len;//doesn't help.
                    }
                }
                /*
                FindEigvals();
                double[,] cov = new double[dimCount, dimCount];
                for (int dim = 0; dim < dimCount; dim++) {

                    for (int dim2 = dim; dim2 < dimCount; dim2++) {
                        double sum = 0;
                        for (int pi = 0; pi < pCount; pi++) {
                            sum += mappedPos[pi, dim] * mappedPos[pi, dim2];
                        }
                        if (dim2 == dim) {
                            Console.WriteLine("Dim {0}<->{0} sqrLen: {1}, length:{2}, eig:{3}", dim, sum, Math.Sqrt(sum), eigvals[dim]);
                            //                            eigvals[dim] = Math.Sqrt(sum);
                            cov[dim, dim] = 1 ;
                        } else {
                            Console.WriteLine("Dim {0}<->{1} dotprod: {2}", dim, dim2, sum);
                            cov[dim, dim2] = cov[dim2, dim] = -sum / eigvals[dim] / eigvals[dim2];
                        }
                    }
                }
                */


                allPoses = new double[allCount, dimCount];
                double[] netDiff = new double[allCount];
                for (int unmP = 0; unmP < allCount; unmP++) {
                    prog(unmP / (double)allCount);
#if DONT_REMAP_LANDMARKS
                    if (mapper.IsMapped(unmP)) {
                        int mP = mapper.Map(unmP);
                        for (int dim = 0; dim < dimCount; dim++)
                            allPoses[unmP, dim] = mappedPos[mP, dim];
                    } else {
#endif
                        for (int pi = 0; pi < pCount; pi++) {
                            double dist = distsFromMapped[pi][unmP];
                            netDiff[pi] = dist * dist - Du[pi];
                        }
                        double[] sums = new double[dimCount];
                        for (int dim = 0; dim < dimCount; dim++) {
                            //double sum = 0.0;
                            for (int pi = 0; pi < pCount; pi++) {
                                sums[dim] += mappedPos[pi, dim] * netDiff[pi];
                            }
                        }
                        for (int dim = 0; dim < dimCount; dim++) {

                            allPoses[unmP, dim] = (-0.5) *
                                sums[dim]  / eigvals[dim];
                                // sums.Select((sum, d) => sum * cov[d, dim]).Sum()/eigvals[dim]; //tried this, is no better
                        }
#if DONT_REMAP_LANDMARKS
                    }
#endif
                }
                prog(1.0);
            }

            private void Rescale(double p) {
                int dimCount = mappedPos.GetLength(1);
                int pCount = mappedPos.GetLength(0);
                for (int pi = 0; pi < pCount; pi++) {
                    for (int dim = 0; dim < dimCount; dim++) {
                        mappedPos[pi, dim] *= p;
                    }
                }

            }

        }

        MappedDistStruct CreateDistMat(Random r, NiceTimer t) {
            Console.WriteLine("  - Initializing Distances.");
            //SymmetricDistanceMatrix distMat = new SymmetricDistanceMatrix();
            //distMat.ElementCount = origs.Length;
            int connCount = 0;
            double sigma = percentToSigma(DIST_NOISE * 100) / Math.Sqrt(2); // div by sqrt(2) simulates symmetrization.
            for (int i = 0; i < origs.Length; i++)
                for (int j = i + 1; j < origs.Length; j++) {
                    double dist = origs[i].DistanceTo(origs[j]);
                    dist *= Math.Exp(r.NextNorm() * sigma); //between 1 - DIST_NOISE and 1+DIST_NOISE;
                    double distLim = DIST_LIMIT_AVG * (1.0 + DIST_LIMIT_RAND * 2 * (r.NextDouble() - 0.5));
                    if (dist < distLim) {
                        distsTo[i].Add(new Dijkstra.DistanceTo { distance = (float)dist, targetNode = j });
                        distsTo[j].Add(new Dijkstra.DistanceTo { distance = (float)dist, targetNode = i });
                        connCount++;
                    }
                    //distMat[i, j] = (float)( dist<distLim ? dist : float.PositiveInfinity);
                    //if (distMat[i, j].IsFinite()) connCount++;
                }
            Console.WriteLine("Avg connectivity: " + (connCount * 2.0 / origs.Length));
            Console.WriteLine("  - Dijkstras");
            //ok, rand dists initialized
            //now we insert random ones into the mapping:
            ArbitraryTrackMapper mapper = new ArbitraryTrackMapper();
            List<float[]> distsFromMapped = new List<float[]>();
            SymmetricDistanceMatrix distMat = new SymmetricDistanceMatrix();
            startMDS = DateTime.Now;
            while (mapper.Count < SUBSET_SIZE) {
                int next;
                do { next = r.Next(origs.Length); } while (mapper.IsMapped(next));

                //now find shortest path:
                float[] distanceFromNext;
                int[] pathToA;
                Dijkstra.FindShortestPath(i => distsTo[i], origs.Length, new[] { next }, out distanceFromNext, out  pathToA);
                distsFromMapped.Add(distanceFromNext);
                //have path now: map global id to dense id, add dense ID to DistanceMatrix and add distances to dense distance matrix


                int mappedID = mapper.Map(next);

                distMat.ElementCount = mapper.Count;
                foreach (var other in mapper.CurrentMappings) {
                    int otherMappedID = other.Value;
                    int otherGlobalID = other.Key;
                    if (otherMappedID != mappedID) //can't map to itself!
                        distMat[mappedID, otherMappedID] = distanceFromNext[otherGlobalID];
                }
                if (!(distMat.ElementCount == mapper.Count && mapper.Count == distsFromMapped.Count))
                    throw new Exception("Out of sync!");
                ProgressReport(mapper.Count / (double)SUBSET_SIZE);

            }
            Console.WriteLine("  - replacing infinite distances.");

            var dists = distMat.DirectArrayAccess();
            int maxDistInd = dists.IndexOfMax((i, f) => f.IsFinite());
            if (maxDistInd >= 0) {
                MaxDist = dists[maxDistInd];
                for (int i = 0; i < distMat.DistCount; i++) {
                    if (dists[i].IsFinite()) continue;
                    infCount++;
                    dists[i] = MaxDist * INF_REPLACEMENT_FACTOR;
                }
            }
            return new MappedDistStruct {
                distMat = distMat,
                distsFromMapped = distsFromMapped,
                mapper = mapper,
            };
        }


        void ShowMdsPoints(MdsPoint2D[] points) {
            pointCanvas.Children.Clear();
            Rect boundingBox = BoundMdsPoints(points);
            double scaleFactor = 10*res / Math.Max(boundingBox.Width, boundingBox.Height);
            pointCanvas.Width = 10+scaleFactor * boundingBox.Width;
            pointCanvas.Height = 10+scaleFactor * boundingBox.Height;
            Point topLeft = boundingBox.TopLeft;
            Func<int, int, Point> locatePoint = (i, j) => (Point)(scaleFactor * (points[IndexFromIJ(i, j)].ToPoint() - topLeft) + new Point(5,5));
            for (int i = 0; i < res; i++) {
                for (int j = 0; j < res; j++) {
                    Point thisPoint = locatePoint(i, j);
                    if (i + 1 < res) {
                        Point leftPoint = locatePoint(i + 1, j);
                        pointCanvas.Children.Add(new Line {
                            X1 = thisPoint.X,
                            X2 = leftPoint.X,
                            Y1 = thisPoint.Y,
                            Y2 = leftPoint.Y,
                            StrokeThickness = 1,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            Stroke = Brushes.LightGray
                        });

                    }
                    if (j + 1 < res) {
                        Point botPoint = locatePoint(i, j + 1);

                        pointCanvas.Children.Add(new Line {
                            X1 = thisPoint.X,
                            X2 = botPoint.X,
                            Y1 = thisPoint.Y,
                            Y2 = botPoint.Y,
                            StrokeThickness = 1,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            Stroke = Brushes.LightGray
                        });
                    }
                }
            }

            for (int pi = 0; pi < points.Length; pi++) {
                var mdsPoint = points[pi];
                var point = mdsPoint.ToPoint();
                double radius = wasLandmark != null && wasLandmark[pi] ? 6 : 3;
                int i = pi % res;
                int j = pi / res;
                Point relPoint = locatePoint(i, j);
                double r = i / (double)(res - 1);
                double b = j / (double)(res - 1);
                double g = 1.0 - (r + b) / 2;
                SolidColorBrush color = new SolidColorBrush(new Color() { R = (byte)(255 * r), G = (byte)(255 * g), B = (byte)(255 * b), A = 255 });
                color.Freeze();
                Ellipse pointCirc = new Ellipse {
                    Fill = color,
                    Width = radius,
                    Height = radius,
                };
                Canvas.SetLeft(pointCirc, relPoint.X - radius / 2);
                Canvas.SetTop(pointCirc, relPoint.Y - radius / 2);
                pointCanvas.Children.Add(pointCirc);

                /*pointCanvas.Children.Add(new Line {
                    X1 = relPoint.X,
                    X2 = relPoint.X,
                    Y1 = relPoint.Y,
                    Y2 = relPoint.Y,
                    StrokeThickness = 3,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Stroke = Brushes.Black
                });*/
            }
        }

        void CompositionTarget_Rendering(object sender, EventArgs e) {
            if (!needUpdate)
                lock (cycleSync) {
                    double nextVal = lastCycle;
                    if (mdsProgress.Value != nextVal) {
                        mdsProgress.Value = nextVal;
                        labelETA.Content = (TimeSpan.FromSeconds((DateTime.Now - startMDS).TotalSeconds * ((double)(1.0 - lastCycle) / (double)Math.Max(lastCycle, 0.0000000001)))).ToString();// .ToLongTimeString();
                    }
                    if (calcs != null) ShowMdsPoints(calcs);
                    needUpdate = true;
                }
        }
        double lastCycle;
        bool needUpdate = true;
        object cycleSync = new object();

        void ProgressReport(double cycle) {
            if (needUpdate)
                lock (cycleSync) {
                    lastCycle = cycle;
                    // ExtractCalcs(src);//comment out for no gfx
                    needUpdate = false;
                }
        }
        BitArray wasLandmark;
        void ExtractCalcs(MappedDistStruct mappedDists) {
            wasLandmark = new BitArray(origs.Length, false);
            foreach (var mapping in mappedDists.mapper.CurrentMappings)
                wasLandmark[mapping.Key] = true;
            if (calcs == null)
                calcs = new MdsPoint2D[origs.Length];
            double[,] mdsRes = mappedDists.allPoses;
            if (mdsRes.GetLength(1) == 1)
                for (int p = 0; p < origs.Length; p++)
                    calcs[p] = new MdsPoint2D { x = mdsRes[p, 0], y = mdsRes[p, 0] };

            else
                for (int p = 0; p < origs.Length; p++)
                    calcs[p] = new MdsPoint2D { x = mdsRes[p, 0], y = mdsRes[p, 1] };

        }
        DateTime startMDS;


        void CalcMds() {
            NiceTimer timer = new NiceTimer();
            timer.TimeMark("Initializing MDS...");
            Random r = new Random();//12345678);
            MappedDistStruct mappedDists = CreateDistMat(r, timer);
            using (Hitmds mds =
                new Hitmds( 2,
                   mappedDists.distMat,
                    r)) {
                timer.TimeMark("Training MDS");
                startMDS = DateTime.Now;
                mds.mds_train(totalCycles, LEARN_RATE, 0.0, (cyc, tot, src) => {
                    ProgressReport(cyc / (double)tot);
                    // if(needUpdate) lock(cycleSync) ExtractCalcs(src);
                }, POINT_UPDATE_STYLE);
                timer.TimeMark("Extracting points");
                startMDS = DateTime.Now;
                mappedDists.mappedPos = mds.PointPositions();
                mappedDists.TriangulateUnmapped(ProgressReport, origs.Length);
            }
            lock (cycleSync) {
                lastCycle = totalCycles;
                ExtractCalcs(mappedDists);
                needUpdate = false;
            }
            mappedDists = null;
            System.GC.Collect();
            /*
            timer.TimeMark("Calculating Histogram");
            List<double> stats = new List<double>();
            for (int a = 0; a < origs.Length; a++)
                for (int b = 0; b < origs.Length; b++) {
                    var realDist = origs[a].DistanceTo(origs[b]);
                    var predictDist = calcs[a].DistanceTo(calcs[b]);
                    stats.Add(predictDist / realDist);
                }


            var points =
            new Histogrammer(stats, res, 2000)
                .GenerateHistogram(0.01, 0.99)
                .Select(datapoint => new Point { X = datapoint.point, Y = datapoint.density })
                .ToArray();
            histo.Dispatcher.BeginInvoke((Action)delegate {
                var graph = histo.NewGraph("DistancesDistribution", points);
                histo.ShowGraph(graph);
            });
            /**/
            timer.Done();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e) {
            /*
             *         const int res = 110; //80
        const int SUBSET_SIZE = 10000; //1000
        const int GEN = 60; //30
        const int POINT_UPDATE_STYLE = 1;
        const double LEARN_RATE = 2.0;
        const double DIST_LIMIT_AVG = 5.0;
        const double DIST_LIMIT_RAND = 0.5;
        const double DIST_NOISE = 0.2;
        const float INF_REPLACEMENT_FACTOR = 10.0f;
*/
            var saveDialog = new SaveFileDialog() {
                Title = "Save Embedding As ...",
                Filter = "XPS file|*.xps",
                FileName = "L"+SUBSET_SIZE+"R"+res+"NS"+(int)(DIST_NOISE*100)+ "N"+GEN+"LR"+((int)(LEARN_RATE*1000))+"PU"+POINT_UPDATE_STYLE+".xps",
            };
            if (saveDialog.ShowDialog() == true) {
                using (var writestream = new FileStream(saveDialog.FileName, FileMode.Create, FileAccess.ReadWrite))
                    WpfTools.PrintXPS(pointViewbox, 300, 300, writestream, FileMode.Create, FileAccess.ReadWrite);
            }

        }

    }

    struct MdsPoint2D
    {
        public double x, y;
        public double DistanceTo(MdsPoint2D other) {
            return Math.Sqrt((x - other.x) * (x - other.x) + (y - other.y) * (y - other.y));
        }
        public Point ToPoint() { return new Point(x, y); }

    }
    public static class RndHelper
    {
        public static double NextNorm(this Random r) {//from http://www.cs.princeton.edu/introcs/21function/MyMath.java.html
            return Math.Sin(2 * Math.PI * r.NextDouble()) * Math.Sqrt((-2 * Math.Log(1 - r.NextDouble())));
        }
    }

}
