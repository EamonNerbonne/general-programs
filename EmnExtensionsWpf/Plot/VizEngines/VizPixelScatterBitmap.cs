using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.VizEngines {
    public class VizPixelScatterBitmap : VizDynamicBitmap<Point[]>, IVizPixelScatter
        //for efficiency reasons, accept data in a Point[] rather than the more general IEnumerable<Point>
    {
        Rect m_OuterDataBounds = Rect.Empty;
        uint[] m_image;

        protected override Rect? OuterDataBound { get { return m_OuterDataBounds; } }
        double m_CoverageRatio = 0.9999;
        public double CoverageRatio { get { return m_CoverageRatio; } set { if (value != m_CoverageRatio) { m_CoverageRatio = value; InvalidateDataBounds(); } } }

        double m_CoverageGradient = 5.0;
        public double CoverageGradient { get { return m_CoverageGradient; } set { m_CoverageGradient = value; InvalidateDataBounds(); } }

        public int? OverridePointCountEstimate { get; set; }

        public VizPixelScatterBitmap(IPlotMetaData metadata) : base(metadata) { }


        protected override void UpdateBitmap(int pW, int pH, Matrix dataToBitmap, double viewAreaSize) {
            Trace.WriteLine("UpdateBitmap");

            if (dataToBitmap.IsIdentity) return;//this is the default mapping; it may occur when generating a scatter plot without data - don't bother plotting.

            double thickness = MetaData.RenderThickness ?? VizPixelScatterHelpers.PointCountToThickness(OverridePointCountEstimate ?? (Data == null ? 0 : Data.Length));
            Tuple<double, bool> thicknessTranslation = DecodeThickness(thickness);

            Make2dHistogramInRegion(pW, pH, dataToBitmap, thicknessTranslation.Item2);
            ConvertHistogramToColorDensityImage(pW, pH, thicknessTranslation.Item1);
            CopyImageRegionToWriteableBitmap(pW, pH);
        }

        static Tuple<double, bool> DecodeThickness(double thickness) {
            double thicknessOfSquare = VizPixelScatterHelpers.SquareSidePerThickness * thickness;
            //thicknessOfSquare 1.0 is equivalent to a 1x1 opaque pixel square.
            double alpha = thicknessOfSquare * thicknessOfSquare;

            bool useDiamondPoints = alpha > 1.0;
            if (useDiamondPoints)
                alpha = Math.Min(1.0, alpha / 5.0);

            return new Tuple<double, bool>(alpha, useDiamondPoints);
        }

        #region UpdateBitmap Helpers
        void Make2dHistogramInRegion(int pW, int pH, Matrix dataToBitmap, bool useDiamondPoints) {
            MakeVisibleRegionEmpty(pW, pH);

            if (useDiamondPoints)
                MakeDiamondPoint2dHistogram(pW, pH, dataToBitmap);
            else
                MakeSinglePoint2dHistogram(pW, pH, dataToBitmap);
        }

        void MakeVisibleRegionEmpty(int pW, int pH) {
            if (m_image == null || m_image.Length < pW * pH)
                m_image = new uint[pW * pH];
            else
                for (int i = 0; i < pW * pH; i++)
                    m_image[i] = 0;
        }

        void MakeDiamondPoint2dHistogram(int pW, int pH, Matrix dataToBitmap) {
            foreach (var point in Data) {
                Point displaypoint = dataToBitmap.Transform(point);
                int x = (int)(displaypoint.X);
                int y = (int)(displaypoint.Y);
                if (x >= 1 && x < pW - 1 && y >= 1 && y < pH - 1) {
                    m_image[x + pW * y]++;
                    m_image[x - 1 + pW * y]++;
                    m_image[x + 1 + pW * y]++;
                    m_image[x + pW * (y - 1)]++;
                    m_image[x + pW * (y + 1)]++;
                }
            }
        }

        void MakeSinglePoint2dHistogram(int pW, int pH, Matrix dataToBitmap) {
            foreach (var point in Data) {
                Point displaypoint = dataToBitmap.Transform(point);
                int x = (int)(displaypoint.X);
                int y = (int)(displaypoint.Y);
                if (x >= 0 && x < pW && y >= 0 && y < pH)
                    m_image[x + pW * y]++;
            }
        }

        void ConvertHistogramToColorDensityImage(int pW, int pH, double alpha) {
            Color pointColor = MetaData.RenderColor ?? Colors.Black;

            int numPixels = pW * pH;
            uint[] alphaLookup = PregenerateAlphaLookup(alpha, m_image, numPixels, pointColor.ScA);
            uint nativeColorWithoutAlpha = pointColor.ToNativeColor() & 0x00ffffff;

            for (int pxI = 0; pxI < numPixels; pxI++)
                m_image[pxI] = nativeColorWithoutAlpha | alphaLookup[m_image[pxI]];
        }

        void CopyImageRegionToWriteableBitmap(int pW, int pH) {
            m_bmp.WritePixels(
                sourceRect: new Int32Rect(0, 0, pW, pH),
                sourceBuffer: m_image,
                sourceBufferStride: pW * sizeof(uint),
                destinationX: 0,
                destinationY: 0);
        }

        static uint[] PregenerateAlphaLookup(double alpha, uint[] image, int numPixels, double overallAlpha) {
            uint maximalOverlapCount = ValueOfMax(image, 0, numPixels);
            return MakeAlphaLookupUpto(alpha, maximalOverlapCount, overallAlpha);
        }

        static uint[] MakeAlphaLookupUpto(double alpha, uint maxOverlap, double overallAlpha) {
            double transparencyPerOverlap = 1.0 - alpha;
            uint[] alphaLookup = new uint[maxOverlap + 1];
            for (int overlap = 0; overlap < alphaLookup.Length; overlap++) {
                double overlappingAlpha = overallAlpha * (1.0 - Math.Pow(transparencyPerOverlap, overlap));
                alphaLookup[overlap] = (uint)(overlappingAlpha * 255.5) << 24;
            }
            return alphaLookup;
        }

        static uint ValueOfMax(uint[] m_image, int start, int end) {
            uint maxCount = 0;
            for (int i = start; i < end; i++)
                if (m_image[i] > maxCount)
                    maxCount = m_image[i];
            return maxCount;
        }
        #endregion

        protected override void OnDataChanged(Point[] oldData) {
            InvalidateDataBounds();
            m_OuterDataBounds = VizPixelScatterHelpers.ComputeOuterBounds(Data);
            TriggerChange(GraphChange.Projection); //because we need to relayout the points in the plot
        }

        protected override Rect ComputeBounds() {
            return VizPixelScatterHelpers.ComputeInnerBoundsByRatio(Data, CoverageRatio, CoverageRatio, CoverageGradient, m_OuterDataBounds);
        }

        public override void OnRenderOptionsChanged() {
            TriggerChange(GraphChange.Projection); // because we need to relayout the points in the plot.
        }

        public override bool SupportsColor { get { return true; } }
    }
}