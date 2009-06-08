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
		MatrixTransform m_ProjectionTransform = new MatrixTransform();
		bool m_AutosizeBounds = true;
		Brush m_Fill = Brushes.Black;
		Pen m_Pen = defaultPen;
		Matrix m_geomToAxis = Matrix.Identity;

		//public GraphableGeometry

		static Pen defaultPen = (Pen)new Pen { Brush = Brushes.Black, EndLineCap = PenLineCap.Square, StartLineCap = PenLineCap.Square, Thickness=1.5 }.GetAsFrozen();

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
				m_geomToAxis = value.Transform.Value;
				m_Geometry = value;
				if (m_Geometry != null && !m_Geometry.IsFrozen)
					m_Geometry.Changed += m_Geometry_Changed;
				RecomputeBoundsIfAuto();
				OnChange(GraphChangeEffects.RedrawGraph);
			}
		}

		void m_Pen_Changed(object sender, EventArgs e) { RecomputeBoundsIfAuto(); }
		void m_Geometry_Changed(object sender, EventArgs e) {if(!changingGeometry) RecomputeBoundsIfAuto(); }
		bool changingGeometry = false;

		void RecomputeBoundsIfAuto() {
			if (m_AutosizeBounds) {
				changingGeometry = true;
				
				m_Geometry.Transform = new MatrixTransform(m_geomToAxis);
				DataBounds = m_Geometry.Bounds;//this will trigger OnChanged if neeeded.
				m_Geometry.Transform = m_ProjectionTransform;
				changingGeometry = false;
				Margin = new Thickness(Pen.Thickness/2.0);//this will trigger OnChanged if neeeded.
			}
		}

		public override void SetTransform(Matrix axisToDisplay, Rect displayClip) { 
			changingGeometry = true; 
			m_ProjectionTransform.Matrix = m_geomToAxis * axisToDisplay;
			changingGeometry = false;
			OnChange(GraphChangeEffects.DrawingInternals); 
		}

		public override void DrawGraph(DrawingContext context) {
			context.DrawGeometry(m_Fill, m_Pen, m_Geometry);
		}

	}
}
