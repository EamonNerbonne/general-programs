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

namespace EmnExtensions.Wpf
{
    /// <summary>
    /// Interaction logic for PlotControl.xaml
    /// </summary>
    public partial class PlotControl : UserControl
    {
        public PlotControl() {
            graphLookup.Add("", null);
            InitializeComponent();
            botSelect.ItemsSource = graphs;
            topSelect.ItemsSource = graphs;
            visibilityMenu.ItemsSource = graphs;
            //            graphs.Add(null);
            //NewGraph("unlabelled", new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(1, 0), new Point(0, 0) });
        }

        ObservableCollection<GraphControl> graphs = new ObservableCollection<GraphControl>();
        Dictionary<string, GraphControl> graphLookup = new Dictionary<string, GraphControl>();

        //  public GraphControl GraphControl { get { return graphLookup.Select(kv => kv.Value).FirstOrDefault(); } }

        bool nextIsTopRight;

        public GraphControl NewGraph(string name, IEnumerable<Point> line) {
            GraphControl graph = new GraphControl();
            graph.Name = name;
            graphLookup[name] = graph;
            graphs.Add(graph);
            graphGrid.Children.Add(graph);
            graph.NewLine(line);
            return graph;
        }

        public void Remove(string graphName) { Remove(graphLookup[graphName]); }
        public void Remove(GraphControl graph) {
            if (graph == null)
                throw new ArgumentNullException("graph");
            graphGrid.Children.Remove(graph);
            graphLookup.Remove(graph.Name);
            graphs.Remove(graph);
            foreach (var legend in new[] { leftLegend, lowerLegend, upperLegend, rightLegend }) {
                if (legend.Watch == graph)
                    legend.Watch = null;
            }
        }


        /// <summary>
        /// You must pass a read/write newly created stream!
        /// </summary>
        public void Print(GraphControl graph,Stream s) {
            bool wasContained=graphGrid.Children.Contains(graph);
            var toPrint = new SimpleGraph();

            try {
                if (wasContained)
                    graphGrid.Children.Remove(graph);
                toPrint.Graph = graph;
                WpfTools.PrintXPS(toPrint, 400, 400, s, FileMode.Create, FileAccess.ReadWrite);
            } finally {
                toPrint.Graph = null;
                if (wasContained)
                    graphGrid.Children.Add(graph);
            }
        }

        public bool TryGetGraph(string name, out GraphControl graph) { return graphLookup.TryGetValue(name, out graph); }
        public IEnumerable<GraphControl> Graphs { get { return graphLookup.Values; } }

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
            if (graph.Visibility == Visibility.Visible) {
                graph.Visibility = Visibility.Hidden;
            } else {
                graph.Visibility = Visibility.Visible;
            }
            if (upperLegend.Watch != null && upperLegend.Watch.Visibility != Visibility.Visible) {
                upperLegend.Watch = null;
                rightLegend.Watch = null;
            }
            if (lowerLegend.Watch != null && lowerLegend.Watch.Visibility != Visibility.Visible) {
                leftLegend.Watch = null;
                lowerLegend.Watch = null;
            }
        }


        private void botSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            GraphControl graph = (GraphControl)botSelect.SelectedItem;

            if (graph != null && graph.Visibility != Visibility.Visible)
                graph.Visibility = Visibility.Visible;

            GraphControl oldGraph = lowerLegend.Watch;
            if (oldGraph != null && oldGraph.Visibility == Visibility.Visible)
                oldGraph.Visibility = Visibility.Collapsed;

            lowerLegend.Watch = graph;
            leftLegend.Watch = graph;

        }

        private void topSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            GraphControl graph = (GraphControl)topSelect.SelectedItem;
            if (graph != null && graph.Visibility != Visibility.Visible)
                graph.Visibility = Visibility.Visible;

            GraphControl oldGraph = upperLegend.Watch;
            if (oldGraph != null && oldGraph.Visibility == Visibility.Visible)
                oldGraph.Visibility = Visibility.Collapsed;
            upperLegend.Watch = graph;
            rightLegend.Watch = graph;
        }

    }
}
