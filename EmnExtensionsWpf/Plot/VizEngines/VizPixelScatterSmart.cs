﻿using System.Windows;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public class VizPixelScatterSmart : VizTransformed<Point[], Point[]>, IVizPixelScatter {
		const int MaxPointsInStreamGeometry = 15000;

		IVizPixelScatter engine = new VizPixelScatterGeom();
		protected override IVizEngine<Point[]> Implementation { get { return engine; } }

		public override void ChangeData(Point[] newData) {
			bool useBmpPlot = newData != null && newData.Length > MaxPointsInStreamGeometry;
			bool reconstructEngine = engine is VizPixelScatterBitmap != useBmpPlot;

			if (reconstructEngine) {
				IVizPixelScatter newImplementation = useBmpPlot ? (IVizPixelScatter)new VizPixelScatterBitmap() : new VizPixelScatterGeom();
				newImplementation.Plot = Plot;
				newImplementation.CoverageRatio = CoverageRatio;
				newImplementation.CoverageGradient = CoverageGradient;
				newImplementation.OverridePointCountEstimate = OverridePointCountEstimate;
				engine = newImplementation;
				Plot.GraphChanged(GraphChange.Projection);
				Plot.GraphChanged(GraphChange.Drawing);
			}
			Implementation.ChangeData(newData);
		}

		public int? OverridePointCountEstimate { get { return engine.OverridePointCountEstimate; } set { engine.OverridePointCountEstimate = value; } }


		public double CoverageRatio { get { return engine.CoverageRatio; } set { engine.CoverageRatio = value; } }
		public double CoverageGradient { get { return engine.CoverageGradient; } set { engine.CoverageGradient = value; } }
	}
}
