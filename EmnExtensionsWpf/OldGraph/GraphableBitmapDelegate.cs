using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EmnExtensions.Wpf.Plot;

namespace EmnExtensions.Wpf.OldGraph
{
    public class GraphableBitmapDelegate : GraphableResizeableBitmap
    {
        public GraphableBitmapDelegate()
            => UpdateBitmapDelegate = DefaultUpdateBitmapDelegate;

        static void DefaultUpdateBitmapDelegate(WriteableBitmap bmp, Matrix mat, int pixelWidth, int pixelHeight) { }

        protected override Rect? OuterDataBound
            => m_OuterDataBound;

        public Rect? MaximalDataBound
        {
            get => m_OuterDataBound;
            set {
                m_OuterDataBound = value;
                OnChange(GraphChange.Projection);
            }
        }

        Rect? m_OuterDataBound;

        /// <summary>
        /// This delegate is called whenever the bitmap needs to be updated.
        /// The first parameter is the bitmap that needs to be written to (eventual locking is the responsibility of the client code).
        /// The second parameter is the matrix projecting data point to pixel coordinates (to project pixel coordinates to data space, it must be inverted)
        /// The third and forth parater are the width and height (respectively) of the region in the bitmap that is onscreen.  This region may be smaller than the overall writeable bitmap, and always starts at 0,0.
        /// </summary>
        public Action<WriteableBitmap, Matrix, int, int> UpdateBitmapDelegate { get; set; }

        protected override void UpdateBitmap(int pW, int pH, Matrix dataToBitmap)
            => UpdateBitmapDelegate(m_bmp, dataToBitmap, pW, pH);
    }
}
