using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace EmnExtensions.Wpf
{
	public class GraphGeometryControl : GraphControl
	{
		protected override void RenderGraph(DrawingContext drawingContext) {
			drawingContext.DrawGeometry(null, GraphPen, graphGeom);
		}

		Geometry graphGeom;
		public Geometry GraphGeometry { get { return graphGeom; } set { graphGeom = value; RecomputeBounds(); InvalidateVisual(); } }
		public void RecomputeBounds() {
			graphGeom.Transform = Transform.Identity;
			GraphBounds = graphGeom.Bounds;
		}
		protected override void SetTransform(MatrixTransform displayTransform) {
			graphGeom.Transform = displayTransform;
		}

		public override bool IsEmpty { get { return graphGeom == null; } }
	}
}
