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
namespace EmnImaging {
    public class ImageWindow:Window {
        Canvas canvas;
        public ImageWindow() {
            Title = "Image Window";
            canvas = new Canvas();

            Viewbox viewbox = new Viewbox() {
                Child = canvas,
                Stretch = Stretch.Uniform
            };
            
            Content = viewbox;
        }

        public void SetImage(PixelArgb32[,] image) {
            ImageBrush brush = new ImageBrush {
                 TileMode = TileMode.None,
                  Stretch = Stretch.None,
                   ImageSource = image.AsBitmapSource()
            };

            canvas.Background = brush;
            canvas.Width = image.Width();
            canvas.Height = image.Height();
            
            if (image.Width() > image.Height()) {
                Width = 700.0 + BorderThickness.Left + BorderThickness.Right;
                Height = 700.0 / image.Width() * image.Height() + BorderThickness.Top + BorderThickness.Bottom;

            } else {
                Height = 700.0 + BorderThickness.Left + BorderThickness.Right;
                Width = 700.0 / image.Height() * image.Width() + BorderThickness.Top + BorderThickness.Bottom;
            }
        }

        public void AddShapes(IEnumerable<UIElement> shapes) {
            foreach (Shape shape in shapes)
                canvas.Children.Add(shape);
        }
    }
}
