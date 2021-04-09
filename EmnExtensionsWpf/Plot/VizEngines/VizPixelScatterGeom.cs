//#define PERMIT_SQUARE_CAPS
//using square line caps breaks ghostscript's gxps conversion; disabled.

using System;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.VizEngines
{
    public class VizPixelScatterGeom : VizTransformed<Point[], StreamGeometry>, IVizPixelScatter
    {
        readonly VizGeometry impl;
        Point[] currentData;
        Rect? computedInnerBounds;
        StreamGeometry transformedData;
        public override void ChangeData(Point[] newData)
        {
            currentData = newData;
            transformedData = GraphUtils.PointCloud(newData);
            impl.ChangeData(transformedData);

            InvalidateBounds();

        }
        public VizPixelScatterGeom(IPlotMetaData metadata)
        {
            impl = new VizGeometry(metadata) { AutosizeBounds = false };
        }

        void InvalidateBounds()
        {
            computedInnerBounds = null;
            if (!MetaData.OverrideBounds.HasValue) {
                MetaData.GraphChanged(GraphChange.Projection);
            }
        }
        public int? OverridePointCountEstimate { get; set; }

        double lastAreaScale = 1.0;
        void SetPenSize()
        {
            var pointCount = OverridePointCountEstimate ?? (currentData == null ? 0 : currentData.Length);
            var thickness = MetaData.RenderThickness ?? VizPixelScatterHelpers.PointCountToThickness(pointCount);
            thickness *= lastAreaScale;
            if (Math.Abs(impl.Pen.Thickness - thickness) < 0.1) {
                return;
            }

#if PERMIT_SQUARE_CAPS
            var linecap = PenLineCap.Round;
            if (thickness <= 3) { 
                linecap = PenLineCap.Square;
                thickness *= VizPixelScatterHelpers.SquareSidePerThickness;
            }
#else
            const PenLineCap linecap = PenLineCap.Round;
#endif
            var penCopy = impl.Pen.CloneCurrentValue();
            penCopy.EndLineCap = linecap;
            penCopy.StartLineCap = linecap;
            penCopy.Thickness = thickness;
            penCopy.Freeze();
            impl.Pen = penCopy;
        }

        public override void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY)
        {
            lastAreaScale = Math.Sqrt(displayClip.Width * displayClip.Height / 90000.0);
            SetPenSize();
            base.SetTransform(boundsToDisplay, displayClip, forDpiX, forDpiY);
        }

        double m_Coverage = 0.9999;
        public double CoverageRatio { get => m_Coverage; set { m_Coverage = value; InvalidateBounds(); } }

        double m_CoverageGradient = 5.0;
        public double CoverageGradient { get => m_CoverageGradient; set { m_CoverageGradient = value; InvalidateBounds(); } }

        Rect RecomputeBounds()
        {
            Rect innerBounds, outerBounds;
            VizPixelScatterHelpers.RecomputeBounds(currentData, CoverageRatio, CoverageRatio, CoverageGradient, out outerBounds, out innerBounds);
            return innerBounds;
        }
        public override Rect DataBounds => computedInnerBounds ?? (computedInnerBounds = RecomputeBounds()).Value;

        protected override IVizEngine<StreamGeometry> Implementation => impl;

    }
}
