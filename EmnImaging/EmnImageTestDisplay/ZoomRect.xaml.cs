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

namespace EmnImageTestDisplay {
    /// <summary>
    /// Interaction logic for ZoomRect.xaml
    /// </summary>
    public partial class ZoomRect : UserControl {
        UIElement toZoom;
        VisualBrush zoomViewBrush;
        public ZoomRect() {
            InitializeComponent();
        }

        public UIElement ToZoom {
            get { return toZoom; }
            set {
                if (toZoom != null)
                    throw new ArgumentException("ToZoom already set!");
                toZoom = value;
                toZoom.PreviewMouseMove += new MouseEventHandler(elementMouseMove);
                zoomViewBrush = (VisualBrush)zoomRect.Fill;
                zoomViewBrush.Visual = toZoom;
            }
        }

        void elementMouseMove(object sender, MouseEventArgs e) {
            ShowNewPoint(e.MouseDevice.GetPosition(toZoom));
        }
        void onSizeChanged(object sender, SizeChangedEventArgs e) {
            ShowNewPoint(lastKnownPoint);
        }
        Point lastKnownPoint;
        public void ShowNewPoint(Point newPoint) {
            lastKnownPoint = newPoint;
            if (toZoom!=null)
                zoomViewBrush.Viewbox = new Rect(newPoint.X - zoomRect.ActualWidth / 2, newPoint.Y - zoomRect.ActualHeight / 2, zoomRect.ActualWidth, zoomRect.ActualHeight);
        }

    }
}
