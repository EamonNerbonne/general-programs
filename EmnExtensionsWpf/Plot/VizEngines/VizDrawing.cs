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
		
		public override void DrawGraph(DrawingContext context)
		{
			context.PushClip(m_clip);
			context.PushTransform(m_trans);
			context.DrawDrawing(Data);
			context.Pop();
			context.Pop();
		}

		public override void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY)
		{
			m_trans.Matrix = boundsToDisplay;
			m_clip.Rect = displayClip;
		}

		protected override void OnDataChanged(Drawing oldData)
		{
			SetDataBounds(Data.Bounds);
			TriggerChange(GraphChange.Drawing);
		}

		public override void OnRenderOptionsChanged() { } //doesn't use primary color at all.
		public override bool SupportsColor { get { return false; } }
	}
}
