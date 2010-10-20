using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public interface IVizLineSegments : IVizEngine<Point[]> {
		double CoverageRatioY { get; set; }
		double CoverageRatioX { get; set; }
		double CoverageRatioGrad { get; set; }
	}

	public class VizLineSegments : VizTransformed<Point[], StreamGeometry>, IVizLineSegments {
		IVizEngine<StreamGeometry> impl = new VizGeometry { AutosizeBounds = false };
		StreamGeometry geomCache;
		Point[] currentPoints;
		protected override StreamGeometry TransformedData(Point[] inputData) { return geomCache; }

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

		private void RecomputeBounds(Point[] newData) {
			Rect innerBounds, outerBounds;
			VizPixelScatterHelpers.RecomputeBounds(newData, CoverageRatioX, CoverageRatioY, CoverageRatioGrad, out outerBounds, out innerBounds);
			if (innerBounds != m_InnerBounds) {
				m_InnerBounds = innerBounds;
				if (Plot != null) Plot.GraphChanged(GraphChange.Projection);
			}
		}
		Rect m_InnerBounds;
		public override Rect DataBounds { get { return m_InnerBounds; } }
	}
}
