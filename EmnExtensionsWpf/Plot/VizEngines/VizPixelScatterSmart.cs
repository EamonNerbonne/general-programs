using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public class VizPixelScatterSmart : PlotVizTransform<Point[], Point[]>, IVizPixelScatter {
		const int MaxPointsInStreamGeometry = 15000;

		protected override Point[] TransformedData(Point[] inputData) { return inputData; }
		IVizPixelScatter engine = new VizPixelScatterGeom();
		protected override IVizEngine<Point[]> Implementation { get { return engine; } }

		public override void DataChanged(Point[] newData) {
			bool useBmpPlot = newData != null && newData.Length > MaxPointsInStreamGeometry;
			bool reconstructEngine = engine is VizPixelScatterBitmap != useBmpPlot;

			if (reconstructEngine) {
				IVizPixelScatter newImplementation = useBmpPlot ? (IVizPixelScatter)new VizPixelScatterBitmap() : new VizPixelScatterGeom();
				newImplementation.Owner = Owner;
				newImplementation.CoverageRatio = CoverageRatio;
				engine = newImplementation;
				Owner.TriggerChange(GraphChange.Projection);
				Owner.TriggerChange(GraphChange.Drawing);
			}
			engine.DataChanged(newData);
		}

		public double CoverageRatio { get { return engine.CoverageRatio; } set { engine.CoverageRatio = value; } }
	}
}
