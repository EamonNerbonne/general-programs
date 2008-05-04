using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace EmnImaging {
    class ImageControl :Image{
        public ImageControl(PixelArgb32[,] image) {
            Source = image.AsBitmapSource();
            Width = image.Width();
            Height = image.Height();
            
        }
    }
}
