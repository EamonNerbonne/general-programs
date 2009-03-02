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
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using System.IO.Packaging;
using System.IO;
using System.Collections.Specialized;
using Microsoft.Win32;

namespace EmnExtensions.Wpf
{
	/// <summary>
	/// Interaction logic for PlotControl.xaml
	/// </summary>
	public partial class PlotControl : UserControl
	{
		public PlotControl() {
			InitializeComponent();

			botSelect.ItemsSource = graphs;

			topSelect.ItemsSource = graphs;
			visibilityMenu.ItemsSource = graphs;
			graphs.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(graphs_CollectionChanged);
			//NewGraph("unlabelled", new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(1, 0), new Point(0, 0) });
		}

		protected override void OnInitialized(EventArgs e) {
			graphs.Add(null);
			base.OnInitialized(e);
		}

		void graphs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					foreach (GraphControl graph in e.NewItems) {
						graphLookup[graph.Name] = graph;
						graphGrid.Children.Add(graph);
					}
					break;
				case NotifyCollectionChangedAction.Move: break;
				case NotifyCollectionChangedAction.Remove:
					foreach (GraphControl graph in e.OldItems) {
						graphGrid.Children.Remove(graph);
						graphLookup.Remove(graph.Name);
						foreach (var legend in new[] { leftLegend, lowerLegend, upperLegend, rightLegend }) {
							if (legend.Watch == graph)
								legend.Watch = null;
						}
					}
					break;
				case NotifyCollectionChangedAction.Replace:
					foreach (GraphControl graph in e.OldItems) {
						graphGrid.Children.Remove(graph);
						graphLookup.Remove(graph.Name);
						foreach (var legend in new[] { leftLegend, lowerLegend, upperLegend, rightLegend }) {
							if (legend.Watch == graph)
								legend.Watch = null;
						}
					}
					foreach (GraphControl graph in e.NewItems) {
						graphLookup[graph.Name] = graph;
						graphGrid.Children.Add(graph);
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					graphGrid.Children.Clear();
					graphLookup.Clear();
					foreach (GraphControl graph in graphs) {
						graphLookup[graph.Name] = graph;
						graphGrid.Children.Add(graph);
					}
					break;
			}
		}

		ObservableCollection<GraphControl> graphs = new ObservableCollection<GraphControl>();
		Dictionary<string, GraphControl> graphLookup = new Dictionary<string, GraphControl>();

		//  public GraphControl GraphControl { get { return graphLookup.Select(kv => kv.Value).FirstOrDefault(); } }

		bool nextIsTopRight;

		public ObservableCollection<GraphControl> Graphs { get { return graphs; } }

		public GraphControl NewGraph(string name, IEnumerable<Point> line) {
			GraphControl graph = new GraphGeometryControl {
				Visibility = Visibility.Hidden,
				Name = name,
				GraphGeometry = GraphUtils.Line(line.ToArray())
			};
			graphs.Add(graph);
			return graph;
		}

		public void Remove(string graphName) { Remove(graphLookup[graphName]); }
		public void Remove(GraphControl graph) {
			if (graph == null)
				throw new ArgumentNullException("graph");
			graphs.Remove(graph);
		}

		public Grid GraphWithLegend { get { return graphWithLegend; } }

		/// <summary>
		/// You must pass a read/write newly created stream!
		/// </summary>
		public void Print(GraphControl graph, Stream s) {
			bool wasContained = graphGrid.Children.Contains(graph);
			var toPrint = new SimpleGraph();

			try {
				if (wasContained)
					graphGrid.Children.Remove(graph);
				toPrint.Graph = graph;
				var oldVis = graph.Visibility;
				graph.Visibility = Visibility.Visible;
				WpfTools.PrintXPS(toPrint, 500, 500,1.0, s, FileMode.Create, FileAccess.ReadWrite);
				graph.Visibility = oldVis;
			} finally {
				toPrint.Graph = null;
				if (wasContained)
					graphGrid.Children.Add(graph);
			}
		}

		public void PrintThis() {
			var saveDialog = new SaveFileDialog() {
				Title = "Save Graph As ...",
				Filter = "XPS file|*.xps",
				FileName = (leftLegend.Watch == null ? "graph" : leftLegend.Watch.Name) + ".xps",
			};
			if (saveDialog.ShowDialog() == true) {
				using (var writestream = new FileStream(saveDialog.FileName, FileMode.Create, FileAccess.ReadWrite))
					WpfTools.PrintXPS(graphWithLegend, 500, 500, 1.0,writestream, FileMode.Create, FileAccess.ReadWrite);
			}
		}

