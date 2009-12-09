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
		public override void DrawGraph(Drawing data, DrawingContext context)
		{
			context.PushClip(m_clip);
			context.PushTransform(m_trans);
			context.DrawDrawing(data);
			context.Pop();
			context.Pop();
		}

		public override void SetTransform(Drawing data, Matrix boundsToDisplay, Rect displayClip)
		{
			m_trans.Matrix = boundsToDisplay;
			m_clip.Rect = displayClip;
		}

		public override void DataChanged(Drawing data)
		{
			SetDataBounds(data.Bounds);
			TriggerChange(GraphChange.Drawing);
		}


		public override void RenderOptionsChanged() { } //doesn't use primary color at all.
		public override bool SupportsThickness { get { return false; } }
		public override bool SupportsColor { get { return false; } }
	}
}
