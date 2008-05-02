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

        public static BitmapSource AsBitmapSource(this PixelArgb32[,] image) {
            return BitmapSource.Create(image.Width(), image.Height(), 96.0, 96.0, PixelFormats.Bgra32, null,
                           image.AsByteArray(), image.Width() * 4);
        }
    }
}
