﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public class VizPixelScatterGeom : PlotVizTransform<Point[], StreamGeometry>, IVizPixelScatter {
		VizGeometry impl = new VizGeometry();
		Point[] oldData;
		StreamGeometry transformedData;
		protected override StreamGeometry TransformedData(Point[] inputData) { return transformedData; }
		public override void DataChanged(Point[] newData) {
			oldData = newData;
			transformedData = GraphUtils.PointCloud(newData);
			RecomputeBounds(newData);
			impl.DataChanged(transformedData);
			SetPenSize(OverridePointCountEstimate ?? (newData == null ? 0 : newData.Length));
		}
		public int? OverridePointCountEstimate { get; set; }

		private void SetPenSize(int pointCount) {
			double thickness = VizPixelScatterHelpers.PointCountToThickness(pointCount);

			var linecap = PenLineCap.Round;

			if (thickness <= 3) {
				linecap = PenLineCap.Square;
				thickness *= VizPixelScatterHelpers.SquareSidePerThickness;
			}
			Pen penCopy = impl.Pen.CloneCurrentValue();
			penCopy.EndLineCap = linecap;
			penCopy.StartLineCap = linecap;
			penCopy.Thickness = thickness;
			penCopy.Freeze();
			impl.Pen = penCopy;
		}



		double m_Coverage = 0.9999;
		public double CoverageRatio { get { return m_Coverage; } set { m_Coverage = value; RecomputeBounds(oldData); } }

		private void RecomputeBounds(Point[] newData) {
			Rect innerBounds, outerBounds;
			VizPixelScatterHelpers.RecomputeBounds(newData, CoverageRatio, out outerBounds, out innerBounds);
			if (innerBounds != m_InnerBounds) {
				m_InnerBounds = innerBounds;
				Owner.TriggerChange(GraphChange.Projection);
			}
		}
		Rect m_InnerBounds;
		public override Rect DataBounds(Point[] data) { return m_InnerBounds; }

		protected override IVizEngine<StreamGeometry> Implementation { get { return impl; } }
	}
}
