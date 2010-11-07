// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//see http://msdn.microsoft.com/en-us/library/system.windows.media.imaging.writeablebitmap.aspx
namespace EmnExtensions.Wpf
{
	public class DynamicBitmap : FrameworkElement
	{

		//Dependency Property "BitmapGenerator":==========================

		public static readonly DependencyProperty BitmapGeneratorProperty =
 DependencyProperty.Register("BitmapGenerator", typeof(Func<int, int, uint[]>), typeof(DynamicBitmap),
 new FrameworkPropertyMetadata(null,
	 FrameworkPropertyMetadataOptions.AffectsRender,
	 BitmapGeneratorSet)
 );

		private static void BitmapGeneratorSet(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			((DynamicBitmap)d).InvalidateVisual();
		}
		public Func<int, int, uint[]> BitmapGenerator {
			get { return (Func<int, int, uint[]>)GetValue(BitmapGeneratorProperty); }
			set { SetValue(BitmapGeneratorProperty, value); }
		}

		static uint[] lastAutoGen;
		private static uint[] DefaultBitmapGenerator(int width, int height) {
			if (lastAutoGen == null || lastAutoGen.Length < width * height)
				lastAutoGen = new uint[width * height];
			return lastAutoGen;
		}

		//class implementation: ==========================================
		WriteableBitmap bitmap;

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
			base.OnRenderSizeChanged(sizeInfo);
			bitmap = null;
			InvalidateVisual();
		}
		const int maxWidthHeight = 4096;
		void MakeBitmap() {
			bitmap = new WriteableBitmap(
				Math.Min((int)Math.Ceiling(ActualWidth), maxWidthHeight),
				Math.Min((int)Math.Ceiling(ActualHeight), maxWidthHeight), 
				96, 96, PixelFormats.Bgr32, null);
		}
		void UpdateBitmap() {
			Func<int, int, uint[]> bmpGen = BitmapGenerator ?? DefaultBitmapGenerator;
			bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), bmpGen(bitmap.PixelWidth, bitmap.PixelHeight), bitmap.PixelWidth * 4, 0);
		}
		protected override void OnRender(DrawingContext drawingContext) {
			double shortSide = Math.Min(ActualHeight,ActualWidth);

			if (shortSide <= 0 || !shortSide.IsFinite()) return;
			if (bitmap == null) MakeBitmap();
			UpdateBitmap();

			drawingContext.DrawImage(bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
		}
	}

}