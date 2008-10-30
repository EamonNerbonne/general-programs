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

namespace MdsTestWpf
{
    struct MdsPoint2D
    {
        public double x, y;
        public double DistanceTo(MdsPoint2D other) {
            return Math.Sqrt((x - other.x) * (x - other.x) + (y - other.y) * (y - other.y));
        }
        public Point ToPoint() { return new Point(x, y); }

    }

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MdsDisplay: Window
    {
        const int res = 50;
        int IndexFromIJ(int i, int j) {
            return i + res * j;
        }
        MdsPoint2D[] origs,calcs;
        int totalCycles = 0;
        public MdsDisplay() {
            InitializeComponent();
            origs = new MdsPoint2D[res * res];
            for (int i = 0; i < res; i++)
                for (int j = 0; j < res; j++)
                    origs[IndexFromIJ(i, j)] = new MdsPoint2D { x = i, y = j };
            totalCycles = origs.Length * 5;
            mdsProgress.Maximum = totalCycles;
            mdsProgress.Minimum = 0;
            Thread t = new Thread(CalcMds) {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            t.Start();
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
            ShowMdsPoints(origs);    
        }

        Rect BoundMdsPoints(MdsPoint2D[] points) {
            Rect viewRect= new Rect(points[0].ToPoint(),Size.Empty);
            foreach (var point in points) {
                viewRect.Union(point.ToPoint());
            }
            return viewRect;
        }

        void ShowMdsPoints(MdsPoint2D[] points) {
            pointCanvas.Children.Clear();
            Rect boundingBox = BoundMdsPoints(points);
            double scaleFactor = 1000/Math.Max(boundingBox.Width,boundingBox.Height);
            pointCanvas.Width =scaleFactor* boundingBox.Width;
            pointCanvas.Height = scaleFactor* boundingBox.Height;
            Point topLeft = boundingBox.TopLeft;
            Func<int, int, Point> locatePoint =(i, j) => (Point)(scaleFactor * (points[IndexFromIJ(i, j)].ToPoint() - topLeft));
            for (int i = 0; i < res; i++) {
                for (int j = 0; j < res; j++) {
                    Point thisPoint = locatePoint(i,j);
                    if (i+1 < res) {
                        Point leftPoint = locatePoint(i+1, j);
                        pointCanvas.Children.Add(new Line {
                            X1 = thisPoint.X,
                            X2 = leftPoint.X,
                            Y1 = thisPoint.Y,
                            Y2 = leftPoint.Y,
                            StrokeThickness = 1,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            Stroke = Brushes.LightBlue
                        });

                    }
                    if (j + 1 < res) {
                        Point botPoint = locatePoint(i, j+1);

                        pointCanvas.Children.Add(new Line {
                            X1 = thisPoint.X,
                            X2 = botPoint.X,
                            Y1 = thisPoint.Y,
                            Y2 = botPoint.Y,
                            StrokeThickness = 1,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            Stroke = Brushes.LightBlue
                        });
                    }
                }
            }
                    
            foreach (var point in points.Select(mdsPoint => mdsPoint.ToPoint())) {
                Point relPoint = (Point)(scaleFactor*(point - topLeft));
                pointCanvas.Children.Add(new Line {
                    X1 = relPoint.X,
                    X2 = relPoint.X,
                    Y1 = relPoint.Y,
                    Y2= relPoint.Y,
                    StrokeThickness =3,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Stroke = Brushes.Black
                });
            }
        }

        void CompositionTarget_Rendering(object sender, EventArgs e) {
            RedrawProgress();
        }
        int lastCycle;
        object cycleSync=new object();


        void RedrawProgress() {
            double nextVal;
            lock (cycleSync)
                nextVal = lastCycle;
            if (mdsProgress.Value != nextVal)
                mdsProgress.Value = nextVal;
        }
        void ProgressReport(int cycle, int total) {
            lock(cycleSync) 
                lastCycle=cycle;
        }

        void CalcMds() {
            Random r = new Random();

            using (Hitmds mds = new Hitmds(origs.Length, 2, (i, j) => (float)(origs[i].DistanceTo(origs[j]) + 2* (r.NextDouble() - 0.5)))) {
                mds.mds_train(totalCycles, 1.0,ProgressReport);
                calcs = new MdsPoint2D[origs.Length];
                for(int p=0;p<origs.Length;p++) {
                    calcs[p] = new MdsPoint2D { x = mds.GetPoint(p,0), y = mds.GetPoint(p,1)};
                }
            }
            Dispatcher.Invoke((Action)(() => { ShowMdsPoints(calcs); }));

        }
    }
}
