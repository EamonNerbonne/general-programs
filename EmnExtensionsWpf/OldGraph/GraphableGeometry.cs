using System;
using System.Windows;
using System.Windows.Media;
using EmnExtensions.Wpf.Plot;

namespace EmnExtensions.Wpf.OldGraph
{
    public class GraphableGeometry : GraphableData
    {
        Geometry m_Geometry;
        readonly MatrixTransform m_ProjectionTransform = new();
        bool m_AutosizeBounds = true;
        Brush m_Fill = Brushes.Black;
        Pen m_Pen = defaultPen;
        Matrix m_geomToAxis = Matrix.Identity;

        //public GraphableGeometry
        static readonly Pen defaultPen = (Pen)new Pen { Brush = Brushes.Black, EndLineCap = PenLineCap.Square, StartLineCap = PenLineCap.Square, Thickness = 1.5 }.GetAsFrozen();

        public Brush Fill
        {
            get => m_Fill;
            set {
                if (m_Fill != value) {
                    m_Fill = value;
                    OnChange(GraphChange.Drawing);
                }
            }
        }

        public Pen Pen
        {
            get => m_Pen;
            set {
                if (m_Pen != value) {
                    if (m_Pen != null && !m_Pen.IsFrozen) {
                        m_Pen.Changed -= m_Pen_Changed;
                    }

                    m_Pen = value;
                    if (m_Pen != null && !m_Pen.IsFrozen) {
                        m_Pen.Changed += m_Pen_Changed;
                    }

                    OnChange(GraphChange.Drawing);
                    RecomputeBoundsIfAuto();
                }
            }
        }

        /// <summary>
        /// Defaults to true.
        /// </summary>
        public bool AutosizeBounds
        {
            get => m_AutosizeBounds;
            set {
                m_AutosizeBounds = value;
                RecomputeBoundsIfAuto();
            }
        }

        public Geometry Geometry
        {
            get => m_Geometry;
            set {
                if (m_Geometry != null && !m_Geometry.IsFrozen) {
                    m_Geometry.Changed -= m_Geometry_Changed;
                }

                m_geomToAxis = value.Transform.Value;
                m_Geometry = value;
                if (m_Geometry != null && !m_Geometry.IsFrozen) {
                    m_Geometry.Changed += m_Geometry_Changed;
                }

                RecomputeBoundsIfAuto();
                OnChange(GraphChange.Drawing);
            }
        }

        void m_Pen_Changed(object sender, EventArgs e)
            => RecomputeBoundsIfAuto();

        void m_Geometry_Changed(object sender, EventArgs e)
        {
            if (!changingGeometry) {
                RecomputeBoundsIfAuto();
            }
        }

        bool changingGeometry;

        void RecomputeBoundsIfAuto()
        {
            if (m_AutosizeBounds) {
                changingGeometry = true;

                m_Geometry.Transform = new MatrixTransform(m_geomToAxis);
                DataBounds = m_Geometry.Bounds; //this will trigger OnChanged if neeeded.
                m_Geometry.Transform = m_ProjectionTransform;
                changingGeometry = false;
                Margin = new(Pen.Thickness / 2.0); //this will trigger OnChanged if neeeded.
            }
        }

        public override void SetTransform(Matrix axisToDisplay, Rect displayClip)
        {
            changingGeometry = true;
            m_ProjectionTransform.Matrix = m_geomToAxis * axisToDisplay;
            changingGeometry = false;
        }

        public override void DrawGraph(DrawingContext context)
            => context.DrawGeometry(m_Fill, m_Pen, m_Geometry);
    }
}
