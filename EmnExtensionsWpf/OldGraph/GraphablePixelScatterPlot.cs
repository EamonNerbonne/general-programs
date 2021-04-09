using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EmnExtensions.Wpf.Plot
{
    public class GraphablePixelScatterPlot : GraphableData
    {
        public GraphablePixelScatterPlot() { Margin = new Thickness(0.5); BitmapScalingMode = BitmapScalingMode.Linear; }
        double m_dpiX = 96.0, m_dpiY = 96.0;
        bool m_useDiamondPoints;
        public bool UseDiamondPoints { get { return m_useDiamondPoints; } set { m_useDiamondPoints = value; OnChange(GraphChange.Projection); } }
        public double DpiX { get { return m_dpiX; } set { if (value != m_dpiX) { m_dpiX = value; OnChange(GraphChange.Projection); } } }
        public double DpiY { get { return m_dpiY; } set { if (value != m_dpiY) { m_dpiY = value; OnChange(GraphChange.Projection); } } }
        public BitmapScalingMode BitmapScalingMode { get { return m_scalingMode; } set { m_scalingMode = value; if (m_bmp != null) RenderOptions.SetBitmapScalingMode(m_bmp, value); } }
        BitmapScalingMode m_scalingMode;

        WriteableBitmap m_bmp;
        RectangleGeometry m_clipGeom = new RectangleGeometry();
        TranslateTransform m_offsetTransform = new TranslateTransform();
        const int EXTRA_RESIZE_PIX = 256;
        Point[] m_points;
        Rect m_outerBounds = Rect.Empty;
        public Point[] Points { get { return m_points; } set { if (value != m_points) { m_points = value; DataBounds = ComputeBounds(); OnChange(GraphChange.Projection); } } }
        double m_coverage = 1.0;
        public double CoverageRatio { get { return m_coverage; } set { if (value != m_coverage) { m_coverage = value; DataBounds = ComputeBounds(); } } }

        private Rect ComputeBounds()
        {
            if (m_points == null || m_points.Length == 0)
                return Rect.Empty;
            else {
                var cutoff = (int)(0.5 + 0.5 * (1.0 - m_coverage) * m_points.Length);
                m_outerBounds = Rect.Empty;
                foreach (var point in m_points)
                    m_outerBounds.Union(point);
                if (cutoff == 0) {
                    return m_outerBounds;
                } else if (cutoff * 2 >= m_points.Length) {
                    return Rect.Empty;
                } else {
                    var xs = new double[m_points.Length];
                    var ys = new double[m_points.Length];
                    for (var i = 0; i < m_points.Length; i++) {
                        xs[i] = m_points[i].X;
                        ys[i] = m_points[i].Y;
                    }
                    Array.Sort(xs);
                    Array.Sort(ys);
                    return new Rect(new Point(xs[cutoff], ys[cutoff]), new Point(xs[m_points.Length - 1 - cutoff], ys[m_points.Length - 1 - cutoff]));
                }
            }
        }

        Color m_pointColor;
        public Color PointColor { get { return m_pointColor; } set { if (value != m_pointColor) { m_pointColor = value; OnChange(GraphChange.Projection); } } }

        public override void DrawGraph(System.Windows.Media.DrawingContext context)
        {
            if (m_bmp == null)
                return;
            context.PushGuidelineSet(new GuidelineSet(new[] { 0.0 }, new[] { 0.0 }));
            context.PushClip(m_clipGeom);
            context.PushTransform(m_offsetTransform);
            context.DrawImage(m_bmp, new Rect(0, 0, m_bmp.Width, m_bmp.Height));
            context.Pop();
            context.Pop();
            context.Pop();
#if TRACE
            Console.WriteLine("redraw");
#endif
        }

        static Rect SnapRect(Rect r, double multX, double multY) { return new Rect(new Point(Math.Floor(r.Left / multX) * multX, Math.Floor(r.Top / multY) * multY), new Point(Math.Ceiling((r.Right + 0.01) / multX) * multX, Math.Ceiling((r.Bottom + 0.01) / multY) * multY)); }

        uint[] image;
        public override void SetTransform(Matrix matrix, Rect displayClip)
        {
            if (matrix.IsIdentity) {
                return;//TODO: clear bitmap??
            }

            var outerDispBounds = Rect.Transform(m_outerBounds, matrix);
            outerDispBounds.Intersect(displayClip);
            outerDispBounds = SnapRect(outerDispBounds, 96.0 / m_dpiX, 96.0 / m_dpiX);

            var pW = (int)Math.Ceiling(outerDispBounds.Width * m_dpiX / 96.0);
            var pH = (int)Math.Ceiling(outerDispBounds.Height * m_dpiY / 96.0);
            if (image == null || image.Length < pW * pH)
                image = new uint[pW * pH];
            else
                for (var i = 0; i < pW * pH; i++)
                    image[i] = 0;
            //image.Count();
            matrix.Translate(-outerDispBounds.X, -outerDispBounds.Y);
            var xScale = m_dpiX / 96.0;
            var yScale = m_dpiY / 96.0;
            matrix.Scale(xScale, yScale);
            if (UseDiamondPoints) //for performance, if-lifted out of loop.
                foreach (var point in Points) {
                    var displaypoint = matrix.Transform(point);

                    var x = (int)(displaypoint.X);// - outerDispBounds.X) * m_dpiX / 96.0);
                    var y = (int)(displaypoint.Y);// - outerDispBounds.Y) * m_dpiY / 96.0);
                    if (x >= 1 && x < pW - 1 && y >= 1 && y < pH - 1) {
                        image[x + pW * y]++;
                        image[x - 1 + pW * y]++;
                        image[x + 1 + pW * y]++;
                        image[x + pW * (y - 1)]++;
                        image[x + pW * (y + 1)]++;
                    }
                }
            else //non-diamond case 
                foreach (var point in Points) {
                    var displaypoint = matrix.Transform(point);
                    var x = (int)(displaypoint.X);
                    var y = (int)(displaypoint.Y);
                    if (x >= 0 && x < pW && y >= 0 && y < pH)
                        image[x + pW * y]++;
                } // so now we've counted the number of pixels in each position...

            uint maxOverlap = 0;
            for (var i = 0; i < pW * pH; i++)
                if (image[i] > maxOverlap)
                    maxOverlap = image[i];
            var alphaLookup = new uint[maxOverlap + 1];
            for (var i = 0; i < alphaLookup.Length; i++)
                alphaLookup[i] = ((uint)((1.0 - Math.Pow(1.0 - PointColor.ScA, i)) * 255.5) << 24);

            var nativeColor = PointColor.ToNativeColor() & 0x00ffffff;
            var transparency = 1.0 - PointColor.ScA;
            for (var pxI = 0; pxI < pW * pH; pxI++)
                image[pxI] = nativeColor | alphaLookup[image[pxI]]; // ((uint)((1.0 - Math.Pow(transparency, image[pxI])) * 255.5) << 24);

            if (m_bmp == null || m_bmp.PixelWidth < pW || m_bmp.PixelHeight < pH) {
                var width = Math.Max(m_bmp == null ? 1 : m_bmp.PixelWidth, pW + (int)(EXTRA_RESIZE_PIX * m_dpiX / 96.0));
                var height = Math.Max(m_bmp == null ? 1 : m_bmp.PixelHeight, pH + (int)(EXTRA_RESIZE_PIX * m_dpiY / 96.0));
                m_bmp = new WriteableBitmap(width, height, m_dpiX, m_dpiY, PixelFormats.Bgra32, null);
                RenderOptions.SetBitmapScalingMode(m_bmp, m_scalingMode);
                OnChange(GraphChange.Drawing);
                Trace.WriteLine("new WriteableBitmap");
            }

            try {
                m_bmp.Lock();
                m_bmp.WritePixels(new Int32Rect(0, 0, pW, pH), image, pW * sizeof(uint), 0);
            } finally {
                m_bmp.Unlock();
            }


            if (m_offsetTransform.X != outerDispBounds.X || m_offsetTransform.Y != outerDispBounds.Y) {
                m_offsetTransform.X = outerDispBounds.X;
                m_offsetTransform.Y = outerDispBounds.Y;
            }

            m_clipGeom.Rect = outerDispBounds;
            //painting.
            Trace.WriteLine("retransform");
        }
    }
}
