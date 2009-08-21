//#define VIACPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using HwrLibCliWrapper;
using EmnExtensions.DebugTools;

namespace DataIO
{
	public class HwrPageImage
	{
		ImageStruct<sbyte> image;
		//BitmapImage original;
		public HwrPageImage(FileInfo fileToLoad) {
			NiceTimer timer = new NiceTimer();
			timer.TimeMark("Loading");
			BitmapImage 
				original = new BitmapImage();
			original.BeginInit();
			original.UriSource = new Uri(fileToLoad.FullName);
			original.EndInit();
			 Console.WriteLine(original.IsDownloading);
			original.Freeze();
			
			timer.TimeMark("Converting to array");
			var rawImg = new ImageStruct<uint>(original.PixelWidth, original.PixelHeight);
			Console.WriteLine("{0:X}", rawImg[1000, 2000]);
			uint[] data = new uint[original.PixelWidth * original.PixelHeight];
			original.CopyPixels(data, original.PixelWidth*4, 0);
			original.CopyPixels(rawImg.RawData, rawImg.Stride, 0);
			Console.WriteLine("{0:X}", rawImg[1000, 2000]);
#if VIACPP
			timer.TimeMark("preprocessing");
			image = ImageProcessor.preprocess(rawImg);
#else
			timer.TimeMark("threshholding");
			image = rawImg.MapTo(nr => ((PixelArgb32)nr).R >= 200 ? (sbyte)0 : (sbyte)1);
			/*Width = original.PixelWidth;
			Height = original.PixelHeight;
			image = new float[Width * Height];
			for (int pxOffset = 0; pxOffset < data.Length; pxOffset++)
				image[pxOffset] = ((PixelArgb32)data[pxOffset]).R >= 200 ? 1.0f : 0.0f;*/
#endif
			timer.TimeMark(null);
		}

		public int Width { get { return image.Width; } }
		public int Height { get { return image.Height; } }

		WriteableBitmap bmp;
		public WriteableBitmap BitmapSource { get { UpdateBmp(); return bmp; } }

		private void UpdateBmp() {
			if (bmp == null) bmp = new WriteableBitmap(Width, Height, 96.0, 96.0, PixelFormats.Gray8, null);
			byte[] gray = new byte[image.RawData.Length];
			for (int i = 0; i < image.RawData.Length; i++)
				gray[i] = (byte)(255 - image.RawData[i] * 255);

			bmp.WritePixels(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight), gray, image.Stride, 0);
		}

		public sbyte this[int y, int x] { get { return (sbyte)(1 - image[x,y]); } }

		public float Interpolate(double y, double x) { return ImageDataConversion.Interpolate((yI, xI) => (float)this[yI, xI], y, x); }
		public ImageStruct<sbyte> Image { get { return image; } }
	}
}
