//#define USE_PAGED_XPS_SAVE

using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows;
using System.Windows.Xps.Packaging;
using EmnExtensions.Wpf.Plot;

namespace EmnExtensions.Wpf.WpfTools
{
    public static partial class WpfTools
    {
        /// <summary>
        /// Note that Xps actually reads the stream too, so the file cannot already exist and simply be overwritten
        /// (unless it's a valid xps), and it in particular may not have 0 bytes unless you open it in FileMode.Create.
        /// </summary>
        /// <param name="el">The element to print to xps</param>
        /// <param name="reqWidth">The requested width.  Don't expect miracles.</param>
        /// <param name="reqHeight">The requested height.  Don't expect miracles.</param>
        /// <param name="toStream">the read/write stream to which to store the printout to.</param>
        /// <param name="fileMode">the provided stream's FileMode</param>
        /// <param name="fileAccess">The provided stream's FileAccess</param>
        public static void PrintXPS(FrameworkElement el, double reqWidth, double reqHeight, Stream toStream, FileMode fileMode, FileAccess fileAccess)
        {
            //MemoryStream ms = new MemoryStream();
            //  using (var stream = File.Open(@"C:\test.xps",FileMode.,FileAccess.ReadWrite))
            var oldWidth = el.Width;
            var oldHeight = el.Height;
#if USELAYOUTTRANSFORM
            Transform oldLayout = el.LayoutTransform;
            Transform oldRender = el.RenderTransform;
            double curWidth = el.DesiredSize.Width;
            double curHeight = el.DesiredSize.Height;
            double renderWidth = el.ActualWidth;
            double renderHeight = el.ActualHeight;
#endif

            try {
#if USE_PAGED_XPS_SAVE
                VisualBrush brush = new VisualBrush(el);
#endif
                //el.Width = reqWidth;
                //el.Height = reqHeight;
                el.InvalidateVisual();

                //now IF we already have a size, we'll make a layout-transform that maps the requested size to the current
                //size.  This way, if there's something forcing it to the current size, the element must choose to take the
                //requested size in order to maintain its size.
                //Doing this fixes bugs in saving complex grid layouts that probably are doing some layout calcs outside of
                //UpdateLayout but aren't influenced by our .Measure and .Arrange calls (which is nasty, but seems to be
                //a real issue).
#if USELAYOUTTRANSFORM
                if (curHeight.IsFinite() && curWidth.IsFinite() && curHeight > 0 && curWidth > 0) {
                    el.LayoutTransform = new ScaleTransform(renderWidth / reqWidth, renderHeight / reqHeight);
                    el.RenderTransform = new ScaleTransform(reqWidth / renderWidth, reqHeight / renderHeight);
                    el.UpdateLayout();
                    el.Measure(new Size(curWidth, curHeight));
                    el.Arrange(new Rect(el.DesiredSize));
                } else
#endif
                {
                    el.Width = reqWidth;
                    el.Height = reqHeight;
                    //el.LayoutTransform = Transform.Identity;
                    //el.RenderTransform = Transform.Identity;
                    el.UpdateLayout();
                    var margin_compensating = new Size(reqWidth + DimensionMargins.FromThicknessX(el.Margin).Sum, reqHeight + DimensionMargins.FromThicknessY(el.Margin).Sum);
                    el.Measure(margin_compensating);
                    el.Arrange(new(margin_compensating));
                }

#if USE_PAGED_XPS_SAVE
                FixedPage page = new FixedPage();
                page.Width = el.ActualWidth*scaleFactor;
                page.Height = el.ActualHeight*scaleFactor;
                page.Background = brush;
#endif

                using (var packInto = Package.Open(toStream, fileMode, fileAccess))
                using (var doc = new XpsDocument(packInto)) {
                    var writer = XpsDocument.CreateXpsDocumentWriter(doc);
#if USE_PAGED_XPS_SAVE
                    writer.Write(page);
#else
                    writer.Write(el, new() { OutputQuality = OutputQuality.High });
#endif
                }
            } finally {
                el.Width = oldWidth;
                el.Height = oldHeight;
#if USELAYOUTTRANSFORM
                el.LayoutTransform = oldLayout;
                el.RenderTransform = oldRender;
#endif
                el.InvalidateVisual();
                // this item may be confused about it's position within the parent.  The following is probably imperfect, but
                //a reasonable attempt to ensure the item is really relayouted.
                if (el.Parent is UIElement element) {
                    element.InvalidateArrange();
                    element.UpdateLayout();
                } else {
                    el.UpdateLayout();
                }
            }
        }
    }
}
