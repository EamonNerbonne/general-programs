using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public abstract class GraphableDrawing : GraphableData
	{
		MatrixTransform m_drawingToDisplay = new MatrixTransform();

		public static Rect ComputeDataBounds(Rect innerDrawingBounds, Rect innerDataBounds, Rect drawingBounds) {
			if (innerDataBounds.IsEmpty || innerDrawingBounds.IsEmpty) {
				return Rect.Empty;
			} else {
				Matrix trans = GraphUtils.TransformShape(innerDrawingBounds, innerDataBounds,false);
				return Rect.Transform(drawingBounds, trans);
			}
		}

		public override sealed void DrawGraph(DrawingContext context) {
			context.PushTransform(m_drawingToDisplay);
			try {
				DrawUntransformedIntoDrawingRect(context);
			} finally {
				context.Pop();
			}
		}


		public override sealed void SetTransform(Matrix dataToDisplay, Rect displayClip) {
			m_drawingToDisplay.Matrix = GraphUtils.TransformShape(DrawingRect, DataBounds, false) * dataToDisplay;
		}
		protected abstract void DrawUntransformedIntoDrawingRect(DrawingContext context);
		protected abstract Rect DrawingRect { get; }
	}
}
