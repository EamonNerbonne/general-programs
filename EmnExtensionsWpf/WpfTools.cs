using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.IO.Packaging;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Xps;
using System.Windows;
using System.IO;

namespace EmnExtensions.Wpf
{
    public class WpfTools
    {

        /// <summary>
        /// Note that Xps actually reads the stream too, so the file cannot already exist and simply be overwritten
        /// (unless it's a valid xps), and it in particular may not have 0 bytes unless you open it in FileMode.Create.
        /// </summary>
        /// <param name="el">The element to print to xps</param>
        /// <param name="reqWidth">The requested width.  Don't expect miracles.</param>
        /// <param name="reqHeight">The requested height.  Don't expect miracles.</param>
        /// <param name="toStream"></param>
        public static void PrintXPS(FrameworkElement el, double reqWidth, double reqHeight, Stream toStream, FileMode fileMode, FileAccess fileAccess) {
            //MemoryStream ms = new MemoryStream();
            //  using (var stream = File.Open(@"C:\test.xps",FileMode.,FileAccess.ReadWrite)) 
            Transform oldLayout = el.LayoutTransform;
            double oldWidth = el.Width;
            double oldHeight = el.Height;

            try {
                VisualBrush brush = new VisualBrush(el);

                el.LayoutTransform = Transform.Identity;
                el.Width = reqWidth;
                el.Height = reqHeight;
                el.UpdateLayout();

                var rect = new Rect(0, 0, el.ActualWidth, el.ActualHeight);
                FixedPage page = new FixedPage();
                page.Width = rect.Width;
                page.Height = rect.Height;
                page.Background = brush;
                using (Package packInto = Package.Open(toStream, fileMode, fileAccess))
                using (XpsDocument doc = new XpsDocument(packInto)) {
                    //doc.CoreDocumentProperties.
                    XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc);
                    writer.Write(page);
                }
            } finally {
                el.Width = oldWidth;
                el.Height = oldHeight;
                el.LayoutTransform = oldLayout;
                el.UpdateLayout();
            }
        }

    }
}
