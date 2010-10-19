﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public class VizPixelScatterSmart : VizTransformed<Point[], Point[]>, IVizPixelScatter {
		const int MaxPointsInStreamGeometry = 15000;

		protected override Point[] TransformedData(Point[] inputData) { return inputData; }
		IVizPixelScatter engine = new VizPixelScatterGeom();
		protected override IVizEngine<Point[]> Implementation { get { return engine; } }

		public override void DataChanged(Point[] newData) {
			bool useBmpPlot = newData != null && newData.Length > MaxPointsInStreamGeometry;
			bool reconstructEngine = engine is VizPixelScatterBitmap != useBmpPlot;

			if (reconstructEngine) {
				IVizPixelScatter newImplementation = useBmpPlot ? (IVizPixelScatter)new VizPixelScatterBitmap() : new VizPixelScatterGeom();
				newImplementation.Plot = Plot;
				newImplementation.CoverageRatio = CoverageRatio;
				engine = newImplementation;
				Plot.GraphChanged(GraphChange.Projection);
				Plot.GraphChanged(GraphChange.Drawing);
			}
			Implementation.DataChanged(newData);
		}

		public double CoverageRatio { get { return engine.CoverageRatio; } set { engine.CoverageRatio = value; } }
		public double CoverageGradient { get { return engine.CoverageGradient; } set { engine.CoverageGradient = value; } }
	}
}
