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
using HwrDataModel;

namespace HwrSplitter.Engine {
	public class HwrPageImage {
		static void Fac16SumBlur(ImageStruct<int> img) {
			for (int y = 0; y < img.Height; y++) {
				for (int x = 0; x < img.Width - 1; x++)
					img[x, y] = (img[x, y] + img[x + 1, y]);

				img[img.Width - 1, y] *= 2;

				for (int x = img.Width - 1; x > 0; x--)
					img[x, y] = (img[x, y] + img[x - 1, y]);

				img[0, y] *= 2;
			}

			for (int y = 0; y < img.Height - 1; y++)
				for (int x = 0; x < img.Width; x++)
					img[x, y] = (img[x, y] + img[x, y + 1]);

			for (int x = 0; x < img.Width; x++)
				img[x, img.Height - 1] *= 2;


			for (int y = img.Height - 1; y > 0; y--)
				for (int x = 0; x < img.Width; x++)
					img[x, y] = (img[x, y] + img[x, y - 1]);

			for (int x = 0; x < img.Width; x++)
				img[x, 0] *= 2;
		}

		const int SCALE =32;//not more than 256 due to overflow! 
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
			original.Freeze();

#if LOGSPEED
			timer.TimeMark("Converting to array");
#endif
			var rawImg = new ImageStruct<uint>(original.PixelWidth, original.PixelHeight);
			//loading bug debug help: Console.WriteLine("{0:X}", rawImg[1000, 2000]);
			uint[] data = new uint[original.PixelWidth * original.PixelHeight];//HACK:these lines prevent some sort of loading bug ... that occurs sometimes...
			original.CopyPixels(data, original.PixelWidth * 4, 0);
			original.CopyPixels(rawImg.RawData, rawImg.Stride, 0);

			var greyScale = rawImg.MapTo(nr => ((PixelArgb32)nr).R);
			rawImg = default(ImageStruct<uint>);

			var avgXSmall = new ImageStruct<int>((greyScale.Width + SCALE - 1) / SCALE, greyScale.Height); //mean only over x-direction.
			var avgYSmall = new ImageStruct<int>(greyScale.Width, (greyScale.Height + SCALE - 1) / SCALE); //mean only over y-direction.
			var maxSmall = new ImageStruct<int>((greyScale.Width + SCALE - 1) / SCALE, (greyScale.Height + SCALE - 1) / SCALE);
			var maxYSmall = new ImageStruct<int>(greyScale.Width, (greyScale.Height + SCALE - 1) / SCALE); //max only over y-direction.

			var avg = new ImageStruct<int>(greyScale.Width, greyScale.Height); //mean only over y-direction.
			for (int y = 0; y < avg.Height; y++)
				for (int x = 0; x < avg.Width; x++)
					avg[x, y] = greyScale[x, y];

			for (int y = 0; y < maxSmall.Height; y++)
				for (int x = 0; x < maxSmall.Width; x++)
					maxSmall[x, y] = 0;

			for (int y = 0; y < avgXSmall.Height; y++)
				for (int x = 0; x < avgXSmall.Width; x++)
					avgXSmall[x, y] = 0;

			for (int y = 0; y < avgYSmall.Height; y++)
				for (int x = 0; x < avgYSmall.Width; x++)
					avgYSmall[x, y] = 0;

			for (int y = 0; y < greyScale.Height; y++) {
				int sy = y / SCALE;
				for (int x = 0; x < greyScale.Width; x++) {
					int sx = x / SCALE;
					avgXSmall[sx, y] = avgXSmall[sx, y] + (int)greyScale[x, y];
					avgYSmall[x, sy] = avgYSmall[x, sy] + (int)greyScale[x, y];
					maxSmall[sx, sy] = Math.Max(maxSmall[sx, sy], greyScale[x, y]);
					maxYSmall[x, sy] = Math.Max(maxYSmall[x, sy], greyScale[x, y]);
				}
				int missingPix = avgXSmall.Width * SCALE - greyScale.Width;
				double scaleFac = SCALE / (double)(SCALE - missingPix);
				avgXSmall[avgXSmall.Width - 1, y] = (int)(avgXSmall[avgXSmall.Width - 1, y] * scaleFac + 0.5);
			}

			Fac16SumBlur(maxSmall);
			Fac16SumBlur(maxYSmall);
			Fac16SumBlur(avgXSmall);
			Fac16SumBlur(avgYSmall);
			Fac16SumBlur(avg);

#if LOGSPEED
			timer.TimeMark("threshholding");
#endif

