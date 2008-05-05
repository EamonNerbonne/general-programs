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

namespace EmnImageTestDisplay {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        public ImageAnnotViewbox ImageAnnotViewbox { get { return imgViewbox; } }
        public LogControl LogControl { get { return logControl; } }
        public ZoomRect ZoomRect { get { return zoomRect; } }
        public WordDetail WordDetail { get { return wordDetail; } }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            
        }
    }
}
