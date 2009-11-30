using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public class VizDrawing : PlotVizBase<Drawing>
	{
		MatrixTransform m_trans = new MatrixTransform();
		RectangleGeometry m_clip = new RectangleGeometry();
		public override void DrawGraph(DrawingContext context) {
			context.PushClip(m_clip);
			context.PushTransform(m_trans);
			context.DrawDrawing(Owner.Data);
			context.Pop();
			context.Pop();
		}

		public override void SetTransform(Matrix boundsToDisplay, Rect displayClip)
		{
			m_trans.Matrix = boundsToDisplay;
			m_clip.Rect = displayClip;
		}

		public override void DataChanged(Drawing newData) {
			DataBounds = newData.Bounds;
			OnChange(GraphChange.Drawing);
		}
	}
}
