// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Printing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf.WpfTools;
using Microsoft.Win32;

namespace EmnExtensions.Wpf.Plot
{
    public sealed partial class PlotControl : IPlotContainer
    {
        bool needRedrawGraphs;
        IEnumerable<IVizEngine> visibleGraphs => Graphs.Where(g => !g.MetaData.Hidden);
        public ObservableCollection<IVizEngine> Graphs { get; } = new();

        public IEnumerable<IVizEngine> GraphsEnumerable
        {
            get => Graphs;
            set {
                Graphs.Clear();
                foreach (var plot in value) {
                    Graphs.Add(plot);
                }
            }
        }

        readonly DrawingBrush bgBrush;
        readonly UIElement drawingUi;

        sealed class PlainDrawing : UIElement
        {
            readonly Drawing drawing;
            public PlainDrawing(DrawingGroup dg) => drawing = dg;
            protected override void OnRender(DrawingContext drawingContext) => drawingContext.DrawDrawing(drawing);
        }

        static readonly object syncType = new();

        public PlotControl()
        {
            Graphs.CollectionChanged += graphs_CollectionChanged;
            lock (syncType) {
                InitializeComponent(); //Apparently InitializeComponent isn't thread safe.
            }

            RenderOptions.SetBitmapScalingMode(dg, BitmapScalingMode.Linear);
            bgBrush = new(dg) {
                Stretch = Stretch.None, //No stretch since we want the ticked axis to determine stretch
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new(0, 0, 0, 0), //we want to to start displaying in the corner 0,0 - and width+height are irrelevant due to Stretch.None.
                AlignmentX = AlignmentX.Left, //and corner 0,0 is in the Top-Left!
                AlignmentY = AlignmentY.Top,
            };
            drawingUi = new PlainDrawing(dg);
            //plotArea.Background = bgBrush;
            manualRender = true;
        }

        public bool ShowAxes
        {
            get => (bool)GetValue(ShowAxesProperty);
            set => SetValue(ShowAxesProperty, value);
        }

        public static readonly DependencyProperty ShowAxesProperty =
            DependencyProperty.Register("ShowAxes", typeof(bool), typeof(PlotControl), new UIPropertyMetadata(true, ShowAxesSet));

        static void ShowAxesSet(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((PlotControl)d).SetAxesShow((bool)e.NewValue);

        void SetAxesShow(bool showAxes)
        {
            foreach (var axis in Axes) {
                axis.HideAxis = !showAxes;
            }
        }

        public bool UniformScaling
        {
            get => Axes.All(axis => axis.UniformScale);
            set {
                foreach (var axis in Axes) {
                    axis.UniformScale = value;
                }
            }
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(PlotControl), new UIPropertyMetadata(
                    (depObj, evtArg) => { (depObj as PlotControl).titleTextbox.Visibility = evtArg.NewValue == null ? Visibility.Collapsed : Visibility.Visible; }
                )
            );

        public void AutoPickColors(MersenneTwister rnd = null)
        {
            var ColoredPlots = (
                from graph in Graphs
                where graph != null && graph.SupportsColor
                select graph
            ).ToArray();
            var randomColors = WpfTools.WpfTools.MakeDistributedColors(ColoredPlots.Length, rnd);
            foreach (var plotAndColor in ColoredPlots.Zip(randomColors, Tuple.Create)) {
                plotAndColor.Item1.MetaData.RenderColor = plotAndColor.Item2;
            }
        }

        void RegisterChanged(IEnumerable<IVizEngine> newGraphs)
        {
            foreach (var newgraph in newGraphs) {
                newgraph.MetaData.Container = this;
            }
        }

        static void UnregisterChanged(IEnumerable<IVizEngine> oldGraphs)
        {
            foreach (var oldgraph in oldGraphs) {
                oldgraph.MetaData.Container = null;
            }
        }

        void graphs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) {
                UnregisterChanged(e.OldItems.Cast<IVizEngine>());
            }

            if (e.NewItems != null) {
                RegisterChanged(e.NewItems.Cast<IVizEngine>());
            }

            if (e.OldItems != null && e.OldItems.Count > 0 || e.NewItems != null && e.NewItems.Cast<IVizEngine>().Any(p => !p.MetaData.Hidden)) {
                RequireRedisplay();
            }
        }

