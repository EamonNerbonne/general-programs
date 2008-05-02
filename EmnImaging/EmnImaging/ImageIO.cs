using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace EmnImaging {
    public enum ImageFormats {
        /// <summary>
        /// Note that this Jpeg codec will use a default (rather low) quality setting.  Use a manual parameter for higher quality.
        /// </summary>
        Jpeg, 
        Png, 
        Tiff
    }

    public static class ImageIO {
        public static PixelArgb32[,] Load(Stream stream) { return LoadBitmap((Bitmap)Bitmap.FromStream(stream)); }
        public static PixelArgb32[,] Load(FileInfo fileInfo) { return LoadBitmap((Bitmap)Bitmap.FromFile(fileInfo.FullName)); }
        public static PixelArgb32[,] Load(string filePath) { return LoadBitmap((Bitmap)Bitmap.FromFile(filePath)); }
        public static PixelArgb32[,] LoadBitmap(Bitmap bitmap) {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try {

                if (bitmapData.Stride % 4 != 0)
                    throw new ArgumentException("Image data is not word-aligned!");
                var stride = bitmapData.Stride / 4;

                var width = bitmap.Width;
                var height = bitmap.Height;

                int rowPadding = stride - width;
                var numPixels = stride * height;

                var retval = new PixelArgb32[height, width];
                var intRetval = new int[numPixels];
                Marshal.Copy(bitmapData.Scan0, intRetval, 0, numPixels); //extra allocation to avoid unsafe code.

                int offset = 0;
                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++)
                        retval[y, x] = new PixelArgb32 { Data = (uint)intRetval[offset++] };
                    offset += rowPadding;
                }

                Debug.Assert(offset == numPixels);
                return retval;
            } finally {
                bitmap.UnlockBits(bitmapData);
            }
        }

        public static Bitmap CreateGdiBitmap(this PixelArgb32[,] image) {
            var width = image.GetLength(1);
            var height = image.GetLength(0);
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try {

                if (bitmapData.Stride % 4 != 0)
                    throw new ArgumentException("Image data is not word-aligned!");
                var stride = bitmapData.Stride / 4;
                int rowPadding = stride - width;
                var numPixels = stride * height;


                var intImage = new int[numPixels];

                int offset = 0;
                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++)
                        intImage[offset++] = (int)image[y, x].Data;
                    offset += rowPadding;
                }

                Debug.Assert(offset == numPixels);

                Marshal.Copy(intImage, 0, bitmapData.Scan0, numPixels); //extra allocation to avoid unsafe code.

                return bitmap;
            } finally {
                bitmap.UnlockBits(bitmapData);
            }

        }

        private static ImageFormat fromEnum(ImageFormats format) {
            if (format == ImageFormats.Png)
                return ImageFormat.Png;
            else if (format == ImageFormats.Jpeg)
                return ImageFormat.Jpeg;
            else if (format == ImageFormats.Tiff)
                return ImageFormat.Tiff;
            else throw new ArgumentException("No known image format: {0}", format.ToString());
        }

        public static ImageCodecInfo FindJpegCodec() {
            return ImageCodecInfo.GetImageDecoders().Where(enc => enc.MimeType == "image/jpeg").First();
        }

        public static EncoderParameters QualityEncoderParameters(int qualityPercent) {
            EncoderParameters parameters = new EncoderParameters(1);
            //bugprone: note that using a different number format than "long" will break the encoder.
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)qualityPercent);
            return parameters;
        }

        public static void SaveAsJpeg(PixelArgb32[,] image, Stream targetStream, int qualityPercent) {
            WriteTo(image,targetStream,FindJpegCodec(),QualityEncoderParameters(qualityPercent));
        }
        public static void SaveAsJpeg(PixelArgb32[,] image, FileInfo targetFileInfo, int qualityPercent) {
            WriteTo(image,targetFileInfo,FindJpegCodec(),QualityEncoderParameters(qualityPercent));
        }
        public static void SaveAsJpeg(PixelArgb32[,] image, string targetFilePath, int qualityPercent) {
            WriteTo(image,targetFilePath,FindJpegCodec(),QualityEncoderParameters(qualityPercent));
        }

        public static void WriteTo(PixelArgb32[,] image, Stream targetStream, ImageFormats format) {
            ImageIO.WriteTo(image, targetStream, fromEnum(format));
        }
        public static void WriteTo(PixelArgb32[,] image, FileInfo targetFileInfo, ImageFormats format) {
            ImageIO.WriteTo(image, targetFileInfo, fromEnum(format));
        }
        public static void WriteTo(PixelArgb32[,] image, string targetFilePath, ImageFormats format) {
            ImageIO.WriteTo(image, targetFilePath, fromEnum(format));
        }
        public static void WriteTo(PixelArgb32[,] image, Stream targetStream, ImageFormat format) {
            CreateGdiBitmap(image).Save(targetStream, format);
        }
        public static void WriteTo(PixelArgb32[,] image, FileInfo targetFileInfo, ImageFormat format) {
            CreateGdiBitmap(image).Save(targetFileInfo.FullName, format);
        }
        public static void WriteTo(PixelArgb32[,] image, string targetFilePath, ImageFormat format) {
            CreateGdiBitmap(image).Save(targetFilePath, format);
        }
        public static void WriteTo(PixelArgb32[,] image, Stream targetStream, ImageCodecInfo codec, EncoderParameters parameters) {
            CreateGdiBitmap(image).Save(targetStream, codec,parameters);
        }
        public static void WriteTo(PixelArgb32[,] image, FileInfo targetFileInfo, ImageCodecInfo codec, EncoderParameters parameters) {
            CreateGdiBitmap(image).Save(targetFileInfo.FullName, codec, parameters);
        }
        public static void WriteTo(PixelArgb32[,] image, string targetFilePath, ImageCodecInfo codec, EncoderParameters parameters) {
            CreateGdiBitmap(image).Save(targetFilePath, codec, parameters);
        }

    }
}
