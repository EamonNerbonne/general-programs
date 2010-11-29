//#define PERMIT_SQUARE_CAPS
//using square line caps breaks ghostscript's gxps conversion; disabled.

using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public class VizPixelScatterGeom : VizTransformed<Point[], StreamGeometry>, IVizPixelScatter {
		readonly VizGeometry impl = new VizGeometry { AutosizeBounds = false };
		Point[] currentData;
		Rect? computedInnerBounds;
		StreamGeometry transformedData;
		public override void ChangeData(Point[] newData) {
			currentData = newData;
			transformedData = GraphUtils.PointCloud(newData);
			impl.ChangeData(transformedData);

			InvalidateBounds();
			SetPenSize(OverridePointCountEstimate ?? (newData == null ? 0 : newData.Length));
		}

		void InvalidateBounds() {
			computedInnerBounds = null;
			if (Plot != null)
				if (!Plot.MetaData.OverrideBounds.HasValue)
					Plot.GraphChanged(GraphChange.Projection);
		}
		public int? OverridePointCountEstimate { get; set; }

		private void SetPenSize(int pointCount) {
			double thickness = Plot.MetaData.RenderThickness ?? VizPixelScatterHelpers.PointCountToThickness(pointCount);

#if PERMIT_SQUARE_CAPS
			var linecap = PenLineCap.Round;
			if (thickness <= 3) { 
				linecap = PenLineCap.Square;
				thickness *= VizPixelScatterHelpers.SquareSidePerThickness;
			}
#else
			const PenLineCap linecap = PenLineCap.Round;
#endif
			Pen penCopy = impl.Pen.CloneCurrentValue();
			penCopy.EndLineCap = linecap;
			penCopy.StartLineCap = linecap;
			penCopy.Thickness = thickness;
			penCopy.Freeze();
			impl.Pen = penCopy;
		}

		double m_Coverage = 0.9999;
		public double CoverageRatio { get { return m_Coverage; } set { m_Coverage = value; InvalidateBounds(); } }

		double m_CoverageGradient = 5.0;
		public double CoverageGradient { get { return m_CoverageGradient; } set { m_CoverageGradient = value; InvalidateBounds(); } }

		private Rect RecomputeBounds() {
			Rect innerBounds, outerBounds;
			VizPixelScatterHelpers.RecomputeBounds(currentData, CoverageRatio, CoverageRatio, CoverageGradient, out outerBounds, out innerBounds);
			return innerBounds;
		}
		public override Rect DataBounds { get { return computedInnerBounds ?? (computedInnerBounds = RecomputeBounds()).Value; } }

		protected override IVizEngine<StreamGeometry> Implementation { get { return impl; } }
	}
}
