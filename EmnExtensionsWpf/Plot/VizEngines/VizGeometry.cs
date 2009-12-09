using System;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public class VizGeometry : PlotVizBase<Geometry>
	{
		Geometry m_Geometry;
		MatrixTransform m_ProjectionTransform = new MatrixTransform();
		bool m_AutosizeBounds = true;
		Brush m_Fill = Brushes.Black;
		Pen m_Pen = defaultPen;
		Matrix m_geomToAxis = Matrix.Identity;

		static Pen defaultPen = (Pen)new Pen { Brush = Brushes.Black, EndLineCap = PenLineCap.Square, StartLineCap = PenLineCap.Square, Thickness = 1.5 }.GetAsFrozen();

		public Brush Fill
		{
			get { return m_Fill; }
			set
			{
				if (m_Fill != value)
				{
					m_Fill = value;
					TriggerChange(GraphChange.Drawing);
				}
			}
		}

		public Pen Pen
		{
			get { return m_Pen; }
			set
			{
				if (m_Pen != value)
				{
					if (m_Pen != null && !m_Pen.IsFrozen)
						m_Pen.Changed -= m_Pen_Changed;
					m_Pen = value;
					if (m_Pen != null && !m_Pen.IsFrozen)
						m_Pen.Changed += m_Pen_Changed;
					TriggerChange(GraphChange.Drawing);
					RecomputeBoundsIfAuto();
				}
			}
		}

		/// <summary>
		/// Defaults to true.
		/// </summary>
		public bool AutosizeBounds { get { return m_AutosizeBounds; } set { m_AutosizeBounds = value; RecomputeBoundsIfAuto(); } }

		public override void DataChanged(Geometry newData)
		{
			if (newData == m_Geometry)
				return;
			if (m_Geometry != null && !m_Geometry.IsFrozen)
				m_Geometry.Changed -= m_Geometry_Changed;
			m_geomToAxis = newData.Transform.Value;
			m_Geometry = newData;
			if (m_Geometry != null && !m_Geometry.IsFrozen)
				m_Geometry.Changed += m_Geometry_Changed;
			RecreatePen();
			RecomputeBoundsIfAuto();
			TriggerChange(GraphChange.Drawing);
		}

		void m_Pen_Changed(object sender, EventArgs e) { RecomputeBoundsIfAuto(); }
		void m_Geometry_Changed(object sender, EventArgs e) { if (!changingGeometry) RecomputeBoundsIfAuto(); }
		bool changingGeometry = false;

		void RecomputeBoundsIfAuto()
		{
			if (m_AutosizeBounds)
			{
				changingGeometry = true;

				m_Geometry.Transform = new MatrixTransform(m_geomToAxis);
				SetDataBounds(m_Geometry.Bounds);//this will trigger OnChanged if neeeded.
				m_Geometry.Transform = m_ProjectionTransform;
				SetMargin(new Thickness(Pen.Thickness / 2.0));//this will trigger OnChanged if neeeded.
				changingGeometry = false;
			}
		}

		public override void SetTransform(Geometry data, Matrix axisToDisplay, Rect displayClip)
		{
			changingGeometry = true;
			m_ProjectionTransform.Matrix = m_geomToAxis * axisToDisplay;
			changingGeometry = false;
		}

		public override void DrawGraph(Geometry data, DrawingContext context)
		{
			context.DrawGeometry(m_Fill, m_Pen, m_Geometry);
		}

		public override void RenderOptionsChanged() { RecreatePen(); }

		private void RecreatePen()
		{
			Color currentColor = ((SolidColorBrush)m_Pen.Brush).Color;
			double currentThickness = m_Pen.Thickness;
			Color newColor = Owner.RenderColor ?? currentColor;
			double newThickness = Owner.RenderThickness ?? currentThickness;
			if (newThickness != currentThickness || newColor != currentColor)
			{
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
