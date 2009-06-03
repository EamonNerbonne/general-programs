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
		DrawingGroup drawing;
		//bool needRedraw = false;
		public DrawingGroup GraphDrawing {
			get {
				return drawing;
			}
			set {
				drawing = value;
				InvalidateVisual();
			}
		}
		protected override void OnRender(DrawingContext drawingContext) {
			drawingContext.DrawDrawing(drawing);
		}
	}

}
