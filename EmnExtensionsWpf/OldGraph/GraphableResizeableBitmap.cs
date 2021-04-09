using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EmnExtensions.Wpf.OldGraph
{
    public abstract class GraphableResizeableBitmap : GraphableData
    {
        protected GraphableResizeableBitmap() => BitmapScalingMode = BitmapScalingMode.Linear;

        public BitmapScalingMode BitmapScalingMode
        {
            get => m_scalingMode;
            set {
                m_scalingMode = value;
                if (m_drawing != null) {
                    RenderOptions.SetBitmapScalingMode(m_drawing, value);
                }
            }
        }

        BitmapScalingMode m_scalingMode;

        const int EXTRA_RESIZE_PIX = 256;
        readonly double m_dpiX = 96.0;
        readonly double m_dpiY = 96.0;
        protected WriteableBitmap m_bmp;
        readonly RectangleGeometry m_clipGeom = new();
        readonly TranslateTransform m_offsetTransform = new();
        readonly DrawingGroup m_drawing = new();

        public override void DrawGraph(DrawingContext context)
        {
            context.DrawDrawing(m_drawing);
            Trace.WriteLine("redraw");
        }

        protected abstract Rect? OuterDataBound { get; }

        static Rect SnapRect(Rect r, double multX, double multY) => new(new(Math.Floor(r.Left / multX) * multX, Math.Floor(r.Top / multY) * multY), new Point(Math.Ceiling((r.Right + 0.01) / multX) * multX, Math.Ceiling((r.Bottom + 0.01) / multY) * multY));

        public override void SetTransform(Matrix dataToDisplay, Rect displayClip)
        {
            if (dataToDisplay.IsIdentity) //TODO: is this a good test for no-show?
            {
                using (m_drawing.Open()) {
                    return;
                }
            }

            var drawingClip = displayClip;
            var outerDataBound = OuterDataBound;
            if (outerDataBound.HasValue) {
                drawingClip.Intersect(Rect.Transform(outerDataBound.Value, dataToDisplay));
            }

            var snappedDrawingClip = SnapRect(drawingClip, 96.0 / m_dpiX, 96.0 / m_dpiY);
            var pW = (int)Math.Ceiling(snappedDrawingClip.Width * m_dpiX / 96.0);
            var pH = (int)Math.Ceiling(snappedDrawingClip.Height * m_dpiY / 96.0);

            var dataToBitmap = dataToDisplay;
            dataToBitmap.Translate(-snappedDrawingClip.X, -snappedDrawingClip.Y);
            dataToBitmap.Scale(m_dpiX / 96.0, m_dpiY / 96.0);

            if (m_offsetTransform.X != snappedDrawingClip.X || m_offsetTransform.Y != snappedDrawingClip.Y) {
                m_offsetTransform.X = snappedDrawingClip.X;
                m_offsetTransform.Y = snappedDrawingClip.Y;
            }

            m_clipGeom.Rect = snappedDrawingClip; //TODO: maybe better to clip after transform and then to clip to pW/pH?
            //TODO2: this clips to nearest pixel boundary; but a tighter clip is possible to sub-pixel accuracy.

            if (m_bmp == null || m_bmp.PixelWidth < pW || m_bmp.PixelHeight < pH) {
                var width = Math.Max(m_bmp?.PixelWidth ?? 1, pW + EXTRA_RESIZE_PIX);
                var height = Math.Max(m_bmp?.PixelHeight ?? 1, pH + EXTRA_RESIZE_PIX);
                m_bmp = new(width, height, m_dpiX, m_dpiY, PixelFormats.Bgra32, null);
                using (var context = m_drawing.Open()) {
                    context.PushGuidelineSet(new(new[] { 0.0 }, new[] { 0.0 }));
                    context.PushClip(m_clipGeom);
                    context.PushTransform(m_offsetTransform);
                    context.DrawImage(m_bmp, new(0, 0, m_bmp.Width, m_bmp.Height));
                    context.Pop();
                    context.Pop();
                    context.Pop();
                }

                Trace.WriteLine("new WriteableBitmap");
            }

            UpdateBitmap(pW, pH, dataToBitmap);
            //painting.
            Trace.WriteLine("retransform");
        }

        protected abstract void UpdateBitmap(int pW, int pH, Matrix dataToBitmap);
    }
}