        void RequireRedisplay()
        {
            InvalidateMeasure();

            needRedrawGraphs = true;
            InvalidateVisual();

            labelarea.Children.Clear();
            var any = false;
            var label = new TextBlock {
                TextWrapping = TextWrapping.Wrap
            };
            foreach (var graph in Graphs) {
                if (graph.MetaData.DataLabel == null) {
                    continue;
                }

                if (any) {
                    label.Inlines.Add(";  ");
                }

                any = true;
                label.Inlines.Add(new Image {
                        Source = new DrawingImage(graph.SampleDrawing).AsFrozen(),
                        Stretch = Stretch.None,
                        Margin = new(2, 0, 2, 0)
                    }
                );
                label.Inlines.Add(graph.MetaData.DataLabel);
            }

            if (any) {
                labelarea.Children.Add(label);
            }
        }


        void IPlotContainer.GraphChanged(IVizEngine plot, GraphChange graphChange)
        {
            if (GraphChange.Visibility == graphChange) {
                RequireRedisplay();
            } else if (!plot.MetaData.Hidden) {
                switch (graphChange) {
                    case GraphChange.Drawing:
                        needRedrawGraphs = true;
                        InvalidateMeasure();
                        InvalidateVisual();
                        break;
                    case GraphChange.Projection:
                        InvalidateMeasure();
                        InvalidateVisual();
                        break;
                    case GraphChange.Labels:
                        RequireRedisplay();
                        break;
                }
            }
        }

        IEnumerable<TickedAxis> Axes
        {
            get {
                yield return tickedAxisLft;
                yield return tickedAxisBot;
                yield return tickedAxisRgt;
                yield return tickedAxisTop;
            }
        }

        public bool? AttemptBorderTicks
        {
            set {
                if (value.HasValue) {
                    foreach (var axis in Axes) {
                        axis.AttemptBorderTicks = value.Value;
                    }
                }
            }
            get {
                var vals = Axes.Select(axis => axis.AttemptBorderTicks).Distinct().ToArray();
                return vals.Length != 1 ? null : vals[0];
            }
        }

        #region Static Helper Functions

        static IEnumerable<TickedAxisLocation> ProjectionCorners
        {
            get {
                yield return TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph;
                yield return TickedAxisLocation.BelowGraph | TickedAxisLocation.RightOfGraph;
                yield return TickedAxisLocation.AboveGraph | TickedAxisLocation.LeftOfGraph;
                yield return TickedAxisLocation.AboveGraph | TickedAxisLocation.RightOfGraph;
            }
        }

        static DimensionBounds ToDimBounds(Rect bounds, bool isHorizontal) => bounds.IsEmpty || bounds.Width == 0 || bounds.Height == 0 ? DimensionBounds.Empty : isHorizontal ? DimensionBounds.FromRectX(bounds) : DimensionBounds.FromRectY(bounds);
        static DimensionMargins ToDimMargins(Thickness margins, bool isHorizontal) => isHorizontal ? DimensionMargins.FromThicknessX(margins) : DimensionMargins.FromThicknessY(margins);
        static TickedAxisLocation ChooseProjection(IVizEngine graph) => ProjectionCorners.FirstOrDefault(corner => (graph.MetaData.AxisBindings & corner) == corner);

        #endregion

        void RecomputeBounds()
        {
            Trace.WriteLine("RecomputeBounds");

            foreach (var axis in Axes) {
                // ReSharper disable AccessToModifiedClosure
                var boundGraphs = visibleGraphs.Where(graph => (graph.MetaData.AxisBindings & axis.AxisPos) != 0);
                var bounds =
                    boundGraphs
                        .Select(graph => ToDimBounds(graph.EffectiveDataBounds(), axis.IsHorizontal))
                        .Aggregate(DimensionBounds.Empty, DimensionBounds.Merge);
                var margin =
                    boundGraphs
                        .Select(graph => ToDimMargins(graph.Margin, axis.IsHorizontal))
                        .Aggregate(DimensionMargins.Empty, DimensionMargins.Merge);
                var dataUnits = string.Join(", ", boundGraphs.Select(graph => axis.IsHorizontal ? graph.MetaData.XUnitLabel : graph.MetaData.YUnitLabel).Distinct().Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());
                // ReSharper restore AccessToModifiedClosure

                axis.DataBound = bounds;
                axis.DataMargin = margin;
                axis.DataUnits = dataUnits;
            }
        }

