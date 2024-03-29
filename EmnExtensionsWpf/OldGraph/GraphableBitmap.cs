using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EmnExtensions.Wpf.Plot;

namespace EmnExtensions.Wpf.OldGraph
{
    public class GraphableBitmap : GraphableDrawing
    {
        BitmapSource bmp;

        /// <summary>
        /// Sets DrawingRect + IrrelevantDrawingMargins for you; you should set RelevantDataBounds to Rect describing the data in the image youself.
        /// for an image of WxH pixels, pixel (0,0) corresponds to the top-left relevant data bound, and  pixel (W-1,H-1) to the bottom right.
        /// </summary>
        public BitmapSource Bitmap
        {
            get => bmp;
            set {
                if (bmp != value) {
                    bmp = value;
                    OnChange(GraphChange.Drawing);
                    OnChange(GraphChange.Projection);
                }
            }
        }

        public Rect InnerDataBounds
        {
            get => Rect.Transform(DataBounds, GraphUtils.TransformShape(DrawingRect, InnerDrawingRect, false));
            set => DataBounds = ComputeDataBounds(InnerDrawingRect, value, DrawingRect);
        }

        protected override Rect DrawingRect
            => new(0, 0, bmp.Width, bmp.Height);

        protected Rect InnerDrawingRect
            => new(0.5 * (bmp.Width / bmp.PixelWidth), 0.5 * (bmp.Height / bmp.PixelHeight), bmp.Width - bmp.Width / bmp.PixelWidth, bmp.Height - bmp.Height / bmp.PixelHeight);

        protected override void DrawUntransformedIntoDrawingRect(DrawingContext context)
            => context.DrawImage(bmp, DrawingRect);
    }
}
