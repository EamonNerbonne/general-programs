using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	class GraphDisplayControl : FrameworkElement
	{
		DrawingVisual drawing;
		//bool needRedraw = false;
		public DrawingVisual GraphDrawing {
			get {
				return drawing;
			}
			set {
				if (drawing != null) {
					RemoveLogicalChild(drawing);
					RemoveVisualChild(drawing);
				}
				drawing = value;
				if (drawing != null) {
					AddVisualChild(drawing);
					AddLogicalChild(drawing);
				}
				InvalidateVisual();
			}
		}

		protected override int VisualChildrenCount { get { return drawing == null ? 0 : 1; } }
		protected override Visual GetVisualChild(int index) {
			if (index != 0)
				throw new IndexOutOfRangeException();
			return drawing;
		}


	}

}
