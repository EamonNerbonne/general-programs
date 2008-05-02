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
using EmnImaging;

namespace EmnImageTestDisplay {
    /// <summary>
    /// Interaction logic for ImageViewerWindow.xaml
    /// </summary>
    public partial class ImageViewerWindow : Window {
        public ImageViewerWindow() {            InitializeComponent();       }


        public PixelArgb32[,] ImageToShow {
            set {
                Content = new Image {
                    Source = value.AsBitmapSource()
                };
            }
        }

    }
}
