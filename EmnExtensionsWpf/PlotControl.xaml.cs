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
            botSelect.ItemsSource = graphLookup.Keys;
            topSelect.ItemsSource = graphLookup.Keys;
            NewGraph("unlabelled", new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(1, 0), new Point(0, 0) });
        }


        Dictionary<string, GraphControl> graphLookup = new Dictionary<string, GraphControl>();

      //  public GraphControl GraphControl { get { return graphLookup.Select(kv => kv.Value).FirstOrDefault(); } }

        bool nextIsTopRight;

        public GraphControl NewGraph(string name, IEnumerable<Point> line) {
            GraphControl graph = new GraphControl();
            graph.Name = name;
            graphLookup[name] = graph;
            botSelect.ItemsSource = graphLookup.Keys;
            topSelect.ItemsSource = graphLookup.Keys;
            visibilitySelect.ItemsSource = graphLookup.Keys.Where(s=>s!="");
            graphGrid.Children.Add(graph);
            graph.NewLine(line);
            return graph;
        }

        public void Remove(string graphName) { Remove(graphLookup[graphName]); }
        public void Remove(GraphControl graph) {
            graphGrid.Children.Remove(graph);
            graphLookup.Remove(graph.Name);
            botSelect.ItemsSource = graphLookup.Keys;
            topSelect.ItemsSource = graphLookup.Keys;
            visibilitySelect.ItemsSource = graphLookup.Keys.Where(s => s != "");
            foreach (var legend in new[] { leftLegend, lowerLegend, upperLegend, rightLegend }) {
                if (legend.Watch == graph)
                    legend.Watch = null;
            }
        }

        public bool TryGetGraph(string name, out GraphControl graph) { return graphLookup.TryGetValue(name, out graph); }
        public IEnumerable<GraphControl> Graphs { get { return graphLookup.Values; } }

        public void ShowGraph(string graphname) { ShowGraph(graphLookup[graphname]); }
        public void ShowGraph(GraphControl graph) { ShowGraph(graph, nextIsTopRight); }
        public void ShowGraph(string graphname, bool legendIsTopRight) { ShowGraph(graphLookup[graphname],legendIsTopRight); }
        public void ShowGraph(GraphControl graph, bool legendIsTopRight) {
            if (legendIsTopRight) {
                upperLegend.Watch = graph;
                rightLegend.Watch = graph;
            } else {
                leftLegend.Watch = graph;
                lowerLegend.Watch = graph;
            }

            if (graph!=null && graph.Visibility != Visibility.Visible)
                graph.Visibility = Visibility.Visible;
            nextIsTopRight = !legendIsTopRight;
        }

        private void botSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string selItem =null;
            foreach(string key in e.AddedItems.OfType<string>().Take(1))
                selItem=key;
            ShowGraph(selItem,false);
        }

        private void topSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string selItem = null;
            foreach (string key in e.AddedItems.OfType<string>().Take(1))
                selItem = key;
            ShowGraph(selItem,true);

        }

        private void visibility_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string selItem = null;
            foreach (string key in e.AddedItems.OfType<string>().Take(1))
                selItem = key;
            GraphControl graph = graphLookup[selItem];
            if (graph.Visibility == Visibility.Visible) {
                graph.Visibility = Visibility.Hidden;
            } else {
                graph.Visibility = Visibility.Visible;
            }
            if (upperLegend.Watch!=null && upperLegend.Watch.Visibility != Visibility.Visible) {
                upperLegend.Watch = null;
                rightLegend.Watch = null;
            }
            if (lowerLegend.Watch!=null && lowerLegend.Watch.Visibility != Visibility.Visible) {
                leftLegend.Watch = null;
                lowerLegend.Watch = null;
            }

        }

    }
}
