using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using HwrLibCliWrapper;
using System.Windows.Media;

namespace HwrSplitter.Engine
{
	public static class ImageDataConversion
	{
		public static PixelArgb32 Interpolate(Func<int, int, PixelArgb32> image, double y, double x) {
			int yi = (int)Math.Floor(y),
				xi = (int)Math.Floor(x),
				yj = (int)Math.Ceiling(y),
				xj = (int)Math.Ceiling(x);//no problem if xi==xj, but x must be in range in the image!
			double scalexi = 1 + xi - x;
			double scaleyi = 1 + yi - y;
			double a = scalexi * scaleyi, b = (1 - scalexi) * scaleyi, c = scalexi * (1 - scaleyi), d = (1 - scalexi) * (1 - scaleyi);
			PixelArgb32 A = image(yi, xi), B = image(yi, xj), C = image(yj, xi), D = image(yj, xj);
			return new PixelArgb32(
				(byte)(a * A.A + b * B.A + c * C.A + d * D.A + 0.5),
				(byte)(a * A.R + b * B.R + c * C.R + d * D.R + 0.5),
				(byte)(a * A.G + b * B.G + c * C.G + d * D.G + 0.5),
				(byte)(a * A.B + b * B.B + c * C.B + d * D.B + 0.5)
				);
		}
		public static float Interpolate(Func<int, int, float> image, double y, double x) {
			int yi = (int)Math.Floor(y),
				xi = (int)Math.Floor(x),
				yj = (int)Math.Ceiling(y),
				xj = (int)Math.Ceiling(x);//no problem if xi==xj, but x must be in range in the image!
			float scalexi = 1 + xi - (float)x;
			float scaleyi = 1 + yi - (float)y;
			float a = scalexi * scaleyi, b = (1 - scalexi) * scaleyi, c = scalexi * (1 - scaleyi), d = (1 - scalexi) * (1 - scaleyi);
			float A = image(yi, xi), B = image(yi, xj), C = image(yj, xi), D = image(yj, xj);
			return (a * A + b * B + c * C + d * D);
		}


		public static BitmapSource ToBitmap(this ImageStruct<byte> imgData) {
			return BitmapSource.Create(imgData.Width, imgData.Height, 96.0, 96.0, PixelFormats.Gray8, null, imgData.RawData, imgData.Stride);
		}
		public static BitmapSource ToBitmap(this ImageStruct<uint> imgData)
		{
			return BitmapSource.Create(imgData.Width, imgData.Height, 96.0, 96.0, PixelFormats.Bgra32, null, imgData.RawData, imgData.Stride);
		}

	}
}
