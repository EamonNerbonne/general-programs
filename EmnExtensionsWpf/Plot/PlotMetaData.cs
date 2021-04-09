using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf {
    public sealed class PlotMetaData : IPlotMetaDataWriteable {
        IVizEngine m_viz;
        public IVizEngine Visualisation {
            get { return m_viz; }
            set { if (m_viz != null) throw new InvalidOperationException("The plot for this metadata has already been set."); m_viz = value; }
        }

        public void TriggerChange(GraphChange changeType) {
            if (Container != null) Container.GraphChanged(Visualisation, changeType);
            if (changeType == GraphChange.RenderOptions && Visualisation != null) Visualisation.OnRenderOptionsChanged();
        }
        void IPlotMetaData.GraphChanged(GraphChange changeType) { TriggerChange(changeType); }

        string m_xUnitLabel, m_yUnitLabel, m_DataLabel;
        public string XUnitLabel { get { return m_xUnitLabel; } set { if (m_xUnitLabel != value) { m_xUnitLabel = value; TriggerChange(GraphChange.Labels); } } }
        public string YUnitLabel { get { return m_yUnitLabel; } set { if (m_yUnitLabel != value) { m_yUnitLabel = value; TriggerChange(GraphChange.Labels); } } }
        public string DataLabel { get { return m_DataLabel; } set { if (m_DataLabel != value) { m_DataLabel = value; TriggerChange(GraphChange.Labels); } } }

        TickedAxisLocation m_axisBindings = TickedAxisLocation.Default;
        public TickedAxisLocation AxisBindings { get { return m_axisBindings; } set { if (m_axisBindings != value) { m_axisBindings = value; TriggerChange(GraphChange.Projection); } } }

        Rect? m_OverrideBounds;
        public Rect? OverrideBounds { get { return m_OverrideBounds; } set { if (m_OverrideBounds != value) { m_OverrideBounds = value; TriggerChange(GraphChange.Projection); } } }

        Thickness? m_OverrideMargin;
        public Thickness? OverrideMargin { get { return m_OverrideMargin; } set { if (m_OverrideMargin != value) { m_OverrideMargin = value; TriggerChange(GraphChange.Projection); } } }

        Rect? m_MinimalBounds;
        public Rect? MinimalBounds { get { return m_MinimalBounds; } set { if (m_MinimalBounds != value) { m_MinimalBounds = value; TriggerChange(GraphChange.Projection); } } }

        public object Tag { get; set; }

        public IPlotContainer Container { get; set; }

        Color? m_PrimaryColor;
        public Color? RenderColor { get { return m_PrimaryColor; } set { m_PrimaryColor = value; TriggerChange(GraphChange.RenderOptions); } }

        int zIndex;
        public int ZIndex { get { return zIndex; } set { zIndex = value; TriggerChange(GraphChange.Drawing); } }

        double? m_Thickness;
        public double? RenderThickness { get { return m_Thickness; } set { m_Thickness = value; TriggerChange(GraphChange.RenderOptions); } }

        bool hidden;
        public bool Hidden {
            get { return hidden; }
            set { if (hidden != value) { hidden = value; TriggerChange(GraphChange.Visibility); } }
        }

        public PlotMetaData() { }
        public PlotMetaData(IPlotMetaData clone) {
            this.XUnitLabel = clone.XUnitLabel;
            this.YUnitLabel = clone.YUnitLabel;
            this.DataLabel = clone.DataLabel;
            this.AxisBindings = clone.AxisBindings;
            this.OverrideBounds = clone.OverrideBounds;
            this.OverrideMargin = clone.OverrideMargin;
            this.MinimalBounds = clone.MinimalBounds;
            this.Tag = clone.Tag;
            this.RenderColor = clone.RenderColor;
            this.ZIndex = clone.ZIndex;
            this.RenderThickness = clone.RenderThickness;
            this.Hidden = clone.Hidden;
        }
    }
}
