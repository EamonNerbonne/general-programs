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
    /// Interaction logic for SimpleGraph.xaml
    /// </summary>
    public partial class SimpleGraph : UserControl
    {
        GraphControl kid;
        public GraphControl Graph {
            get {
                return kid;
                
            }
            set {
                if (graphGrid.Children.Contains(kid))
                    graphGrid.Children.Remove(kid);
                kid = value;
                if(kid!=null)
                graphGrid.Children.Add(kid);
                lowerLegend.Watch = kid;
                leftLegend.Watch = kid;
                upperLegend.Watch = kid;
                rightLegend.Watch = kid;
            }
        }

        public SimpleGraph() {
            InitializeComponent();
        }
    }
}
