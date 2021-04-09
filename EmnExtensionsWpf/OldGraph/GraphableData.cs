using System;
using System.Windows;
using System.Windows.Media;
using EmnExtensions.Wpf.Plot;

namespace EmnExtensions.Wpf.OldGraph
{
    public abstract class GraphableData
    {
        string m_xUnitLabel, m_yUnitLabel, m_DataLabel;
        Rect m_DataBounds = Rect.Empty;
        Thickness m_Margin;
        TickedAxisLocation m_axisBindings = TickedAxisLocation.LeftOfGraph | TickedAxisLocation.BelowGraph;

        public string XUnitLabel
        {
            get => m_xUnitLabel;
            set {
                if (m_xUnitLabel != value) {
                    m_xUnitLabel = value;
                    OnChange(GraphChange.Labels);
                }
            }
        }

        public string YUnitLabel
        {
            get => m_yUnitLabel;
            set {
                if (m_yUnitLabel != value) {
                    m_yUnitLabel = value;
                    OnChange(GraphChange.Labels);
                }
            }
        }

        public string DataLabel
        {
            get => m_DataLabel;
            set {
                if (m_DataLabel != value) {
                    m_DataLabel = value;
                    OnChange(GraphChange.Labels);
                }
            }
        }

        public Rect DataBounds
        {
            get => m_DataBounds;
            set {
                if (m_DataBounds != value) {
                    m_DataBounds = value;
                    OnChange(GraphChange.Projection);
                }
            }
        }

        public Thickness Margin
        {
            get => m_Margin;
            set {
                if (m_Margin != value) {
                    m_Margin = value;
                    OnChange(GraphChange.Projection);
                }
            }
        }

        public TickedAxisLocation AxisBindings
        {
            get => m_axisBindings;
            set {
                if (m_axisBindings != value) {
                    m_axisBindings = value;
                    OnChange(GraphChange.Projection);
                }
            }
        }

        public object Tag { get; set; }
        public event Action<GraphableData, GraphChange> Changed;

        protected void OnChange(GraphChange changeType)
        {
            var handler = Changed;

            if (handler != null) {
                handler(this, changeType);
            }
        }

        public abstract void DrawGraph(DrawingContext context);
        public abstract void SetTransform(Matrix boundsToDisplay, Rect displayClip);
    }
}
