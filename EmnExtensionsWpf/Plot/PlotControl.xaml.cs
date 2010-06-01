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

namespace EmnExtensions.Wpf.Plot
{
	public partial class PlotControl : UserControl
	{
		bool needRedrawGraphs = false;
		bool needRecomputeBounds = false;
		ObservableCollection<IPlotViewOnly> graphs = new ObservableCollection<IPlotViewOnly>();
		public ObservableCollection<IPlotViewOnly> Graphs { get { return graphs; } }
		Dictionary<TickedAxisLocation, TickedAxis> axes;
		public PlotControl()
		{
			graphs.CollectionChanged += new NotifyCollectionChangedEventHandler(graphs_CollectionChanged);
			InitializeComponent();
			axes = new[] { tickedAxisLft, tickedAxisBot, tickedAxisRgt, tickedAxisTop }.ToDictionary(axis => axis.AxisPos);
			RenderOptions.SetBitmapScalingMode(dg, BitmapScalingMode.Linear);
			Background = new DrawingBrush(dg)
			{
				Stretch = Stretch.None, //No stretch since we want the ticked axis to determine stretch
				ViewboxUnits = BrushMappingMode.Absolute,
				Viewbox = new Rect(0, 0, 0, 0), //we want to to start displaying in the corner 0,0 - and width+height are irrelevant due to Stretch.None.
				AlignmentX = AlignmentX.Left, //and corner 0,0 is in the Top-Left!
				AlignmentY = AlignmentY.Top,
			};
		}

		void RegisterChanged(IEnumerable<IPlotViewOnly> newGraphs)
		{
			foreach (IPlotViewOnly newgraph in newGraphs)
				newgraph.Changed += new Action<IPlotViewOnly, GraphChange>(graphChanged);
		}

		public void AutoPickColors()
		{
			var ColoredPlots = (
									from graph in Graphs
									let plotWithSettings = graph as IPlotWithSettings
									where plotWithSettings != null && plotWithSettings.VizSupportsColor
									select plotWithSettings
							   ).ToArray();
			var randomColors = EmnExtensions.Wpf.OldGraph.GraphRandomPen.MakeDistributedColors(ColoredPlots.Length);
			foreach (var plotAndColor in ColoredPlots.Zip(randomColors, (a, b) => Tuple.Create(a, b)))
			{
				plotAndColor.Item1.RenderColor = plotAndColor.Item2;
			}
		}

		void UnregisterChanged(IEnumerable<IPlotViewOnly> oldGraphs)
		{
			foreach (IPlotViewOnly oldgraph in oldGraphs)
				oldgraph.Changed -= new Action<IPlotViewOnly, GraphChange>(graphChanged);
		}

		void graphs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
				UnregisterChanged(e.OldItems.Cast<IPlotViewOnly>());
			if (e.NewItems != null)
				RegisterChanged(e.NewItems.Cast<IPlotViewOnly>());
			RequireRedisplay();
		}

		private void RequireRedisplay()
		{
			needRecomputeBounds = true;
			InvalidateMeasure(); //todo; flag and invalidatemeasure always together?

			needRedrawGraphs = true;
			InvalidateVisual();//todo; flag and InvalidateVisual always together?
		}

		void graphChanged(IPlotViewOnly graph, GraphChange graphChange)
		{
			if (graphChange == GraphChange.Drawing)
			{
				needRedrawGraphs = true;
				InvalidateVisual();
			}
			else if (graphChange == GraphChange.Labels || graphChange == GraphChange.Projection)
			{
				needRecomputeBounds = true;
				InvalidateMeasure();
			}
		}

		private IEnumerable<TickedAxis> Axes { get { return new[] { tickedAxisLft, tickedAxisBot, tickedAxisRgt, tickedAxisTop }; } }

		public bool? AttemptBorderTicks
		{
			set { if (value.HasValue) foreach (var axis in Axes) axis.AttemptBorderTicks = value.Value; }
			get
			{
				bool[] vals = Axes.Select(axis => axis.AttemptBorderTicks).Distinct().ToArray();
				return vals.Length != 1 ? (bool?)null : vals[0];
			}
		}

		#region Static Helper Functions
		private static IEnumerable<TickedAxisLocation> ProjectionCorners
		{
			get
			{
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

		private void RecomputeBounds()
		{
			Trace.WriteLine("RecomputeBounds");
			foreach (TickedAxis axis in Axes)
			{
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

		private void RedrawGraphs(TickedAxisLocation gridLineAxes)
		{
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


		private void RedrawScene(DrawingContext drawingContext, TickedAxisLocation gridLineAxes)
		{
			if (ShowGridLines)
				foreach (var axis in Axes)
					if ((axis.AxisPos & gridLineAxes) != 0)
						drawingContext.DrawDrawing(axis.GridLines);
			foreach (var graph in graphs.OrderBy(g => g.ZIndex))
				graph.PlotVisualizer.DrawGraph(drawingContext);
		}

		protected override Size MeasureOverride(Size constraint)
		{
			if (needRecomputeBounds) RecomputeBounds();
			return base.MeasureOverride(constraint);
		}

		DrawingGroup dg = new DrawingGroup();

		protected override void OnRender(DrawingContext drawingContext)
		{
			Trace.WriteLine("NewPlotControl.OnRender");
			//axes which influence projection matrices:
			TickedAxisLocation relevantAxes = graphs.Aggregate(TickedAxisLocation.None, (axisLoc, graph) => axisLoc | ChooseProjection(graph));
			var transforms =
				from axis in Axes
				where (axis.AxisPos & relevantAxes) != 0
				select new
				{
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
										   .Aggregate((t1, t2) => new
										   {
											   AxisPos = t1.AxisPos | t2.AxisPos,
											   Transform = t1.Transform * t2.Transform,
											   HorizontalClip = DimensionBounds.Merge(t1.HorizontalClip, t2.HorizontalClip),
											   VerticalClip = DimensionBounds.Merge(t1.VerticalClip, t2.VerticalClip),
										   })
					);

			foreach (var graph in graphs)
			{
				var trans = cornerProjection[ChooseProjection(graph)];
				Rect bounds = new Rect(new Point(trans.HorizontalClip.Start, trans.VerticalClip.Start), new Point(trans.HorizontalClip.End, trans.VerticalClip.End));
				graph.PlotVisualizer.SetTransform(trans.Transform, bounds);
			}
			foreach (var axis in Axes)
				axis.SetGridLineExtent(RenderSize);
			if (needRedrawGraphs) RedrawGraphs(relevantAxes);
			//	drawingContext.DrawDrawing(dg);
			base.OnRender(drawingContext);
		}
	}
}
