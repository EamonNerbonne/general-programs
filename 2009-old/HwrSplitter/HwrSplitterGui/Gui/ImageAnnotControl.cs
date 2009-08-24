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
using HwrDataModel;
using System.Windows.Threading;
using HwrSplitter.Engine;
namespace HwrSplitter.Gui
{
    public class ImageAnnotViewbox : Viewbox {
        Canvas canvas;
        
        public ImageAnnotViewbox() {
            canvas = new Canvas();
            //image = new Image();
            //System.Windows.Controls.
            Child = canvas;
            Stretch = Stretch.Uniform;
        }


		public void SetImage(HwrPageImage image) {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
				new Action<HwrPageImage>(SetImageUI), image);
        }

		void SetImageUI(HwrPageImage image) {
            ImageBrush brush = new ImageBrush {
                TileMode = TileMode.None,
                Stretch = Stretch.Fill,

                ImageSource = image.BitmapSource
            };
            brush.Freeze();
            canvas.Background = brush;
            canvas.Width = image.Width;
            canvas.Height = image.Height;
        }

        public Canvas ImageCanvas { get { return canvas; } }
    }
}
