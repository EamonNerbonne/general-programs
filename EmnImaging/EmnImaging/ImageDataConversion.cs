using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace EmnImaging {
    public static class ImageDataConversion {
        public static byte[] AsByteArray(this PixelArgb32[,] image) {
            byte[] retval = new byte[image.Length * 4];
            int offset = 0;
            image.ForEach( pix => {
                retval[offset++] = pix.B;
                retval[offset++] = pix.G;
                retval[offset++] = pix.R;
                retval[offset++] = pix.A;
            });
            Debug.Assert(offset == 4 * image.Length, "Error in foreach - offset == 4*image.Length fails");
            return retval;
        }
        public static int Height(this PixelArgb32[,] image) { return image.GetLength(0); }
        public static int Width(this PixelArgb32[,] image) { return image.GetLength(1); }
        public static void ForEach(this PixelArgb32[,] image, Action<int, int, PixelArgb32> actionYXP) {
            int height = image.Height(), width = image.Width();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    actionYXP(y, x, image[y, x]);
        }
        public static void ForEach(this PixelArgb32[,] image, Action<PixelArgb32> actionP) {
            int height = image.Height(), width = image.Width();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    actionP(image[y, x]);
        }

        public static IEnumerable<PixelArgb32> AsEnumerable(this PixelArgb32[,] image) {
            int height = image.Height(), width = image.Width();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    yield return image[y, x];
        }

        public static IEnumerable<byte> AsEnumerableRs(this PixelArgb32[,] image) {
            return image.AsEnumerable().Select(p => p.R);
        }
        public static IEnumerable<byte> AsEnumerableGs(this PixelArgb32[,] image) {
            return image.AsEnumerable().Select(p => p.G);
        }
        public static IEnumerable<byte> AsEnumerableBs(this PixelArgb32[,] image) {
            return image.AsEnumerable().Select(p => p.B);
        }
        public static IEnumerable<byte> AsEnumerableAs(this PixelArgb32[,] image) {
            return image.AsEnumerable().Select(p => p.A);
        }


        public static BitmapSource AsBitmapSource(this PixelArgb32[,] image) {
            return BitmapSource.Create(image.Width(), image.Height(), 96.0, 96.0, PixelFormats.Bgra32, null,
                           image.AsByteArray(), image.Width() * 4);
        }

        public static PixelArgb32 Interpolate(this PixelArgb32[,] image, double y, double x) {
            int yi = (int)Math.Floor(y),
                xi = (int)Math.Floor(x),
                yj = (int)Math.Ceiling(y),
                xj = (int)Math.Ceiling(x);//no problem if xi==xj, but x must be in range in the image!
            double scalexi = 1 + xi - x;
            double scaleyi = 1 + yi - y;
            double a = scalexi * scaleyi, b = (1 - scalexi) * scaleyi, c = scalexi * (1 - scaleyi), d = (1 - scalexi) * (1 - scaleyi);
            PixelArgb32 A = image[yi, xi], B = image[yi, xj], C = image[yj, xi], D = image[yj, xj];
            return new PixelArgb32(
                (byte)(a * A.A + b * B.A + c * C.A + d * D.A + 0.5),
                (byte)(a * A.R + b * B.R + c * C.R + d * D.R + 0.5),
                (byte)(a * A.G + b * B.G + c * C.G + d * D.G + 0.5),
                (byte)(a * A.B + b * B.B + c * C.B + d * D.B + 0.5)
                );
        }
    }
}
