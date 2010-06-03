using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Printing;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.IO.Packaging;

namespace EmnExtensions.Wpf.Plot {
	public partial class PlotControl : UserControl {
		bool needRedrawGraphs = false;
		bool needRecomputeBounds = false;
		ObservableCollection<IPlotViewOnly> graphs = new ObservableCollection<IPlotViewOnly>();
		public ObservableCollection<IPlotViewOnly> Graphs { get { return graphs; } }
		DrawingBrush bgBrush;
		public PlotControl() {
			graphs.CollectionChanged += new NotifyCollectionChangedEventHandler(graphs_CollectionChanged);
			InitializeComponent();
			RenderOptions.SetBitmapScalingMode(dg, BitmapScalingMode.Linear);
			bgBrush = new DrawingBrush(dg) {
				Stretch = Stretch.None, //No stretch since we want the ticked axis to determine stretch
				ViewboxUnits = BrushMappingMode.Absolute,
				Viewbox = new Rect(0, 0, 0, 0), //we want to to start displaying in the corner 0,0 - and width+height are irrelevant due to Stretch.None.
				AlignmentX = AlignmentX.Left, //and corner 0,0 is in the Top-Left!
				AlignmentY = AlignmentY.Top,
			};
			Background = bgBrush;
		}


		public bool ShowAxes {
			get { return (bool)GetValue(ShowAxesProperty); }
			set { SetValue(ShowAxesProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShowAxes.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowAxesProperty =
			DependencyProperty.Register("ShowAxes", typeof(bool), typeof(PlotControl), new UIPropertyMetadata(true, new PropertyChangedCallback(ShowAxesSet)));

		private static void ShowAxesSet(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			((PlotControl)d).SetAxesShow((bool)e.NewValue);
		}

		void SetAxesShow(bool showAxes) {
			foreach (var axis in Axes)
				axis.Visibility = showAxes ? Visibility.Visible : Visibility.Collapsed;
		}

		void RegisterChanged(IEnumerable<IPlotViewOnly> newGraphs) {
			foreach (IPlotViewOnly newgraph in newGraphs)
				newgraph.Changed += new Action<IPlotViewOnly, GraphChange>(graphChanged);
		}

		public void AutoPickColors() {
			var ColoredPlots = (
									from graph in Graphs
									let plotWithSettings = graph as IPlotWithSettings
									where plotWithSettings != null && plotWithSettings.VizSupportsColor
									select plotWithSettings
							   ).ToArray();
			var randomColors = EmnExtensions.Wpf.GraphRandomPen.MakeDistributedColors(ColoredPlots.Length);
			foreach (var plotAndColor in ColoredPlots.Zip(randomColors, (a, b) => Tuple.Create(a, b))) {
				plotAndColor.Item1.RenderColor = plotAndColor.Item2;
			}
		}

		void UnregisterChanged(IEnumerable<IPlotViewOnly> oldGraphs) {
			foreach (IPlotViewOnly oldgraph in oldGraphs)
				oldgraph.Changed -= new Action<IPlotViewOnly, GraphChange>(graphChanged);
		}

		void graphs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.OldItems != null)
				UnregisterChanged(e.OldItems.Cast<IPlotViewOnly>());
			if (e.NewItems != null)
				RegisterChanged(e.NewItems.Cast<IPlotViewOnly>());
			RequireRedisplay();
		}

		private void RequireRedisplay() {
			needRecomputeBounds = true;
			InvalidateMeasure(); //todo; flag and invalidatemeasure always together?

			needRedrawGraphs = true;
			InvalidateVisual();//todo; flag and InvalidateVisual always together?
		}

		void graphChanged(IPlotViewOnly graph, GraphChange graphChange) {
			if (graphChange == GraphChange.Drawing) {
				needRedrawGraphs = true;
				InvalidateVisual();
			} else if (graphChange == GraphChange.Labels || graphChange == GraphChange.Projection) {
				needRecomputeBounds = true;
				InvalidateMeasure();
			}
		}

		private IEnumerable<TickedAxis> Axes { get { yield return tickedAxisLft; yield return tickedAxisBot; yield return tickedAxisRgt; yield return tickedAxisTop; } }

		public bool? AttemptBorderTicks {
			set { if (value.HasValue) foreach (var axis in Axes) axis.AttemptBorderTicks = value.Value; }
			get {
				bool[] vals = Axes.Select(axis => axis.AttemptBorderTicks).Distinct().ToArray();
				return vals.Length != 1 ? (bool?)null : vals[0];
			}
		}

		#region Static Helper Functions
		private static IEnumerable<TickedAxisLocation> ProjectionCorners {
			get {
				yield return TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph;
				yield return TickedAxisLocation.BelowGraph | TickedAxisLocation.RightOfGraph;
				yield return TickedAxisLocation.AboveGraph | TickedAxisLocation.LeftOfGraph;
				yield return TickedAxisLocation.AboveGraph | TickedAxisLocation.RightOfGraph;
			}
		}
		private static DimensionBounds ToDimBounds(Rect bounds, bool isHorizontal) { return isHorizontal ? DimensionBounds.FromRectX(bounds) : DimensionBounds.FromRectY(bounds); }
		private static DimensionMargins ToDimMargins(Thickness margins, bool isHorizontal) { return isHorizontal ? DimensionMargins.FromThicknessX(margins) : DimensionMargins.FromThicknessY(margins); }
		private static TickedAxisLocation ChooseProjection(IPlotViewOnly graph) { return ProjectionCorners.FirstOrDefault(corner => (graph.AxisBindings & corner) == corner); }
		#endregion

		private void RecomputeBounds() {
			Trace.WriteLine("RecomputeBounds");
			foreach (TickedAxis axis in Axes) {
				var boundGraphs = graphs.Where(graph => (graph.AxisBindings & axis.AxisPos) != 0);
				DimensionBounds bounds =
					boundGraphs
					.Select(graph => ToDimBounds(graph.PlotVisualizer.DataBounds, axis.IsHorizontal))
					.Aggregate(DimensionBounds.Empty, (bounds1, bounds2) => DimensionBounds.Merge(bounds1, bounds2));
				DimensionMargins margin =
					boundGraphs
					.Select(graph => ToDimMargins(graph.PlotVisualizer.Margin, axis.IsHorizontal))
					.Aggregate(DimensionMargins.Empty, (m1, m2) => DimensionMargins.Merge(m1, m2));
				string dataUnits = string.Join(", ", graphs.Select(graph => axis.IsHorizontal ? graph.XUnitLabel : graph.YUnitLabel).Distinct().Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());

				axis.DataBound = bounds;
				axis.DataMargin = margin;
				axis.DataUnits = dataUnits;
			}
			needRecomputeBounds = false;
		}

		private void RedrawGraphs(TickedAxisLocation gridLineAxes) {
			Trace.WriteLine("Redrawing Graphs");
			using (var drawingContext = dg.Open())
				RedrawScene(drawingContext, gridLineAxes);
			needRedrawGraphs = false;
		}

		public bool ShowGridLines {
			get { return (bool)GetValue(ShowGridLinesProperty); }
			set { SetValue(ShowGridLinesProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShowGridLines.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowGridLinesProperty =
			DependencyProperty.Register("ShowGridLines", typeof(bool), typeof(PlotControl), new UIPropertyMetadata(false,
				(o, e) => {
					((PlotControl)o).needRedrawGraphs = true;
					((PlotControl)o).InvalidateVisual();
				}
				));


		private void RedrawScene(DrawingContext drawingContext, TickedAxisLocation gridLineAxes) {
			//drawingContext.PushClip(overallClipRect);
			if (ShowGridLines)
				foreach (var axis in Axes)
					if ((axis.AxisPos & gridLineAxes) != 0)
						drawingContext.DrawDrawing(axis.GridLines);
			foreach (var graph in graphs.OrderBy(g => g.ZIndex))
				graph.PlotVisualizer.DrawGraph(drawingContext);
			//drawingContext.Pop();
		}

		protected override Size MeasureOverride(Size constraint) {
			if (needRecomputeBounds) RecomputeBounds();
			return base.MeasureOverride(constraint);
		}
		RectangleGeometry overallClipRect = new RectangleGeometry();
		DrawingGroup dg = new DrawingGroup();

		protected override void OnRender(DrawingContext drawingContext) {
			Trace.WriteLine("NewPlotControl.OnRender");
			//axes which influence projection matrices:
			TickedAxisLocation relevantAxes = graphs.Aggregate(TickedAxisLocation.None, (axisLoc, graph) => axisLoc | ChooseProjection(graph));
			var transforms =
				from axis in Axes
				where (axis.AxisPos & relevantAxes) != 0
				select new {
					AxisPos = axis.AxisPos,
					Transform = axis.DataToDisplayTransform,
					HorizontalClip = axis.IsHorizontal ? axis.DisplayClippingBounds : DimensionBounds.Empty,
					VerticalClip = axis.IsHorizontal ? DimensionBounds.Empty : axis.DisplayClippingBounds,
				};

			var cornerProjection =
				ProjectionCorners
					.Where(corner => corner == (corner & relevantAxes))
					.ToDictionary(//we have only relevant corners...
						corner => corner,
						corner => transforms.Where(transform => transform.AxisPos == (transform.AxisPos & corner))
										   .Aggregate((t1, t2) => new {
											   AxisPos = t1.AxisPos | t2.AxisPos,
											   Transform = t1.Transform * t2.Transform,
											   HorizontalClip = DimensionBounds.Merge(t1.HorizontalClip, t2.HorizontalClip),
											   VerticalClip = DimensionBounds.Merge(t1.VerticalClip, t2.VerticalClip),
										   })
					);

			Rect overallClip =
			cornerProjection.Values.Select(trans => new Rect(new Point(trans.HorizontalClip.Start, trans.VerticalClip.Start), new Point(trans.HorizontalClip.End, trans.VerticalClip.End)))
				.Aggregate(Rect.Empty, (rect1, rect2) => Rect.Union(rect1, rect2));
			overallClipRect.Rect = overallClip;

			foreach (var graph in graphs) {
				var trans = cornerProjection[ChooseProjection(graph)];
				Rect bounds = new Rect(new Point(trans.HorizontalClip.Start, trans.VerticalClip.Start), new Point(trans.HorizontalClip.End, trans.VerticalClip.End));
				graph.PlotVisualizer.SetTransform(trans.Transform, bounds, m_dpiX, m_dpiY);
			}
			foreach (var axis in Axes)
				axis.SetGridLineExtent(RenderSize);
			if (needRedrawGraphs) RedrawGraphs(relevantAxes);
			if (manualRender) {
				drawingContext.DrawDrawing(dg);
				Background = null;
			} else if (Background == null)
				Background = bgBrush;

			base.OnRender(drawingContext);
		}

		double m_dpiX = 96.0;
		double m_dpiY = 96.0;
		bool manualRender = false;

		private void ExportGraph(object sender, RoutedEventArgs e) {
			byte[] xpsData = PrintToByteArray();
			var dialogThread = new Thread(() => {
				SaveFileDialog saveDialog = new SaveFileDialog() {
					AddExtension = true,
					CheckPathExists = true,
					DefaultExt = ".xps",
					Filter = "XPS files (*.xps)|*.xps",
				};
				if (saveDialog.ShowDialog() ?? false) {
					FileInfo selectedFile = new FileInfo(saveDialog.FileName);
					using (var fileStream = selectedFile.Open(FileMode.Create))
						fileStream.Write(xpsData, 0, xpsData.Length);
				}
			});
			dialogThread.SetApartmentState(ApartmentState.STA);
			dialogThread.IsBackground = true;
			dialogThread.Start();
		}

		private byte[] PrintToByteArray() {
			using (MemoryStream ms = new MemoryStream()) {
				PrintToStream(ms);
				return ms.ToArray();
			}
		}

		private void PrintToStream(Stream writeTo) {
			try {
				manualRender = true; m_dpiX = 192.0; m_dpiY = 192.0;
				WpfTools.PrintXPS(this, this.ActualWidth, this.ActualHeight, 1.0, writeTo, FileMode.Create, FileAccess.ReadWrite);
			} finally {
				manualRender = false; m_dpiX = 96.0; m_dpiY = 96.0;
			}
		}



		private void PrintGraph(object sender, RoutedEventArgs ree) {
			//			new PrintDialog().PrintVisual(this, "PrintGraph");

			byte[] xpsData = PrintToByteArray();

			var printThread = new Thread(() => {
				string tempFile = System.IO.Path.GetTempFileName();
				try {
					File.WriteAllBytes(tempFile, xpsData);
					using (LocalPrintServer localPrintServer = new LocalPrintServer())
					using (PrintQueue defaultPrintQueue = LocalPrintServer.GetDefaultPrintQueue())
					using (var dataReadStream = File.OpenRead(tempFile))
					using (Package package = Package.Open(dataReadStream, FileMode.Open, FileAccess.Read))
					using (XpsDocument doc = new XpsDocument(package, CompressionOption.Normal, tempFile)) {
						var xpsPrintWriter = PrintQueue.CreateXpsDocumentWriter(defaultPrintQueue);
						xpsPrintWriter.Write(doc.GetFixedDocumentSequence());
					}
				} catch (Exception e) {
					Console.WriteLine("Printing error!\n{0}", e);
				}
			});
			printThread.SetApartmentState(ApartmentState.STA);
			printThread.Start();
		}
	}
}
