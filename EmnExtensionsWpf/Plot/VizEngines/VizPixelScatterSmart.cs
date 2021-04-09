using System.Windows;

namespace EmnExtensions.Wpf.VizEngines
{
    public class VizPixelScatterSmart : VizTransformed<Point[], Point[]>, IVizPixelScatter
    {
        const int MaxPointsInStreamGeometry = 10000;

        IVizPixelScatter engine;
        public VizPixelScatterSmart(IPlotMetaData metadata) => engine = new VizPixelScatterGeom(metadata);
        protected override IVizEngine<Point[]> Implementation => engine;

        public override void ChangeData(Point[] newData)
        {
            var useBmpPlot = newData != null && newData.Length > MaxPointsInStreamGeometry;
            var reconstructEngine = engine is VizPixelScatterBitmap != useBmpPlot;

            if (reconstructEngine) {
                var newImplementation = useBmpPlot ? (IVizPixelScatter)new VizPixelScatterBitmap(MetaData) : new VizPixelScatterGeom(MetaData);
                newImplementation.CoverageRatio = CoverageRatio;
                newImplementation.CoverageGradient = CoverageGradient;
                newImplementation.OverridePointCountEstimate = OverridePointCountEstimate;
                engine = newImplementation;
                MetaData.GraphChanged(GraphChange.Projection);
                MetaData.GraphChanged(GraphChange.Drawing);
            }

            Implementation.ChangeData(newData);
        }

        public int? OverridePointCountEstimate
        {
            get => engine.OverridePointCountEstimate;
            set => engine.OverridePointCountEstimate = value;
        }


        public double CoverageRatio
        {
            get => engine.CoverageRatio;
            set => engine.CoverageRatio = value;
        }

        public double CoverageGradient
        {
            get => engine.CoverageGradient;
            set => engine.CoverageGradient = value;
        }
    }
}
