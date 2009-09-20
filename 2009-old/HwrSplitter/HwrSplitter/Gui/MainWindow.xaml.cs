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
using System.Windows.Shapes;
using EmnExtensions.Wpf;

namespace HwrSplitter.Gui
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		MainManager manager;
		public MainWindow() {
			this.WindowState = WindowState.Maximized;
			InitializeComponent();

			zoomRect.ToZoom = imgViewbox.ImageCanvas;
			wordDetail.ToZoom = imgViewbox.ImageCanvas;
			manager = new MainManager(this);
		}

		public MainManager Manager { get { return manager; } }
		public ImageAnnotViewbox ImageAnnotViewbox { get { return imgViewbox; } }
		public LogControl LogControl { get { return logControl; } }
		public ZoomRect ZoomRect { get { return zoomRect; } }
		public WordDetail WordDetail { get { return wordDetail; } }

		private void Window_Loaded(object sender, RoutedEventArgs e) { }

		private void pauseBox_Checked(object sender, RoutedEventArgs e) { manager.Paused = true; }

		private void pauseBox_Unchecked(object sender, RoutedEventArgs e) { manager.Paused = false; }
	}
}
