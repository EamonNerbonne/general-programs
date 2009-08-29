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

namespace HwrSplitter.Engine
{
	public class HwrPageImage
	{
		ImageStruct<sbyte> image;
		//BitmapImage original;
		public HwrPageImage(FileInfo fileToLoad) {
#if LOGSPEED
			NiceTimer timer = new NiceTimer();
			timer.TimeMark("Loading");
#endif
			BitmapImage
				original = new BitmapImage();
			original.BeginInit();
			original.UriSource = new Uri(fileToLoad.FullName);
			original.EndInit();
			//Console.WriteLine(original.IsDownloading);
			original.Freeze();

#if LOGSPEED
			timer.TimeMark("Converting to array");
#endif
			var rawImg = new ImageStruct<uint>(original.PixelWidth, original.PixelHeight);
			//Console.WriteLine("{0:X}", rawImg[1000, 2000]);//loading bug debug help
			uint[] data = new uint[original.PixelWidth * original.PixelHeight];//TODO:these lines prevent some sort of loading bug ... sometimes...
			original.CopyPixels(data, original.PixelWidth*4, 0);
			original.CopyPixels(rawImg.RawData, rawImg.Stride, 0);
			//Console.WriteLine("{0:X}", rawImg[1000, 2000]);//loading bug debug help
#if VIACPP
#if LOGSPEED
			timer.TimeMark("preprocessing");
#endif
			image = ImageProcessor.preprocess(rawImg);
#else
#if LOGSPEED
			timer.TimeMark("threshholding");
#endif
			image = rawImg.MapTo(nr => ((PixelArgb32)nr).R >= 200 ? (sbyte)0 : (sbyte)1);
#endif
#if LOGSPEED
			timer.TimeMark(null);
#endif
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

		public sbyte this[int y, int x] { get { return (sbyte)(1 - image[x, y]); } }

		public float Interpolate(double y, double x) { return ImageDataConversion.Interpolate((yI, xI) => (float)this[yI, xI], y, x); }
		public ImageStruct<sbyte> Image { get { return image; } }
	}
}
