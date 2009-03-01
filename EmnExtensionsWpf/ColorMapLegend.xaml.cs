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
	/// Interaction logic for ColorMapLegend.xaml
	/// </summary>
	public partial class ColorMapLegend : UserControl
	{
		public ColorMapLegend() {
			InitializeComponent();
		}

		public TickedLegendControl TickedLegendControl { get { return colorLegend; } }
		public DynamicBitmap DynamicBitmap { get { return colormapControl; } }
	}
}
