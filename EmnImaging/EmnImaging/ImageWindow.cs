using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
namespace EmnImaging {
    public class ImageWindow:Window {
        public ImageWindow(PixelArgb32[,] image) : this(image, null) { }
        public ImageWindow(PixelArgb32[,] image,string title) {
            Title = title ?? "Image Window";
            var control= new ImageControl(image);
            Viewbox viewbox = new Viewbox() {
                Child = control,
                Stretch = Stretch.Uniform
            };
            
            if (image.Width() > image.Height()) {
                
                Width = 700.0 + BorderThickness.Left +BorderThickness.Right;
                Height = 700.0 / image.Width() * image.Height() + BorderThickness.Top+BorderThickness.Bottom;
                
            } else {
                Height = 700.0 + BorderThickness.Left + BorderThickness.Right;
                Width = 700.0 / image.Height() * image.Width() + BorderThickness.Top + BorderThickness.Bottom;
            }
            Content = viewbox;
        }
        
    }
}
