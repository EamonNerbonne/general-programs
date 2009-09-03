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
		const int SCALE = 64;
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
			uint[] data = new uint[original.PixelWidth * original.PixelHeight];//TODO:these lines prevent some sort of loading bug ... that occurs sometimes...
			original.CopyPixels(data, original.PixelWidth * 4, 0);
			original.CopyPixels(rawImg.RawData, rawImg.Stride, 0);

			var greyScale = rawImg.MapTo(nr => ((PixelArgb32)nr).R);
			rawImg = default(ImageStruct<uint>);

			var minSmall = new ImageStruct<ushort>((greyScale.Width + SCALE - 1) / SCALE, (greyScale.Height + SCALE - 1) / SCALE);
			var maxSmall = new ImageStruct<ushort>((greyScale.Width + SCALE - 1) / SCALE, (greyScale.Height + SCALE - 1) / SCALE);
			for (int y = 0; y < minSmall.Height; y++)
				for (int x = 0; x < minSmall.Width; x++) {
					minSmall[x, y] = 255;
					maxSmall[x, y] = 0;
				}

			for (int y = 0; y < greyScale.Height; y++)
				for (int x = 0; x < greyScale.Width; x++) {
					int sx = x / SCALE;
					int sy = y / SCALE;
					minSmall[sx, sy] = Math.Min(minSmall[sx, sy], greyScale[x, y]);
					maxSmall[sx, sy] = Math.Max(maxSmall[sx, sy], greyScale[x, y]);
				}



			for (int y = 0; y < minSmall.Height; y++) {
				for (int x = 0; x < minSmall.Width - 1; x++) {
					minSmall[x, y] = (ushort)(minSmall[x, y] + minSmall[x + 1, y]);
					maxSmall[x, y] = (ushort)(maxSmall[x, y] + maxSmall[x + 1, y]);
				}
				minSmall[minSmall.Width - 1, y] *= 2;
				maxSmall[minSmall.Width - 1, y] *= 2;

				for (int x = minSmall.Width - 1; x > 0; x--) {
					minSmall[x, y] = (ushort)(minSmall[x, y] + minSmall[x - 1, y]);
					maxSmall[x, y] = (ushort)(maxSmall[x, y] + maxSmall[x - 1, y]);
				}
				minSmall[0, y] *= 2;
				maxSmall[0, y] *= 2;
			}

			for (int y = 0; y < minSmall.Height - 1; y++)
				for (int x = 0; x < minSmall.Width; x++) {
					minSmall[x, y] = (ushort)(minSmall[x, y] + minSmall[x, y + 1]);
					maxSmall[x, y] = (ushort)(maxSmall[x, y] + maxSmall[x, y + 1]);
				}
			for (int x = 0; x < minSmall.Width; x++) {
				minSmall[x, minSmall.Height - 1] *= 2;
				maxSmall[x, minSmall.Height - 1] *= 2;
			}

			for (int y = minSmall.Height - 1; y > 0; y--)
				for (int x = 0; x < minSmall.Width; x++) {
					minSmall[x, y] = (ushort)(minSmall[x, y] + minSmall[x, y - 1]);
					maxSmall[x, y] = (ushort)(maxSmall[x, y] + maxSmall[x, y - 1]);
				}
			for (int x = 0; x < minSmall.Width; x++) {
				minSmall[x, 0] *= 2;
				maxSmall[x, 0] *= 2;
			}



#if LOGSPEED
			timer.TimeMark("threshholding");
#endif

			image = new ImageStruct<sbyte>(greyScale.Width, greyScale.Height);

			for (int y = 0; y < greyScale.Height; y++)
				for (int x = 0; x < greyScale.Width; x++) {
					int sx = x / SCALE;
					int sy = y / SCALE;
					byte threshold = (byte)(200.0*0.6+ 0.4*(minSmall[sx, sy] * (1 / 16.0) * 0.3 + maxSmall[sx, sy] * (1 / 16.0) * 0.7) + 0.5);
					image[x, y] = greyScale[x, y] > threshold ? (sbyte)0 : (sbyte)1;
				}


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

		public double[] XProjectionSmart { get; set; }
		public double[] XProjectionRaw { get; set; }

		public void ComputeXProjection(int x0, int x1) {
			double[] xProjectionSmart = new double[Height];
			double[] xProjectionRaw = new double[Height];
			for (int y = 0; y < Height; y++) {
				int sum = 0;
				int sumRaw = 0;
				for (int x = x0 + 1; x < x1 - 1; x++) {
					if (y > 0 && y < Height - 1
						&& image[x, y] != 0

						//&& image[x-1, y] == 0

						&& image[x - 1, y - 1] != 0
						&& image[x + 1, y + 1] != 0
						&& (image[x + 1, y] == 0 || image[x, y - 1] == 0)
						&& image[x + 1, y - 1] == 0
						)
						sum++;

					if (image[x, y] != 0)
						sumRaw++;
				}
				xProjectionSmart[y] = (sum + sumRaw) / 2 / (double)(x1 - x0);
				xProjectionRaw[y] = sumRaw / (double)(x1 - x0);
			}
			XProjectionSmart = xProjectionSmart;
			XProjectionRaw = xProjectionRaw;
		}

	}
}
