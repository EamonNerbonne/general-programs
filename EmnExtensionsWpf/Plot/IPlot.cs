using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf {
    public interface IPlotContainer {
        Dispatcher Dispatcher { get; }
        void GraphChanged(IVizEngine plot, GraphChange changeType);
    }

    public interface IPlotMetaData {
        string XUnitLabel { get; }
        string YUnitLabel { get; }
        string DataLabel { get; }
        int ZIndex { get; }

        TickedAxisLocation AxisBindings { get; set; }
        bool Hidden { get; set; }

        void TriggerChange(GraphChange changeType);
        void GraphChanged(GraphChange changeType);

        Rect? OverrideBounds { get; }
        Thickness? OverrideMargin { get; }
        Rect? MinimalBounds { get; }

        Color? RenderColor { get; set; }
        double? RenderThickness { get; }
        object Tag { get; }
        IPlotContainer Container { get; set; }
        IVizEngine Visualisation { get; }
    }

    public interface IPlotMetaDataWriteable : IPlotMetaData {
        new string XUnitLabel { get; set; }
        new string YUnitLabel { get; set; }
        new string DataLabel { get; set; }
        new int ZIndex { get; set; }
        new Rect? OverrideBounds { get; set; }
        new Rect? MinimalBounds { get; set; }
        new double? RenderThickness { get; set; }
        new object Tag { get; set; }
    }
}
