using System.Windows.Controls;

namespace EmnExtensions.Wpf.OldGraph
{
    /// <summary>
    /// Interaction logic for ColorMapLegend.xaml
    /// </summary>
    public partial class ColorMapLegend
    {
        public ColorMapLegend() => InitializeComponent();

        public TickedLegendControl TickedLegendControl => colorLegend;
        public DynamicBitmap DynamicBitmap => colormapControl;
    }
}
