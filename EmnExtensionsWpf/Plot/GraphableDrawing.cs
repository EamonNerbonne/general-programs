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
		Rect m_relevant=Rect.Empty, m_relevantData=Rect.Empty;
		Thickness m_drawingMargins = new Thickness();
		MatrixTransform m_drawingToDisplay = new MatrixTransform();
		public Rect DrawingRect { get { return m_relevant; } set { if (value != m_relevant) { m_relevant = value; RecomuteBounds(); } } }
		public Thickness IrrelevantDrawingMargins { get { return m_drawingMargins; } set { if (value != m_drawingMargins) { m_drawingMargins = value; RecomuteBounds(); } } }
		public Rect RelevantDataBounds { get { return m_relevantData; } set { if (value != m_relevantData) { m_relevantData = value; RecomuteBounds(); } } }

		private void RecomuteBounds() {
			if (RelevantDataBounds.IsEmpty || DrawingRect.IsEmpty) {
				DataBounds = Rect.Empty;
			} else {
				Rect relevantDrawingRect = DrawingRect;
				relevantDrawingRect.X += IrrelevantDrawingMargins.Left;
				relevantDrawingRect.Width -= IrrelevantDrawingMargins.Left + IrrelevantDrawingMargins.Right;
				relevantDrawingRect.Y += IrrelevantDrawingMargins.Top;
				relevantDrawingRect.Height -= IrrelevantDrawingMargins.Top + IrrelevantDrawingMargins.Bottom;

				Matrix trans = ComputeBoundsTransform(relevantDrawingRect, RelevantDataBounds);
				DataBounds = Rect.Transform(DrawingRect, trans);
			}
		}

		static Matrix ComputeBoundsTransform(Rect src, Rect dst) { return GraphUtils.TransformShape(src, dst, false); }

		Matrix ComputeDrawingToDataTransform() { return ComputeBoundsTransform(DrawingRect, DataBounds); }
		public override void DrawGraph(DrawingContext context) {
			context.PushTransform(m_drawingToDisplay);
			try {
				DrawUntransformedIntoDrawingRect(context);
			} finally {
				context.Pop();
			}
		}

		protected abstract void DrawUntransformedIntoDrawingRect(DrawingContext context);

		public override void SetTransform(Matrix dataToDisplay, Size estimatedDisplaySize) {
			m_drawingToDisplay.Matrix = ComputeDrawingToDataTransform() * dataToDisplay;
		}
	}
}
