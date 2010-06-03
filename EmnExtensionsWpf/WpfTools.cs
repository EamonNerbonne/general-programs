//#define USE_PAGED_XPS_SAVE
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
using System.Printing;

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
		public static void PrintXPS(FrameworkElement el, double reqWidth, double reqHeight, double scaleFactor, Stream toStream, FileMode fileMode, FileAccess fileAccess) {
			//MemoryStream ms = new MemoryStream();
			//  using (var stream = File.Open(@"C:\test.xps",FileMode.,FileAccess.ReadWrite)) 
			Transform oldLayout = el.LayoutTransform;
			double oldWidth = el.Width;
			double oldHeight = el.Height;
			double curWidth =el.DesiredSize.Width;
			double curHeight = el.DesiredSize.Height;

			try {
#if USE_PAGED_XPS_SAVE
				VisualBrush brush = new VisualBrush(el);
#endif
				el.Width = reqWidth;
				el.Height = reqHeight;

				//now IF we already have a size, we'll make a layout-transform that maps the requested size to the current 
				//size.  This way, if there's something forcing it to the current size, the element must choose to take the 
				//requested size in order to maintain its size.
				//Doing this fixes bugs in saving complex grid layouts that probably are doing some layout calcs outside of
				//in UpdateLayout  but aren't influenced by our .Measure and .Arrange calls (which is nasty, but seems to be
				//a real issue).
				if ( curHeight.IsFinite() && curWidth.IsFinite() && curHeight > 0 && curWidth > 0) {
					el.LayoutTransform = new ScaleTransform(curWidth / reqWidth, curHeight / reqHeight);
					el.Measure(new Size(curWidth, curHeight));
				} else {
					el.LayoutTransform = Transform.Identity;
					el.Measure(new Size(reqWidth, reqHeight));
				}

				el.Arrange(new Rect(el.DesiredSize));
				el.InvalidateVisual();
				el.UpdateLayout();
				

#if USE_PAGED_XPS_SAVE
				FixedPage page = new FixedPage();
				page.Width = el.ActualWidth*scaleFactor;
				page.Height = el.ActualHeight*scaleFactor;
				page.Background = brush;
#endif

				using (Package packInto = Package.Open(toStream, fileMode, fileAccess))
				using (XpsDocument doc = new XpsDocument(packInto)) {

					XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc);
					
					
#if USE_PAGED_XPS_SAVE
					writer.Write(page);
#else
					writer.Write(el, new PrintTicket { OutputQuality = OutputQuality.High });
#endif
				}
			} finally {
				el.Width = oldWidth;
				el.Height = oldHeight;
				el.LayoutTransform = oldLayout;
				el.InvalidateArrange();
				// this item may be confused about it's position within the parent.  The following is probably imperfect, but
				//a reasonable attempt to ensure the item is really relayouted.
				if (el.Parent is UIElement)
					((UIElement)el.Parent).InvalidateArrange(); 
			}
		}
	}
}
