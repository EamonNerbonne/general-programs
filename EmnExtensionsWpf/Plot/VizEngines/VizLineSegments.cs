using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	class VizLineSegments : PlotVizTransform<Point[], StreamGeometry>
	{
		IVizEngine<StreamGeometry> impl = new VizGeometry();
		StreamGeometry geomCache;
		protected override StreamGeometry TransformedData(Point[] inputData) { return geomCache; }

		protected override IVizEngine<StreamGeometry> Implementation { get { return impl; } }

		public override void DataChanged(Point[] newData)
		{
			geomCache = GraphUtils.LineUnscaled (newData);
			impl.DataChanged(geomCache);
		}
	}
}
