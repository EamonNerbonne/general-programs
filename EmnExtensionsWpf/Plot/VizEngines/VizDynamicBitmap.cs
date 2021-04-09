using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EmnExtensions.Wpf.VizEngines
{
    public abstract class VizDynamicBitmap<T> : PlotVizBase<T>
    {
        protected VizDynamicBitmap(IPlotMetaData owner) : base(owner) => BitmapScalingMode = BitmapScalingMode.Linear;

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

        //DrawingGroup painting = new DrawingGroup();
        const int EXTRA_RESIZE_PIX = 128;
        protected WriteableBitmap m_bmp;
        readonly RectangleGeometry m_clipGeom = new RectangleGeometry();
        readonly MatrixTransform m_bitmapToDisplayTransform = new MatrixTransform();
        readonly DrawingGroup m_drawing = new DrawingGroup();

        public sealed override void DrawGraph(DrawingContext context)
        {
            Trace.WriteLine("redraw");
            context.DrawDrawing(m_drawing);
        }

        static Rect SnapRect(Rect r, double multX, double multY) => new Rect(new Point(Math.Floor(r.Left / multX) * multX, Math.Floor(r.Top / multY) * multY), new Point(Math.Ceiling((r.Right + 0.01) / multX) * multX, Math.Ceiling((r.Bottom + 0.01) / multY) * multY));

        public sealed override void SetTransform(Matrix dataToDisplay, Rect displayClip, double dpiX, double dpiY)
        {
            if (displayClip.IsEmpty) {
                using (m_drawing.Open()) {
                    return;
                }
            }

            double scaleX = dpiX / 96.0, scaleY = dpiY / 96.0;

            var drawingClip = ComputeRelevantDisplay(displayClip, OuterDataBound, dataToDisplay);

            var snappedDrawingClip = SnapRect(drawingClip, 1.0 / scaleX, 1.0 / scaleY);

            var dataToBitmapToDisplay = SplitDataToDisplay(dataToDisplay, snappedDrawingClip, dpiX, dpiY);

            m_bitmapToDisplayTransform.Matrix = dataToBitmapToDisplay.Item2;

            m_clipGeom.Rect = snappedDrawingClip;
            //This clips to nearest pixel boundary; but a tighter clip is possible to sub-pixel accuracy:
            //m_clipGeom.Rect = drawingClip;


            var pW = (int)(0.5 + snappedDrawingClip.Width * scaleX);
            var pH = (int)(0.5 + snappedDrawingClip.Height * scaleY);
            if (m_bmp == null || m_bmp.PixelWidth < pW || m_bmp.PixelHeight < pH || dpiX != m_bmp.DpiX || dpiY != m_bmp.DpiY) {
                var width = Math.Max(m_bmp == null ? 1 : m_bmp.PixelWidth, pW + EXTRA_RESIZE_PIX);
                var height = Math.Max(m_bmp == null ? 1 : m_bmp.PixelHeight, pH + EXTRA_RESIZE_PIX);
                m_bmp = new WriteableBitmap(width, height, dpiX, dpiY, PixelFormats.Bgra32, null);
                using (var context = m_drawing.Open()) {
                    context.PushGuidelineSet(new GuidelineSet(new[] { 0.0 }, new[] { 0.0 }));
                    context.PushClip(m_clipGeom);
                    context.PushTransform(m_bitmapToDisplayTransform);
                    context.DrawImage(m_bmp, new Rect(0, 0, m_bmp.Width, m_bmp.Height));
                    context.Pop();
                    context.Pop();
                    context.Pop();
                }

                Trace.WriteLine("new WriteableBitmap");
            }

            UpdateBitmap(pW, pH, dataToBitmapToDisplay.Item1, displayClip.Width * displayClip.Height * scaleX * scaleY);
            //painting.
            Trace.WriteLine("retransform");
        }

        static Rect ComputeRelevantDisplay(Rect clip, Rect? dataBounds, Matrix dataToDisplay)
        {
            if (dataBounds.HasValue) {
                clip.Intersect(Rect.Transform(dataBounds.Value, dataToDisplay));
            }

            return clip;
        }

        static Tuple<Matrix, Matrix> SplitDataToDisplay(Matrix dataToDisplay, Rect snappedDrawingClip, double dpiX, double dpiY)
        {
            var dataToBitmap = dataToDisplay;
            dataToBitmap.Translate(-snappedDrawingClip.X, -snappedDrawingClip.Y); //transform real-location --> coordinates
            dataToBitmap.Scale(dpiX / 96.0, dpiY / 96.0); //transform from abstract units --> pixels

            var bitmapToDisplay = Matrix.Identity;
            //bitmapToDisplay.Scale(96.0 / dpiX, 96.0 / dpiY); //transform pixels --> abstract units; not necessary, already done by WriteableBitmap's DPI setting.
            bitmapToDisplay.Translate(snappedDrawingClip.X, snappedDrawingClip.Y); //transform coordinates --> real-location

            return Tuple.Create(dataToBitmap, bitmapToDisplay);
        }

        protected abstract void UpdateBitmap(int pW, int pH, Matrix dataToBitmap, double externalViewArea);

        //DataBound includes the portion of the data to display; may exclude irrelevant portions.  
        //The actual display may be larger due to various reasons and that can be inefficient.
        //We can't clip to DataBound, however, since _if_ there's more space leaving out irrelevant portions is misleading (cut-off scatter plots, etc.)
        //OuterDataBound is the utmost outer bound:
        //providing one is an optimization that permits using a smaller bitmap; the rest of the drawing is just left blank then.
        //if you don't provide an OuterDataBound, the entire display clip will be available as a WriteableBitmap.
        protected abstract Rect? OuterDataBound { get; }

        public override bool SupportsColor => false;
    }
}