        void RedrawGraphs(TickedAxisLocation gridLineAxes)
        {
            Trace.WriteLine("Redrawing Graphs");
            using (var drawingContext = dg.Open()) {
                RedrawScene(drawingContext, gridLineAxes);
            }

            needRedrawGraphs = false;
        }

        public bool ShowGridLines
        {
            get => (bool)GetValue(ShowGridLinesProperty);
            set => SetValue(ShowGridLinesProperty, value);
        }

        // Using a DependencyProperty as the backing store for ShowGridLines.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowGridLinesProperty =
            DependencyProperty.Register("ShowGridLines", typeof(bool), typeof(PlotControl), new UIPropertyMetadata(false, ShowGridLinesChanged));

        static void ShowGridLinesChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var self = (PlotControl)o;
            foreach (var axis in self.Axes) {
                axis.MatchOppositeTicks = (bool)e.NewValue;
            }

            self.needRedrawGraphs = true;
            self.InvalidateVisual();
        }

        void RedrawScene(DrawingContext drawingContext, TickedAxisLocation gridLineAxes)
        {
            //drawingContext.PushClip(overallClipRect);
            if (ShowGridLines) {
                foreach (var axis in Axes) {
                    if ((axis.AxisPos & gridLineAxes) != 0) {
                        drawingContext.DrawDrawing(axis.GridLines);
                    }
                }
            }

            foreach (var graph in visibleGraphs.OrderBy(g => g.MetaData.ZIndex)) {
                graph.DrawGraph(drawingContext);
            }
            //drawingContext.Pop();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            RecomputeBounds();
            return base.MeasureOverride(constraint);
        }

        //readonly RectangleGeometry overallClipRect = new RectangleGeometry();
        readonly DrawingGroup dg = new();

        protected override void OnRender(DrawingContext drawingContext)
        {
            Trace.WriteLine("PlotControl.OnRender");
            //axes which influence projection matrices:
            var relevantAxes = visibleGraphs.Aggregate(TickedAxisLocation.None, (axisLoc, graph) => axisLoc | ChooseProjection(graph));
            var transforms =
                from axis in Axes
                where (axis.AxisPos & relevantAxes) != 0 && axis.DataBound.Length > 0
                select new {
                    axis.AxisPos,
                    Transform = axis.DataToDisplayTransform,
                    HorizontalClip = axis.IsHorizontal ? axis.DisplayClippingBounds : DimensionBounds.Empty,
                    VerticalClip = axis.IsHorizontal ? DimensionBounds.Empty : axis.DisplayClippingBounds,
                };

            var cornerProjection = (
                from corner in ProjectionCorners
                where corner == (corner & relevantAxes)
                let relevantTransforms = transforms.Where(transform => transform.AxisPos == (transform.AxisPos & corner))
                where relevantTransforms.Count() == 2
                select relevantTransforms.Aggregate((t1, t2) => new {
                        AxisPos = t1.AxisPos | t2.AxisPos,
                        Transform = t1.Transform * t2.Transform,
                        HorizontalClip = DimensionBounds.Merge(t1.HorizontalClip, t2.HorizontalClip),
                        VerticalClip = DimensionBounds.Merge(t1.VerticalClip, t2.VerticalClip),
                    }
                )
            ).ToDictionary(cornerTransform => cornerTransform.AxisPos);

            //Rect overallClip =
            //cornerProjection.Values.Select(trans => new Rect(new Point(trans.HorizontalClip.Start, trans.VerticalClip.Start), new Point(trans.HorizontalClip.End, trans.VerticalClip.End)))
            //    .Aggregate(Rect.Empty, Rect.Union);
            //overallClipRect.Rect = overallClip;

            foreach (var graph in visibleGraphs) {
                var graphCorner = ChooseProjection(graph);
                if (cornerProjection.ContainsKey(graphCorner)) {
                    var trans = cornerProjection[graphCorner];
                    var bounds = new Rect(new(trans.HorizontalClip.Start, trans.VerticalClip.Start), new Point(trans.HorizontalClip.End, trans.VerticalClip.End));
                    graph.SetTransform(trans.Transform, bounds, m_dpiX, m_dpiY);
                } else {
                    graph.SetTransform(Matrix.Identity, Rect.Empty, m_dpiX, m_dpiY);
                }
            }

            var axisBounds = Axes.Aggregate(Rect.Empty, (bound, axis) => Rect.Union(bound, new Rect(axis.RenderSize)));
            foreach (var axis in Axes) {
                axis.SetGridLineExtent(axisBounds.Size);
            }

            if (needRedrawGraphs) {
                RedrawGraphs(relevantAxes);
            }

            if (manualRender) {
                plotArea.Background = null;
                if (!plotArea.Children.Contains(drawingUi)) {
                    plotArea.Children.Add(drawingUi);
                }
            } else {
                plotArea.Background = bgBrush;
                if (plotArea.Children.Contains(drawingUi)) {
                    plotArea.Children.Remove(drawingUi);
                }
            }

            base.OnRender(drawingContext);
        }

        double m_dpiX = 96;
        double m_dpiY = 96;
        readonly bool manualRender;

        void ExportGraph(object sender, RoutedEventArgs e)
        {
            var xpsData = PrintToByteArray();
            var dialogThread = new Thread(() => {
                    var saveDialog = new SaveFileDialog {
                        AddExtension = true,
                        CheckPathExists = true,
                        DefaultExt = ".xps",
                        Filter = "XPS files (*.xps)|*.xps",
                    };

                    using (var emnExtensionsWpfKey = Registry.CurrentUser.OpenSubKey(@"Software\EmnExtensionsWpf")) {
                        if (emnExtensionsWpfKey != null) {
                            saveDialog.InitialDirectory = emnExtensionsWpfKey.GetValue("ExportDir") as string;
                        }
                    }


                    // ReSharper disable ConstantNullCoalescingCondition
                    if (saveDialog.ShowDialog() ?? false) {
                        // ReSharper restore ConstantNullCoalescingCondition
                        var selectedFile = new FileInfo(saveDialog.FileName);
                        using (var emnExtensionsWpfKey = Registry.CurrentUser.CreateSubKey(@"Software\EmnExtensionsWpf")) {
                            emnExtensionsWpfKey.SetValue("ExportDir", selectedFile.Directory.FullName);
                        }

                        using (var fileStream = selectedFile.Open(FileMode.Create)) {
                            fileStream.Write(xpsData, 0, xpsData.Length);
                        }
                    }
                }
            );
            dialogThread.SetApartmentState(ApartmentState.STA);
            dialogThread.IsBackground = true;
            dialogThread.Start();
        }

        public byte[] PrintToByteArray()
        {
            using (var ms = new MemoryStream()) {
                PrintToStream(ms);
                return ms.ToArray();
            }
        }

        void PrintToStream(Stream writeTo)
        {
            try {
                //manualRender = true;
                m_dpiX = 288.0;
                m_dpiY = 288.0;
                WpfTools.WpfTools.PrintXPS(this, 350, 350, writeTo, FileMode.Create, FileAccess.ReadWrite);
            } finally {
                //manualRender = false;
                m_dpiX = 96.0;
                m_dpiY = 96.0;
                RequireRedisplay();
            }
        }

        void PrintGraph(object sender, RoutedEventArgs ree)
        {
            var xpsData = PrintToByteArray();

            var printThread = new Thread(() => {
                    var tempFile = Path.GetTempFileName();
                    try {
                        File.WriteAllBytes(tempFile, xpsData);
                        using (var defaultPrintQueue = LocalPrintServer.GetDefaultPrintQueue())
                        using (var dataReadStream = File.OpenRead(tempFile))
                        using (var package = Package.Open(dataReadStream, FileMode.Open, FileAccess.Read))
                        using (var doc = new XpsDocument(package, CompressionOption.Normal, tempFile)) {
                            var xpsPrintWriter = PrintQueue.CreateXpsDocumentWriter(defaultPrintQueue);
                            xpsPrintWriter.Write(doc.GetFixedDocumentSequence());
                        }
                    } catch (Exception e) {
                        Console.WriteLine("Printing error!\n{0}", e);
                    }
                }
            );
            printThread.SetApartmentState(ApartmentState.STA);
            printThread.Start();
        }

        public string PlotName { get; set; }
    }
}
