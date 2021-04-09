using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EmnExtensions.Wpf.Plot.VizEngines;

namespace EmnExtensions.Wpf.Plot
{
    public static class PlotHelpers
    {
        static T HelpCreate<T>(Func<IPlotMetaData, T> factory, IPlotMetaData metadata = null)
            where T : IVizEngine
        {
            var newmetadata = metadata == null ? new PlotMetaData() : new PlotMetaData(metadata);
            var plot = factory(newmetadata);
            newmetadata.Visualisation = plot;
            return plot;
        }

        public static VizLineSegments CreateLine(IPlotMetaData metadata = null) => HelpCreate(md => new VizLineSegments(md), metadata);
        public static VizDataRange CreateDataRange(IPlotMetaData metadata = null) => HelpCreate(md => new VizDataRange(md), metadata);
        public static VizPixelScatterSmart CreatePixelScatter(IPlotMetaData metadata = null) => HelpCreate(md => new VizPixelScatterSmart(md), metadata);
        public static VizPointCloudBitmap CreatePointCloud(IPlotMetaData metadata = null) => HelpCreate(md => new VizPointCloudBitmap(md), metadata);
        public static VizDelegateBitmap<T> CreateBitmapDelegate<T>(Action<WriteableBitmap, Matrix, int, int, T> bitmapDelegate, IPlotMetaData metadata = null) => HelpCreate(md => new VizDelegateBitmap<T>(md), metadata).Update(plot => plot.UpdateBitmapDelegate = bitmapDelegate);
    }

    public static class PlotExtensions
    {
        public static T Update<T>(this T plot, Action<T> process)
            where T : IVizEngine
        {
            process(plot);
            return plot;
        }

        public static Rect EffectiveDataBounds(this IVizEngine plot) => plot.MetaData.OverrideBounds ?? Rect.Union(plot.DataBounds, plot.MetaData.MinimalBounds ?? Rect.Empty);
        public static IVizEngine<TIn> Map<TOut, TIn>(this IVizEngine<TOut> impl, Func<TIn, TOut> map) => new VizMapped<TIn, TOut>(impl, map);
        public static DispatcherOperation BeginDataChange<T>(this IVizEngine<T> sink, T data, DispatcherPriority priority = DispatcherPriority.Background) => sink.MetaData.Container.Dispatcher.BeginInvoke((Action<T>)sink.ChangeData, priority, data);
        public static Dispatcher GetDispatcher<T>(this IVizEngine<T> sink) => sink == null || sink.MetaData == null || sink.MetaData.Container == null ? null : sink.MetaData.Container.Dispatcher;
    }
}
