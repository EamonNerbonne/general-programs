using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.VizEngines
{
    public class VizPointCloudBitmap : VizDynamicBitmap<LabelledPoint[]>
        //for efficiency reasons, accept data in a Point[] rather than the more general IEnumerable<Point>
    {
        struct UintColor
        {
            public uint R, G, B;

            public UintColor(Color c)
            {
                R = c.R;
                G = c.G;
                B = c.B;
            }

            public static UintColor operator +(UintColor a, UintColor b)
            {
                var result = default(UintColor);
                result.R = a.R + b.R;
                result.G = a.G + b.G;
                result.B = a.B + b.B;
                return result;
            }

            public void Increment(UintColor other)
            {
                R += other.R;
                G += other.G;
                B += other.B;
            }

            public uint RawDiv(uint divisor) => divisor == 0 ? 0u : (R / divisor << 16) + (G / divisor << 8) + (B / divisor);
        }

        Rect m_OuterDataBounds = Rect.Empty;
        uint[] m_image;
        UintColor[] m_accumulator;

        public VizPointCloudBitmap(IPlotMetaData metadata) : base(metadata) { }

        protected override Rect? OuterDataBound => m_OuterDataBounds;
        double m_CoverageRatio = 0.9999;

        public double CoverageRatio
        {
            get => m_CoverageRatio;
            set {
                if (value != m_CoverageRatio) {
                    m_CoverageRatio = value;
                    InvalidateDataBounds();
                }
            }
        }

        double m_CoverageGradient = 5.0;

        public double CoverageGradient
        {
            get => m_CoverageGradient;
            set {
                m_CoverageGradient = value;
                InvalidateDataBounds();
            }
        }

        Color[] m_ClassColors;
        UintColor[] m_MappedColors;

        public Color[] ClassColors
        {
            get => m_ClassColors;
            set {
                m_ClassColors = value;
                RecomputeColors();
            }
        }

        void RecomputeColors()
        {
            m_MappedColors = m_ClassColors == null ? null : m_ClassColors.Select(c => new UintColor(c)).ToArray();
            OnRenderOptionsChanged();
        }

        public int? OverridePointCountEstimate { get; set; }

        protected override void UpdateBitmap(int pW, int pH, Matrix dataToBitmap, double viewAreaSize)
        {
            Trace.WriteLine("UpdateBitmap");

            if (dataToBitmap.IsIdentity || m_ClassColors == null || Data == null || Data.Length == 0) {
                return; //this is the default mapping; it may occur when generating a scatter plot without data - don't bother plotting.
            }

            var thickness = MetaData.RenderThickness ?? VizPixelScatterHelpers.PointCountToThickness(OverridePointCountEstimate ?? (Data == null ? 0 : Data.Length));
            //Console.WriteLine(thickness + " * " + Math.Sqrt(viewAreaSize / 90000.0));
            thickness *= Math.Sqrt(viewAreaSize / 90000.0);
            var thicknessTranslation = DecodeThickness(thickness);

            Make2dHistogramInRegion(pW, pH, dataToBitmap, thicknessTranslation.Item2);
            ConvertHistogramToColorDensityImage(pW, pH, thicknessTranslation.Item1);
            CopyImageRegionToWriteableBitmap(pW, pH);
        }

        static Tuple<double, int> DecodeThickness(double thickness)
        {
            var thicknessOfSquare = VizPixelScatterHelpers.SquareSidePerThickness * thickness;
            //thicknessOfSquare 1.0 is equivalent to a 1x1 opaque pixel square.
            var alpha = thicknessOfSquare * thicknessOfSquare;

            if (alpha <= 1.0) {
                return Tuple.Create(alpha, 0);
            }

            if (alpha <= 1.2) {
                return Tuple.Create(1.0, 0);
            }

            if (alpha <= 2.5) {
                return Tuple.Create(alpha / 6.0, 1);
            }

            if (alpha <= 5.0) {
                return Tuple.Create(alpha / 5.0, 2);
            }

            if (alpha <= 6.0) {
                return Tuple.Create(1.0, 2);
            }

            if (alpha <= 10.0) {
                return Tuple.Create(alpha / 10.0, 3);
            }

            if (alpha <= 12.0) {
                return Tuple.Create(1.0, 3);
            }

            if (alpha <= 21.0) {
                return Tuple.Create(alpha / 21.0, 4);
            }

            return Tuple.Create(1.0, 4);
        }

        #region UpdateBitmap Helpers

        void Make2dHistogramInRegion(int pW, int pH, Matrix dataToBitmap, int addpixelMode)
        {
            MakeVisibleRegionEmpty(pW, pH);

            if (addpixelMode == 0) {
                MakeSinglePoint2dHistogram(pW, pH, dataToBitmap);
            } else if (addpixelMode == 1) {
                MakeSoftDiamondPoint2dHistogram(pW, pH, dataToBitmap);
            } else if (addpixelMode == 2) {
                MakeDiamondPoint2dHistogram(pW, pH, dataToBitmap);
            } else if (addpixelMode == 3) {
                MakeSquarePoint2dHistogram(pW, pH, dataToBitmap);
            } else if (addpixelMode == 4) {
                Make21Point2dHistogram(pW, pH, dataToBitmap);
            }
        }

        void MakeVisibleRegionEmpty(int pW, int pH)
        {
            if (m_image == null || m_image.Length < pW * pH) {
                m_image = new uint[pW * pH];
                m_accumulator = new UintColor[pW * pH];
            } else {
                for (var i = 0; i < pW * pH; i++) {
                    m_image[i] = 0;
                }

                for (var i = 0; i < pW * pH; i++) {
                    m_accumulator[i] = default(UintColor);
                }
            }
        }


        void MakeSinglePoint2dHistogram(int pW, int pH, Matrix dataToBitmap)
        {
            var data = Data;
            for (var i = 0; i < data.Length; i++) {
                var displaypoint = dataToBitmap.Transform(data[i].point);
                var x = (int)(displaypoint.X);
                var y = (int)(displaypoint.Y);
                if (x >= 0 && x < pW && y >= 0 && y < pH) {
                    AddPixel(x + pW * y, data[i].label);
                }
            }
        }

        void MakeSoftDiamondPoint2dHistogram(int pW, int pH, Matrix dataToBitmap)
        {
            var data = Data;
            for (var i = 0; i < data.Length; i++) {
                var displaypoint = dataToBitmap.Transform(data[i].point);
                var label = data[i].label;
                var x = (int)(displaypoint.X);
                var y = (int)(displaypoint.Y);
                if (x >= 1 && x < pW - 1 && y >= 1 && y < pH - 1) {
                    AddPixel(x + pW * y, label);
                    AddPixel(x + pW * y, label);
                    AddPixel(x + pW * y, label);
                    AddPixel(x - 1 + pW * y, label);
                    AddPixel(x + 1 + pW * y, label);
                    AddPixel(x + pW * (y - 1), label);
                    AddPixel(x + pW * (y + 1), label);
                }
            }
        }

        void MakeDiamondPoint2dHistogram(int pW, int pH, Matrix dataToBitmap)
        {
            var data = Data;
            for (var i = 0; i < data.Length; i++) {
                var label = data[i].label;
                var displaypoint = dataToBitmap.Transform(data[i].point);
                var x = (int)(displaypoint.X);
                var y = (int)(displaypoint.Y);
                if (x >= 1 && x < pW - 1 && y >= 1 && y < pH - 1) {
                    AddPixel(x + pW * y, label);
                    AddPixel(x - 1 + pW * y, label);
                    AddPixel(x + 1 + pW * y, label);
                    AddPixel(x + pW * (y - 1), label);
                    AddPixel(x + pW * (y + 1), label);
                }
            }
        }

        void MakeSquarePoint2dHistogram(int pW, int pH, Matrix dataToBitmap)
        {
            var data = Data;
            for (var i = 0; i < data.Length; i++) {
                var label = data[i].label;
                var displaypoint = dataToBitmap.Transform(data[i].point);
                var x = (int)(displaypoint.X);
                var y = (int)(displaypoint.Y);
                if (x >= 1 && x < pW - 1 && y >= 1 && y < pH - 1) {
                    AddPixel(x + pW * y, label);
                    AddPixel(x - 1 + pW * y, label);
                    AddPixel(x + 1 + pW * y, label);
                    AddPixel(x + pW * (y - 1), label);
                    AddPixel(x + pW * (y + 1), label);

                    AddPixel(x + pW * y, label);
                    AddPixel(x - 1 + pW * y, label);
                    AddPixel(x + 1 + pW * y, label);
                    AddPixel(x - 1 + pW * (y - 1), label);
                    AddPixel(x + pW * (y - 1), label);
                    AddPixel(x + 1 + pW * (y - 1), label);
                    AddPixel(x - 1 + pW * (y + 1), label);
                    AddPixel(x + pW * (y + 1), label);
                    AddPixel(x + 1 + pW * (y + 1), label);
                }
            }
        }

        void Make21Point2dHistogram(int pW, int pH, Matrix dataToBitmap)
        {
            var data = Data;
            for (var i = 0; i < data.Length; i++) {
                var label = data[i].label;
                var displaypoint = dataToBitmap.Transform(data[i].point);
                var x = (int)(displaypoint.X);
                var y = (int)(displaypoint.Y);
                if (x >= 2 && x < pW - 2 && y >= 2 && y < pH - 2) {
                    var offset = x + pW * y;
                    AddPixel(offset - 1 - 2 * pW, label);
                    AddPixel(offset - 2 * pW, label);
                    AddPixel(offset + 1 - 2 * pW, label);

                    AddPixel(offset - 2 - pW, label);
                    AddPixel(offset - 1 - pW, label);
                    AddPixel(offset - pW, label);
                    AddPixel(offset + 1 - pW, label);
                    AddPixel(offset + 2 - pW, label);

                    AddPixel(offset - 2, label);
                    AddPixel(offset - 1, label);
                    AddPixel(offset, label);
                    AddPixel(offset + 1, label);
                    AddPixel(offset + 2, label);

                    AddPixel(offset - 2 + pW, label);
                    AddPixel(offset - 1 + pW, label);
                    AddPixel(offset + pW, label);
                    AddPixel(offset + 1 + pW, label);
                    AddPixel(offset + 2 + pW, label);

                    AddPixel(offset - 1 + 2 * pW, label);
                    AddPixel(offset + 2 * pW, label);
                    AddPixel(offset + 1 + 2 * pW, label);


                    AddPixel(offset - 1 - pW, label);
                    AddPixel(offset - pW, label);
                    AddPixel(offset + 1 - pW, label);

                    AddPixel(offset - 1, label);
                    AddPixel(offset, label);
                    AddPixel(offset + 1, label);

                    AddPixel(offset - 1 + pW, label);
                    AddPixel(offset + pW, label);
                    AddPixel(offset + 1 + pW, label);
                }
            }
        }

        void AddPixel(int offset, int label)
        {
            m_image[offset]++;
            m_accumulator[offset].Increment(m_MappedColors[label]);
        }

        void ConvertHistogramToColorDensityImage(int pW, int pH, double alpha)
        {
            var numPixels = pW * pH;
            var alphaLookup = PregenerateAlphaLookup(alpha, m_image, numPixels);

            for (var pxI = 0; pxI < numPixels; pxI++) {
                var pointOverlap = m_image[pxI];
                m_image[pxI] = m_accumulator[pxI].RawDiv(pointOverlap) | alphaLookup[pointOverlap];
            }
        }

        void CopyImageRegionToWriteableBitmap(int pW, int pH)
        {
            try {
                m_bmp.WritePixels(
                    new Int32Rect(0, 0, pW, pH),
                    m_image,
                    pW * sizeof(uint),
                    0,
                    0
                );
            } catch (ArgumentOutOfRangeException ae) {
                Console.WriteLine(ae);
                Console.WriteLine("pW: " + pW);
                Console.WriteLine("pH: " + pH);
                Console.WriteLine("m_bmp.PixelWidth: " + m_bmp.PixelWidth);
                Console.WriteLine("m_bmp.PixelHeight: " + m_bmp.PixelHeight);
                Console.WriteLine("m_image.Length: " + m_image.Length + "\n\n");
            }
        }

        static uint[] PregenerateAlphaLookup(double alpha, uint[] image, int numPixels)
        {
            var maximalOverlapCount = ValueOfMax(image, 0, numPixels);
            var transparencyPerOverlap = (1.0 - alpha);
            var alphaLookup = new uint[maximalOverlapCount + 1];
            for (var overlap = 0; overlap < alphaLookup.Length; overlap++) {
                var overlappingAlpha = (1.0 - Math.Pow(transparencyPerOverlap, overlap / 2.0));
                alphaLookup[overlap] = (uint)(overlappingAlpha * 255.5) << 24;
            }

            return alphaLookup;
        }

        static uint ValueOfMax(uint[] m_image, int start, int end)
        {
            uint maxCount = 0;
            for (var i = start; i < end; i++) {
                if (m_image[i] > maxCount) {
                    maxCount = m_image[i];
                }
            }

            return maxCount;
        }

        #endregion

        protected override void OnDataChanged(LabelledPoint[] oldData)
        {
            InvalidateDataBounds();

            m_OuterDataBounds = VizPixelScatterHelpers.ComputeOuterBounds(Data.Select(lp => lp.point).ToArray());
            TriggerChange(GraphChange.Projection); //because we need to relayout the points in the plot
        }

        protected override Rect ComputeBounds() => m_OuterDataBounds.IsEmpty ? m_OuterDataBounds : VizPixelScatterHelpers.ComputeInnerBoundsByRatio(Data.Select(lp => lp.point).ToArray(), CoverageRatio, CoverageRatio, CoverageGradient, m_OuterDataBounds);

        public override void OnRenderOptionsChanged() => TriggerChange(GraphChange.Projection); // because we need to relayout the points in the plot.

        public override bool SupportsColor => false;
    }
}
