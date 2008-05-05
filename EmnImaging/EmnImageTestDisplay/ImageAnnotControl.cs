using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using EmnImaging;
namespace EmnImageTestDisplay {
    public class ImageAnnotViewbox : Viewbox {
        Canvas canvas;
        public ImageAnnotViewbox() {
            canvas = new Canvas();
            Child = canvas;
            Stretch = Stretch.Uniform;
        }

        public void SetImage(float[,] image) {
            ImageBrush brush = new ImageBrush {
                TileMode = TileMode.None,
                Stretch = Stretch.None,
                ImageSource = image.AsBitmapSource()
            };

            canvas.Background = brush;
            canvas.Width = image.Width();
            canvas.Height = image.Height();

        }

        public void AddShapes(IEnumerable<UIElement> shapes) {
            foreach (Shape shape in shapes)
                canvas.Children.Add(shape);
        }
        public Canvas ImageCanvas { get { return canvas; } }
    }
}
