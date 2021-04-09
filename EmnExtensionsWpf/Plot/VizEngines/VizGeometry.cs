using System;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.VizEngines
{
    public class VizGeometry : PlotVizBase<Geometry>
    {
        readonly MatrixTransform m_ProjectionTransform = new MatrixTransform();
        readonly GeometryGroup combinesGeom = new GeometryGroup();
        readonly RectangleGeometry clipRectangle = new RectangleGeometry();

        bool m_AutosizeBounds = true;
        Brush m_Fill = Brushes.Black;
        Pen m_Pen = defaultPen;

        public VizGeometry(IPlotMetaData owner)
            : base(owner)
        {
            combinesGeom.Transform = m_ProjectionTransform;
            RecomputeMargin();
            RecreatePen();
        }

        static readonly Pen defaultPen = (Pen)new Pen { Brush = Brushes.Black, EndLineCap = PenLineCap.Square, StartLineCap = PenLineCap.Square, Thickness = 1.5, }.GetAsFrozen();

        public Brush Fill
        {
            get { return m_Fill; }
            set {
                if (m_Fill != value) {
                    m_Fill = value;
                    TriggerChange(GraphChange.Drawing);
                }
            }
        }

        public Pen Pen
        {
            get { return m_Pen; }
            set {
                if (m_Pen != value) {
                    if (m_Pen != null && !m_Pen.IsFrozen)
                        m_Pen.Changed -= m_Pen_Changed;
                    m_Pen = value;
                    if (m_Pen != null && !m_Pen.IsFrozen)
                        m_Pen.Changed += m_Pen_Changed;
                    m_Pen_Changed(null, null);
                }
            }
        }

        /// <summary>
        /// Defaults to true.
        /// </summary>
        public bool AutosizeBounds { get { return m_AutosizeBounds; } set { m_AutosizeBounds = value; RecomputeBoundsIfAuto(); } }

        protected override void OnDataChanged(Geometry oldData)
        {
            if (Data == oldData)
                return;
            if (oldData != null && !oldData.IsFrozen)
                oldData.Changed -= m_Geometry_Changed;

            if (Data == null)
                combinesGeom.Children.Clear();
            else if (combinesGeom.Children.Count > 0 && combinesGeom.Children[0] != Data)
                combinesGeom.Children[0] = Data;
            else
                combinesGeom.Children.Add(Data);


            if (Data != null && !Data.IsFrozen)
                Data.Changed += m_Geometry_Changed;
            RecomputeBoundsIfAuto();
            TriggerChange(GraphChange.Drawing);
        }

        void m_Pen_Changed(object sender, EventArgs e)
        {
            TriggerChange(GraphChange.Drawing);
            RecomputeMargin();
        }
        void RecomputeMargin()
        {//this will trigger OnChanged if neeeded.
            SetMargin(new Thickness(Pen.Thickness / 2.0));
        }
        void m_Geometry_Changed(object sender, EventArgs e) { RecomputeBoundsIfAuto(); }

        void RecomputeBoundsIfAuto()
        {
            if (m_AutosizeBounds)
                InvalidateDataBounds();//this will trigger OnChanged if neeeded.
        }
        protected override Rect ComputeBounds() { return Data.Bounds; }

        public override void SetTransform(Matrix axisToDisplay, Rect displayClip, double forDpiX, double forDpiY)
        {
            clipRectangle.Rect = displayClip;
            m_ProjectionTransform.Matrix = axisToDisplay;
        }

        public override void DrawGraph(DrawingContext context)
        {
            context.PushClip(clipRectangle);
            context.DrawGeometry(IsFilled ? m_Fill : null, IsStroked ? m_Pen : null, combinesGeom);
            context.Pop();
        }

        public override void OnRenderOptionsChanged() { RecreatePen(); }

        void RecreatePen()
        {
            var currentColor = ((SolidColorBrush)m_Pen.Brush).Color;
            var currentThickness = m_Pen.Thickness;
            var newColor = MetaData.RenderColor ?? currentColor;
            var newThickness = MetaData.RenderThickness ?? currentThickness;
            if (newThickness != currentThickness || newColor != currentColor) {
                if (newColor != currentColor)
                    TriggerChange(GraphChange.Labels);

                var newPen = m_Pen.CloneCurrentValue();
                newPen.Brush = Fill = new SolidColorBrush(newColor);
                newPen.Thickness = newThickness;
                newPen.Freeze();
                Pen = newPen;
            }
        }
        public override bool SupportsColor { get { return true; } }
        bool _isFilled;
        public bool IsFilled { get { return _isFilled; } set { if (_isFilled != value) { _isFilled = value; TriggerChange(GraphChange.Drawing); } } }
        bool _isStroked = true;
        public bool IsStroked { get { return _isStroked; } set { if (_isStroked != value) { _isStroked = value; TriggerChange(GraphChange.Drawing); } } }

    }
}
