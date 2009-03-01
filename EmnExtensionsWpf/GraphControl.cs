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
	/// <summary>
	/// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
	///
	/// Step 1a) Using this custom control in a XAML file that exists in the current project.
	/// Add this XmlNamespace attribute to the root element of the markup file where it is 
	/// to be used:
	///
	///     xmlns:MyNamespace="clr-namespace:EmnExtensions.Wpf"
	///
	///
	/// Step 1b) Using this custom control in a XAML file that exists in a different project.
	/// Add this XmlNamespace attribute to the root element of the markup file where it is 
	/// to be used:
	///
	///     xmlns:MyNamespace="clr-namespace:EmnExtensions.Wpf;assembly=EmnExtensions.Wpf"
	///
	/// You will also need to add a project reference from the project where the XAML file lives
	/// to this project and Rebuild to avoid compilation errors:
	///
	///     Right click on the target project in the Solution Explorer and
	///     "Add Reference"->"Projects"->[Browse to and select this project]
	///
	///
	/// Step 2)
	/// Go ahead and use your control in the XAML file.
	///
	///     <MyNamespace:GraphControl/>
	///
	/// </summary>
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

		protected abstract void SetTransform(MatrixTransform displayTransform);

		void UpdateBounds() {
			if (!(graphBoundsPrivate.Height.IsFinite() && graphBoundsPrivate.Width.IsFinite())) return; //no nonsense
			Size curSize = new Size(ActualWidth, ActualHeight);
			if (curSize.IsEmpty || curSize.Width*curSize.Height==0) return;

			SetTransform(new MatrixTransform(GraphUtils.TransformShape(graphBoundsPrivate,new Rect(curSize),true)));
	
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
				graphLinePen.Freeze();
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

		public GraphControl() {	GraphPen = GraphRandomPen.MakeDefaultPen(true);	}

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
