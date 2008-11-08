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
            InitializeComponent();
        }

        public GraphControl GraphControl { get { return graph; } }

        public void Plot(IEnumerable<Point> line) {
            graph.ShowLine(line);
            Rect range = graph.GraphBounds;
            
            lowerLegend.StartVal = range.X;
            lowerLegend.EndVal = range.X + range.Width;
            leftLegend.StartVal = range.Y;
            leftLegend.EndVal = range.Y + range.Height;
        }

    }
}
