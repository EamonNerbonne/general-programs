using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EmnExtensions.Wpf
{
	public abstract class GraphControl : FrameworkElement
	{
		protected override Size MeasureOverride(Size constraint) {
			return new Size(
				constraint.Width.IsFinite() ? constraint.Width : 150,
				constraint.Height.IsFinite() ? constraint.Height : 150
				);
		}
		public event Action<GraphControl, Rect> GraphBoundsUpdated;

		Rect oldBounds = Rect.Empty;
		Rect graphBoundsPrivate = Rect.Empty;
		public Rect GraphBounds { //TODO dependency property?
			get {
				return graphBoundsPrivate;
			}
			set {
				graphBoundsPrivate = value;
				UpdateBounds();
			}
		}

		public void EnsurePointInBounds(Point p) {
			graphBoundsPrivate.Union(p);
			UpdateBounds();
		}

		Size lastDispSize = Size.Empty;

		protected abstract void SetTransform(Matrix displayTransform);

		void UpdateBounds() {
			if (!(graphBoundsPrivate.Height.IsFinite() && graphBoundsPrivate.Width.IsFinite())) return; //no nonsense
			Size curSize = new Size(ActualWidth, ActualHeight);
			if (curSize.IsEmpty || curSize.Width*curSize.Height==0) return;

			SetTransform(GraphUtils.TransformShape(graphBoundsPrivate,new Rect(curSize),true));
	
			if (oldBounds == graphBoundsPrivate && curSize == lastDispSize) return; //no visual invalidation if no change
			lastDispSize = curSize;
			
			InvalidateVisual();
			if (oldBounds == graphBoundsPrivate) return;//no graph bound change event handler if no change to bounds
			oldBounds = graphBoundsPrivate;

			if (GraphBoundsUpdated != null) GraphBoundsUpdated(this, graphBoundsPrivate);
		}

		string xLabel;
		public string XLabel { get { return xLabel; } set { xLabel = value; InvalidateVisual(); } }
		string yLabel;
		public string YLabel { get { return yLabel; } set { yLabel = value; InvalidateVisual(); } }

		Pen graphLinePen;
		public Pen GraphPen {
			set {
				graphLinePen = value;
				if(graphLinePen!=null) graphLinePen.Freeze();
				InvalidateVisual();
			}
			get {
				return graphLinePen;
			}
		}
		public Brush GraphLineColor {
			set {
				Pen newPen = graphLinePen.CloneCurrentValue();
				newPen.Brush = value;
				GraphPen = newPen;
			}
			get {
				return graphLinePen.Brush;
			}
		}
		public double PenThickness {
			get {
				return graphLinePen.Thickness;
			}
			set {
				Pen newPen = graphLinePen.CloneCurrentValue();
				newPen.Thickness = value;
				GraphPen = newPen;
			}

		}

		public GraphControl() {	GraphPen = GraphRandomPen.MakeDefaultPen(true);IsHitTestVisible = false;}	

		public abstract bool IsEmpty { get; }

		protected override void OnRender(DrawingContext drawingContext) {
			if (IsEmpty) return;
			UpdateBounds();
			drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
			RenderGraph(drawingContext);
		}
		protected abstract void RenderGraph(DrawingContext drawingContext) ;
	}
}