		public bool TryGetGraph(string name, out GraphControl graph) { return graphLookup.TryGetValue(name, out graph); }

		public void ShowGraph(string graphname) { ShowGraph(graphLookup[graphname]); }
		public void ShowGraph(GraphControl graph) { ShowGraph(graph, nextIsTopRight); }
		public void ShowGraph(string graphname, bool legendIsTopRight) { ShowGraph(graphLookup[graphname], legendIsTopRight); }
		public void ShowGraph(GraphControl graph, bool legendIsTopRight) {
			if (legendIsTopRight) {
				topSelect.SelectedItem = graph;
			} else {
				botSelect.SelectedItem = graph;
			}
			nextIsTopRight = !legendIsTopRight;
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e) {
			MenuItem item = (MenuItem)sender;
			GraphControl graph = (GraphControl)item.DataContext;
			if (graph == null) return;
			if (graph.Visibility == Visibility.Visible) {
				graph.Visibility = Visibility.Hidden;
				if (upperLegend.Watch != null && upperLegend.Watch.Visibility != Visibility.Visible) {
					upperLegend.Watch = null;
					rightLegend.Watch = null;
				}
				if (lowerLegend.Watch != null && lowerLegend.Watch.Visibility != Visibility.Visible) {
					leftLegend.Watch = null;
					lowerLegend.Watch = null;
				}
			} else {
				graph.Visibility = Visibility.Visible;
				//ShowGraph(graph);
			}
		}

		private void botSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {

			GraphControl graph = (GraphControl)botSelect.SelectedItem;

			if (graph != null && graph.Visibility != Visibility.Visible)
				graph.Visibility = Visibility.Visible;

			GraphControl oldGraph = lowerLegend.Watch;
			if (oldGraph != null && oldGraph.Visibility == Visibility.Visible && oldGraph != topSelect.SelectedItem)
				oldGraph.Visibility = Visibility.Collapsed;

			lowerLegend.Watch = graph;
			leftLegend.Watch = graph;


			if (graph is Graph2DControl) {
				foreach (var othergraph in Graphs)
					if (othergraph is Graph2DControl && othergraph != graph)
						othergraph.Visibility = Visibility.Collapsed;
				var g = graph as Graph2DControl;
				colormapLegend.TickedLegendControl.StartVal = g.RealMin;
				colormapLegend.TickedLegendControl.EndVal = g.RealMax;
				colormapLegend.TickedLegendControl.LegendLabel = g.ColorLabel;
				colormapLegend.DynamicBitmap.BitmapGenerator = (w, h) => {
					uint[] map = new uint[w * h];
					int i = 0;
					for (int y = 0; y < h; y++)
						for (int x = 0; x < w; x++)
							map[i++] = g.Colormap(x / (double)(w - 1)).ToNativeColor();
					return map;
				};
				colormapLegend.Visibility = Visibility.Visible;
			} else
				colormapLegend.Visibility = Visibility.Collapsed;
		}

		private void topSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			GraphControl graph = (GraphControl)topSelect.SelectedItem;
			if (graph != null && graph.Visibility != Visibility.Visible)
				graph.Visibility = Visibility.Visible;

			GraphControl oldGraph = upperLegend.Watch;
			if (oldGraph != null && oldGraph.Visibility == Visibility.Visible && oldGraph != botSelect.SelectedItem)
				oldGraph.Visibility = Visibility.Collapsed;
			upperLegend.Watch = graph;
			rightLegend.Watch = graph;
		}

		private void topSelect_Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			TextBlock target = sender as TextBlock;
			if (target.DataContext == null)
				topSelect.SelectedItem = null;
		}

		private void botSelect_Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			TextBlock target = sender as TextBlock;
			if (target.DataContext == null)
				botSelect.SelectedItem = null;
		}
	}

	public class DealWithNullConverter : IValueConverter
	{
		public DealWithNullConverter() {
			Console.WriteLine("Started converter");
		}
		const string NotSet = "<unset>";
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			GraphControl graph = value as GraphControl;
			if (graph == null)
				return NotSet;
			else
				return graph.Name;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			return DependencyProperty.UnsetValue;
		}

	}


}
