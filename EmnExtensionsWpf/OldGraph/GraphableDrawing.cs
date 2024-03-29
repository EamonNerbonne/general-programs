using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.OldGraph
{
    public abstract class GraphableDrawing : GraphableData
    {
        readonly MatrixTransform m_drawingToDisplay = new();

        public static Rect ComputeDataBounds(Rect innerDrawingBounds, Rect innerDataBounds, Rect drawingBounds)
        {
            if (innerDataBounds.IsEmpty || innerDrawingBounds.IsEmpty) {
                return Rect.Empty;
            }

            var trans = GraphUtils.TransformShape(innerDrawingBounds, innerDataBounds, false);
            return Rect.Transform(drawingBounds, trans);
        }

        public sealed override void DrawGraph(DrawingContext context)
        {
            context.PushTransform(m_drawingToDisplay);
            try {
                DrawUntransformedIntoDrawingRect(context);
            } finally {
                context.Pop();
            }
        }

        public sealed override void SetTransform(Matrix dataToDisplay, Rect displayClip)
            => m_drawingToDisplay.Matrix = GraphUtils.TransformShape(DrawingRect, DataBounds, false) * dataToDisplay;

        protected abstract void DrawUntransformedIntoDrawingRect(DrawingContext context);
        protected abstract Rect DrawingRect { get; }
    }
}
