using System;
using System.Collections.Generic;
using System.Linq;
namespace HwrSplitter.Gui
{

    /// <summary>
    /// This code is no longer used.  It was used to perform local contrast enhancement, but simply thresholding with 200 works almost as well and is much faster.
    /// </summary>
    static class SlowImageOperations
    {
		public static IEnumerable<int> Yrange<T>(this T[,] image) { return Enumerable.Range(0, image.Height()); }
		public static IEnumerable<int> Xrange<T>(this T[,] image) { return Enumerable.Range(0, image.Width()); }
		public static int Height<T>(this T[,] image) { return image.GetLength(0); }
		public static int Width<T>(this T[,] image) { return image.GetLength(1); }

        public static void Invert(float[,] image) {
            foreach (var y in image.Yrange())
                foreach (var x in image.Xrange()) {
                    image[y, x] = 1.0f - image[y, x];
                }
        }
        public static float[,] BoxBlur(float[,] image) {
            return BoxBlurH(BoxBlurV(image));
        }

        public static float[,] BoxBlurV(float[,] image) {
            float[,] retval = (float[,])image.Clone();
            for (int x = 0; x < image.Width(); x++) {
                retval[0, x] = (image[0, x] + image[1, x]) / 2.0f;
            }

            for (int y = 1; y < image.Height() - 1; y++)
                for (int x = 0; x < image.Width(); x++) {
                    retval[y, x] = (image[y - 1, x] + image[y, x] + image[y + 1, x]) / 3.0f;
                }

            for (int x = 0; x < image.Width(); x++) {
                retval[image.Height() - 1, x] = (image[image.Height() - 2, x] + image[image.Height() - 1, x]) / 2.0f;
            }

            return retval;
        }

        public static float[,] BoxBlurH(float[,] image) {
            float[,] retval = (float[,])image.Clone();
            for (int y = 0; y < image.Height(); y++) {
                retval[y, 0] = (image[y, 0] + image[y, 1]) / 2;
            }

            for (int y = 0; y < image.Height(); y++)
                for (int x = 1; x < image.Width() - 1; x++) {
                    retval[y, x] = (image[y, x - 1] + image[y, x] + image[y, x + 1]) / 3;
                }

            for (int y = 0; y < image.Height(); y++) {
                retval[y, image.Width() - 1] = (image[y, image.Width() - 2] + image[y, image.Width() - 1]) / 2;
            }

            return retval;
        }

        public static float[,] MinH(float[,] image) {
            float[,] retval = (float[,])image.Clone();
            for (int y = 0; y < image.Height(); y++) {

                retval[y, 0] = Math.Min(image[y, 0], image[y, 1]);
            }

            for (int y = 0; y < image.Height(); y++)
                for (int x = 1; x < image.Width() - 1; x++) {

                    retval[y, x] = Math.Min(image[y, x - 1], Math.Min(image[y, x], image[y, x + 1]));

                }

            for (int y = 0; y < image.Height(); y++) {
                retval[y, image.Width() - 1] = Math.Min(image[y, image.Width() - 2], image[y, image.Width() - 1]);
            }

            return retval;
        }

        public static float[,] BoxMin(float[,] image) {
            return MinH(MinV(image));
        }

        public static float[,] MinV(float[,] image) {
            float[,] retval = (float[,])image.Clone();
            for (int x = 0; x < image.Width(); x++) {


                retval[0, x] = Math.Min((uint)image[0, x], image[1, x]);
            }

            for (int y = 1; y < image.Height() - 1; y++)
                for (int x = 0; x < image.Width(); x++) {

                    retval[y, x] = Math.Min(Math.Min(image[y, x], image[y - 1, x]), image[y + 1, x]);

                }

            for (int x = 0; x < image.Width(); x++) {
                retval[image.Height() - 1, x] = Math.Min(image[image.Height() - 2, x], image[image.Height() - 1, x]);
            }

            return retval;
        }


        public static float[,] Median(float[,] image) {
            float[,] retval = (float[,])image.Clone();
            float[] vals = new float[9];
            for (int y = 1; y < image.Height() - 1; y++)
                for (int x = 1; x < image.Width() - 1; x++) {

                    var pixs = new[]{image[y-1,x-1],image[y-1,x+0],image[y-1,x+1],
                        image[y+0,x-1],image[y+0,x+0],image[y+0,x+1],
                        image[y+1,x-1],image[y+1,x+0],image[y+1,x+1]};//TODO:megaslow

                    Array.Sort(pixs);

                    retval[y, x] = pixs[4];

                }

            return retval;
        }

    }
}
