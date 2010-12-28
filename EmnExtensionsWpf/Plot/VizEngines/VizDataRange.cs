﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public interface IVizDataRange : IVizEngine<Tuple<Point[], Point[]>> {
		double CoverageRatioY { get; set; }
		double CoverageRatioX { get; set; }
		double CoverageRatioGrad { get; set; }
	}

	public class VizDataRange : VizTransformed<Tuple<Point[], Point[]>, StreamGeometry>, IVizDataRange {
		readonly IVizEngine<StreamGeometry> impl = new VizGeometry { AutosizeBounds = false, IsStroked = false, IsFilled = true };
		StreamGeometry geomCache;
		Tuple<Point[], Point[]> currentPoints;

		protected override IVizEngine<StreamGeometry> Implementation { get { return impl; } }

		double m_CoverageRatioY = 0.9999;
		public double CoverageRatioY { get { return m_CoverageRatioY; } set { if (value != m_CoverageRatioY) { m_CoverageRatioY = value; RecomputeBounds(); } } }
		double m_CoverageRatioX = 1.0;
		public double CoverageRatioX { get { return m_CoverageRatioX; } set { if (value != m_CoverageRatioX) { m_CoverageRatioX = value; RecomputeBounds(); } } }

		double m_CoverageRatioGrad = 2.0;
		public double CoverageRatioGrad { get { return m_CoverageRatioGrad; } set { if (value != m_CoverageRatioGrad) { m_CoverageRatioGrad = value; RecomputeBounds(); } } }

		public override void ChangeData(Tuple<Point[], Point[]> newData) {
			currentPoints = newData;
			geomCache = GraphUtils.RangeScaled(newData.Item1, newData.Item2);
			RecomputeBounds();
			Implementation.ChangeData(geomCache);
		}

		void RecomputeBounds() {
			Rect innerBounds = Rect.Empty, outerBounds;
			if (currentPoints != null)
				VizPixelScatterHelpers.RecomputeBounds(currentPoints.Item1.Concat(currentPoints.Item2).ToArray(), CoverageRatioX, CoverageRatioY, CoverageRatioGrad, out outerBounds, out innerBounds);
			if (innerBounds != m_InnerBounds) {
				m_InnerBounds = innerBounds;
				if (Plot != null) Plot.GraphChanged(GraphChange.Projection);
			}
		}
		Rect m_InnerBounds;
		public override Rect DataBounds { get { return m_InnerBounds; } }
	}
}
