using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public class VizPixelScatterGeom : PlotVizTransform<Point[], StreamGeometry>, IVizPixelScatter
	{
		VizGeometry impl = new VizGeometry();

		Point[] oldData;
		StreamGeometry transformedData;
		protected override StreamGeometry TransformedData(Point[] inputData) { return transformedData; }
		public override void DataChanged(Point[] newData)
		{
			oldData = newData;
			transformedData = GraphUtils.PointCloud(newData);
			RecomputeBounds(newData);
			impl.DataChanged(transformedData);
		}



		double m_Coverage = 1.0;
		public double CoverageRatio { get { return m_Coverage; } set { m_Coverage = value; RecomputeBounds(oldData); } }

		private void RecomputeBounds(Point[] oldData)
		{
			Rect innerBounds, outerBounds;
			VizPixelScatterHelpers.RecomputeBounds(oldData, CoverageRatio, out outerBounds, out innerBounds);
			if (innerBounds != m_InnerBounds)
			{
				m_InnerBounds = innerBounds;
				Owner.TriggerChange(GraphChange.Projection);
			}
		}
		Rect m_InnerBounds;
		public override Rect DataBounds(Point[] data) { return m_InnerBounds; }

		public Color PointColor
		{
			get
			{
				return ((SolidColorBrush)impl.Pen.Brush).Color;
			}
			set
			{
				Pen penCopy = impl.Pen.CloneCurrentValue();
				penCopy.Brush = new SolidColorBrush(value);
				penCopy.Freeze();
				impl.Pen = penCopy;
			}
		}
		protected override IVizEngine<StreamGeometry> Implementation { get { return impl; } }
	}
}