			image = new ImageStruct<sbyte>(greyScale.Width, greyScale.Height);

			for (int y = 0; y < greyScale.Height; y++)
				for (int x = 0; x < greyScale.Width; x++) {
					int sx = x / SCALE;
					int sy = y / SCALE;
					byte threshold = (byte)(
						0.1 * (avg[x,y]*1/16.0)//200.0
						+ 0.05 * (maxSmall[sx, sy] * (1 / 16.0) * 0.91)
						+ 0.4 * (maxYSmall[x, sy] * (1 / 16.0) * 0.91)
						+ 0.1 * (avgXSmall[sx, y] * (1 / (double)SCALE / 16.0) * 0.91)
						+ 0.35 * (avgYSmall[x, sy] * (1 / (double)SCALE /16.0) * 0.91) 
						+ 0.5);
					image[x, y] = greyScale[x, y] > threshold ? (sbyte)0 : (sbyte)1;
				}

			bmp = UpdateBmp(image);

#if LOGSPEED
			timer.TimeMark(null);
#endif
		}

		public int Width { get { return image.Width; } }
		public int Height { get { return image.Height; } }

		readonly WriteableBitmap bmp;
		public ImageSource BitmapSource { get { return bmp; } }

		static WriteableBitmap UpdateBmp(ImageStruct<sbyte> img) {
			var bmp = new WriteableBitmap(img.Width, img.Height, 96.0, 96.0, PixelFormats.Gray8, null);
			byte[] gray = new byte[img.RawData.Length];
			for (int i = 0; i < img.RawData.Length; i++)
				gray[i] = (byte)(255 - img.RawData[i] * 255);

			bmp.WritePixels(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight), gray, img.Stride, 0);
			bmp.Freeze();
			return bmp;
		}

		public sbyte this[int y, int x] { get { return (sbyte)(1 - image[x, y]); } }

		public float Interpolate(double y, double x) { return ImageDataConversion.Interpolate((yI, xI) => (float)this[yI, xI], y, x); }
		public ImageStruct<sbyte> Image { get { return image; } }
		public HwrTextPage TextPage { get; set; }

		public double[] XProjectionSmart { get; set; }

		public void ComputeXProjection(int x0, int x1) {
			double[] xProjectionSmart = new double[Height];
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
				xProjectionSmart[y] = (sum * 2 + sumRaw) / 3.0 / (double)(x1 - x0);
				//xProjectionRaw[y] = sumRaw / (double)(x1 - x0);
			}
			XProjectionSmart = xProjectionSmart;
		}

		public void ComputeFeatures(HwrTextLine line, out BitmapSource featureImage, out Point offset) {
			int topXoffset;

			int x0 = Math.Max(0, (int)(line.OuterExtremeLeft + 0.5));
			int x1 = Math.Min(image.Width, (int)(line.OuterExtremeRight + 0.5));
			int y0 = (int)(line.top + 0.5);
			int y1 = (int)(line.bottom + 0.5);

			ImageStruct<float> data = ImageProcessor.ExtractFeatures(this.Image.CropTo(x0, y0, x1, y1), line, out topXoffset);
			int featDataY = y0;
			int featDataX = (int)x0 + topXoffset;
			var featImgRGB = data.MapTo(f => (byte)(255.9 * f)).MapTo(b => new PixelArgb32(255, b, b, b));
			foreach (int wordBoundary in
							from word in line.words
							from edge in new[] { word.left, word.right }
							let edgeTrans = (int)(edge + 0.5) - featDataX
							where edgeTrans >= 0 && edgeTrans < featImgRGB.Width
							select edgeTrans) {
				for (int y = 0; y < featImgRGB.Height; y++) {
					var pix = featImgRGB[wordBoundary, y];
					pix.R = 255;
					pix.B = 255;
					featImgRGB[wordBoundary, y] = pix;
				}
			}

			for (int x = 0; x < featImgRGB.Width; x++) { //only useful for scaled version, not for features!
				var pix = featImgRGB[x, line.bodyTop];
				pix.G = 255;
				featImgRGB[x, line.bodyTop] = pix;
				var pixB = featImgRGB[x, line.bodyBot];
				pixB.G = 255;
				featImgRGB[x, line.bodyBot] = pixB;
			}


			featureImage = featImgRGB.MapTo(p => p.Data).ToBitmap();
			featureImage.Freeze();
			offset = new Point(featDataX, featDataY);
		}
	}
}
