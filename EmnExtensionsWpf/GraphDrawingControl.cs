using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace EmnExtensions.Wpf
{
	public class GraphDrawingControl : GraphControl
	{
		Drawing graphDrawing;
		public Drawing GraphDrawing { get { return graphDrawing; } set { graphDrawing = value; RecomputeBounds(); InvalidateVisual(); } }

		public void RecomputeBounds() { GraphBounds = graphDrawing.Bounds; }

		MatrixTransform drawingTransform;
		protected override void SetTransform(MatrixTransform displayTransform) {
			drawingTransform = displayTransform;
		}

		public override bool IsEmpty { get { return graphDrawing == null; } }

		protected override void RenderGraph(DrawingContext drawingContext) {
			drawingContext.PushTransform(drawingTransform);
			drawingContext.DrawDrawing(graphDrawing);
			drawingContext.Pop();
		}
	}
}
