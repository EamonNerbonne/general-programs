using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public class GraphableGeometry : GraphableData
	{
		Geometry m_Geometry ;
		GeometryGroup m_OuterGeom = new GeometryGroup();
		MatrixTransform m_OuterGeomTransform = new MatrixTransform();
		bool m_AutosizeBounds = true;
		Brush m_Fill = Brushes.Black;
		Pen m_Pen = defaultPen;

		//public GraphableGeometry

		static Pen defaultPen = (Pen)new Pen { Brush = Brushes.Black, EndLineCap = PenLineCap.Square, StartLineCap = PenLineCap.Square }.GetAsFrozen();

		public Brush Fill { get { return m_Fill; } set { if (m_Fill != value) { m_Fill = value; OnChange(GraphChangeEffects.RedrawGraph); } } }
		public Pen Pen {
			get { return m_Pen; }
			set {
				if (m_Pen != value) {
					if (m_Pen != null && !m_Pen.IsFrozen)
						m_Pen.Changed -= m_Pen_Changed;
					m_Pen = value;
					if (m_Pen != null && !m_Pen.IsFrozen)
						m_Pen.Changed += m_Pen_Changed;
					RecomputeBoundsIfAuto();
					OnChange(GraphChangeEffects.RedrawGraph);
				}
			}
		}


		/// <summary>
		/// Defaults to true.
		/// </summary>
		public bool AutosizeBounds { get { return m_AutosizeBounds; } set { m_AutosizeBounds = value; RecomputeBoundsIfAuto(); } }

		public Geometry Geometry {
			get { return m_Geometry; }
			set {
				if (m_Geometry != null && !m_Geometry.IsFrozen)
					m_Geometry.Changed -= m_Geometry_Changed;
				m_Geometry = value;
				m_OuterGeom = null;
				if (m_Geometry != null && !m_Geometry.IsFrozen)
					m_Geometry.Changed += m_Geometry_Changed;
				RecomputeBoundsIfAuto();
				OnChange(GraphChangeEffects.DrawingInternals);
			}
		}

		void m_Pen_Changed(object sender, EventArgs e) { RecomputeBoundsIfAuto(); }
		void m_Geometry_Changed(object sender, EventArgs e) { RecomputeBoundsIfAuto(); }

		void RecomputeBoundsIfAuto() {
			if (m_AutosizeBounds) {
				DataBounds = m_Geometry.Bounds;//this will trigger OnChanged if neeeded.
				Margin = new Thickness(Pen.Thickness);//this will trigger OnChanged if neeeded.
			}
		}

		public override void SetTransform(Matrix matrix) { this.m_OuterGeomTransform.Matrix = matrix;OnChange(GraphChangeEffects.DrawingInternals); }

		public override void DrawGraph(DrawingContext context) {
			if (m_OuterGeom == null) { //we lazily construct this outer geometry since it might never be needed.
				m_OuterGeom = new GeometryGroup();
				m_OuterGeom.Children.Add(m_Geometry);
				//m_OuterGeom.FillRule = FillRule.Nonzero;
				m_OuterGeom.Transform = m_OuterGeomTransform;
			}
			context.DrawGeometry(m_Fill, m_Pen, m_OuterGeom);
		}

	}
}
