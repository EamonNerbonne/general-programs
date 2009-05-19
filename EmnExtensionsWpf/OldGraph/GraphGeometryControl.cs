using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace EmnExtensions.Wpf.OldGraph
{
	public class GraphGeometryControl : GraphControl
	{
		//MatrixTransform dispTrans;
		protected override void RenderGraph(DrawingContext drawingContext) {
			//drawingContext.PushTransform(dispTrans);
			drawingContext.DrawGeometry(GraphFill, GraphPen, graphGeom );//(Geometry)graphGeom.GetCurrentValueAsFrozen());
			//drawingContext.Pop();
		}

		Geometry graphGeom;
		public Geometry GraphGeometry {
			get { return graphGeom; }
			set {
				graphGeom = value;// (Geometry)value.GetCurrentValueAsFrozen();
				
				RecomputeBounds();
				//InvalidateVisual();
			}
		}

		public Brush GraphFill { get; set; }

		public void RecomputeBounds() {
		//	graphGeom.Transform = Transform.Identity;
		//	GraphBounds = graphGeom.Bounds;
			GraphBounds = graphGeom.Transform.Inverse.TransformBounds(graphGeom.Bounds);
		}
		protected override void SetTransform(Matrix displayTransform) {
			//dispTrans = displayTransform;
			//graphGeom = graphGeom.CloneCurrentValue();
			graphGeom.Transform = new MatrixTransform(displayTransform);
			//graphGeom.Freeze();
		}

		public override bool IsEmpty { get { return graphGeom == null; } }
	}
}
