using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public abstract class GraphableData : FrameworkElement
	{
		public event EventHandler Changed;
		string m_xUnitLabel,m_yUnitLabel,m_DataLabel;

		public string XUnitLabel { get { return m_xUnitLabel; } set { if (m_xUnitLabel != value) { m_xUnitLabel = value; onchange(); } } }
		public string YUnitLabel { get { return m_yUnitLabel; } set { if (m_yUnitLabel != value) { m_yUnitLabel = value; onchange(); } } }
		public string DataLabel { get { return m_DataLabel; } set { if (m_DataLabel != value) { m_DataLabel = value; onchange(); } } }

		public Matrix ComputeAxisToDisplayMatrix() {
			Rect displayRect = new Rect(0, 0, ActualWidth, ActualHeight);
			Rect axisRect = GraphAxisBounds;
			if (! axisRect.IsFiniteNonEmpty() || !displayRect.IsFiniteNonEmpty() ) return Matrix.Identity; //no nonsense
			
			return GraphUtils.TransformShape(axisRect, displayRect, true);
			
		}

		private void onchange();
	}
}
