using System.Windows.Controls;

namespace EmnExtensions.Wpf.OldGraph
{
    /// <summary>
    /// Interaction logic for ColorMapLegend.xaml
    /// </summary>
    public partial class ColorMapLegend : UserControl
    {
        public ColorMapLegend()
        {
            InitializeComponent();
        }

        public TickedLegendControl TickedLegendControl { get { return colorLegend; } }
        public DynamicBitmap DynamicBitmap { get { return colormapControl; } }
    }
}
