using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.VizEngines {
    public interface IVizLineSegments : IVizEngine<Point[]> {
        double CoverageRatioY { get; set; }
        double CoverageRatioX { get; set; }
        double CoverageRatioGrad { get; set; }
    }

    public class VizLineSegments : VizTransformed<Point[], StreamGeometry>, IVizLineSegments {
        readonly VizGeometry impl;
        
        StreamGeometry geomCache;
        Point[] currentPoints;

        public VizLineSegments(IPlotMetaData owner) { impl = new VizGeometry(owner) { AutosizeBounds = false }; }

        protected override IVizEngine<StreamGeometry> Implementation { get { return impl; } }

        double m_CoverageRatioY = 0.9999;
        public double CoverageRatioY { get { return m_CoverageRatioY; } set { if (value != m_CoverageRatioY) { m_CoverageRatioY = value; RecomputeBounds(currentPoints); } } }
        double m_CoverageRatioX = 1.0;
        public double CoverageRatioX { get { return m_CoverageRatioX; } set { if (value != m_CoverageRatioX) { m_CoverageRatioX = value; RecomputeBounds(currentPoints); } } }

        double m_CoverageRatioGrad = 2.0;
        public double CoverageRatioGrad { get { return m_CoverageRatioGrad; } set { if (value != m_CoverageRatioGrad) { m_CoverageRatioGrad = value; RecomputeBounds(currentPoints); } } }

        public override void ChangeData(Point[] newData) {
            currentPoints = newData;
            geomCache = GraphUtils.LineScaled(newData);
            RecomputeBounds(currentPoints);
            Implementation.ChangeData(geomCache);
        }

        public DashStyle DashStyle { get { return impl.Pen.DashStyle; } set { impl.Pen = impl.Pen.AsFrozen(pen => pen.DashStyle = value); } }

        void RecomputeBounds(Point[] newData) {
            Rect innerBounds, outerBounds;
            VizPixelScatterHelpers.RecomputeBounds(newData, CoverageRatioX, CoverageRatioY, CoverageRatioGrad, out outerBounds, out innerBounds);
            if (innerBounds != m_InnerBounds) {
                m_InnerBounds = innerBounds;
                MetaData.GraphChanged(GraphChange.Projection);
            }
        }
        Rect m_InnerBounds;
        public override Rect DataBounds { get { return m_InnerBounds; } }
    }
}