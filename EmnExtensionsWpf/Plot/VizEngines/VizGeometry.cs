using System;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public class VizGeometry : PlotVizBase<Geometry> {
		Geometry m_Geometry;
		MatrixTransform m_ProjectionTransform = new MatrixTransform();
		GeometryGroup combinesGeom = new GeometryGroup();
		bool m_AutosizeBounds = true;
		Brush m_Fill = Brushes.Black;
		Pen m_Pen = defaultPen;
		RectangleGeometry clipRectangle = new RectangleGeometry();
		public VizGeometry() {
			combinesGeom.Transform = m_ProjectionTransform;
			SetMargin(new Thickness(Pen.Thickness / 2.0));//this will trigger OnChanged if neeeded.
		}

		static Pen defaultPen = (Pen)new Pen { Brush = Brushes.Black, EndLineCap = PenLineCap.Square, StartLineCap = PenLineCap.Square, Thickness = 1.5 }.GetAsFrozen();

		public Brush Fill {
			get { return m_Fill; }
			set {
				if (m_Fill != value) {
					m_Fill = value;
					TriggerChange(GraphChange.Drawing);
				}
			}
		}

		public Pen Pen {
			get { return m_Pen; }
			set {
				if (m_Pen != value) {
					if (m_Pen != null && !m_Pen.IsFrozen) m_Pen.Changed -= m_Pen_Changed;
					m_Pen = value;
					if (m_Pen != null && !m_Pen.IsFrozen) m_Pen.Changed += m_Pen_Changed;
					m_Pen_Changed(null, null);
				}
			}
		}

		/// <summary>
		/// Defaults to true.
		/// </summary>
		public bool AutosizeBounds { get { return m_AutosizeBounds; } set { m_AutosizeBounds = value; RecomputeBoundsIfAuto(); } }

		public override void DataChanged(Geometry newData) {
			if (newData == m_Geometry)
				return;
			if (m_Geometry != null && !m_Geometry.IsFrozen)
				m_Geometry.Changed -= m_Geometry_Changed;
			m_Geometry = newData;

			if (m_Geometry == null)
				combinesGeom.Children.Clear();
			else if (combinesGeom.Children.Count > 0 && combinesGeom.Children[0] != m_Geometry)
				combinesGeom.Children[0] = m_Geometry;
			else
				combinesGeom.Children.Add(m_Geometry);


			if (m_Geometry != null && !m_Geometry.IsFrozen)
				m_Geometry.Changed += m_Geometry_Changed;
			RecomputeBoundsIfAuto();
			TriggerChange(GraphChange.Drawing);
		}

		void m_Pen_Changed(object sender, EventArgs e) {
			TriggerChange(GraphChange.Drawing);
			SetMargin(new Thickness(Pen.Thickness / 2.0));//this will trigger OnChanged if neeeded.
		}
		void m_Geometry_Changed(object sender, EventArgs e) { RecomputeBoundsIfAuto(); }

		void RecomputeBoundsIfAuto() {
			if (m_AutosizeBounds) SetDataBounds(m_Geometry.Bounds);//this will trigger OnChanged if neeeded.
		}

		public override void SetTransform(Geometry data, Matrix axisToDisplay, Rect displayClip, double forDpiX, double forDpiY) {
			clipRectangle.Rect = displayClip;
			m_ProjectionTransform.Matrix = axisToDisplay;
		}

		public override void DrawGraph(Geometry data, DrawingContext context) {
			context.PushClip(clipRectangle);
			context.DrawGeometry(m_Fill, m_Pen, combinesGeom);
			context.Pop();
		}

		public override void RenderOptionsChanged() { RecreatePen(); }

		private void RecreatePen() {
			Color currentColor = ((SolidColorBrush)m_Pen.Brush).Color;
			double currentThickness = m_Pen.Thickness;
			Color newColor = Owner.RenderColor ?? currentColor;
			double newThickness = Owner.RenderThickness ?? currentThickness;
			if (newThickness != currentThickness || newColor != currentColor) {
				if (newColor != currentColor)
					TriggerChange(GraphChange.Labels);

				Pen newPen = m_Pen.CloneCurrentValue();
				newPen.Brush = new SolidColorBrush(newColor);
				newPen.Thickness = newThickness;
				newPen.Freeze();
				Pen = newPen;
			}
		}
		public override bool SupportsThickness { get { return true; } }
		public override bool SupportsColor { get { return true; } }
	}
}
