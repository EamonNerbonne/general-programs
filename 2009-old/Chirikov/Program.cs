using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;
using System.Drawing;

namespace Chirikov {
    class Program {
        public const int FrameCount = 10000;
        public const int Res = 1024;
        public const int trailLength = 70000;
        public const double maxK = 7.0;
        public const int ScaleConst = 10;

        static void Main(string[] args) {
            int size = 12;
            DateTime start = DateTime.Now;
            Random rand = new Random();
            double[] jiggle = new double[1337];
            int jiggleIndex=0;
            for (int i = 0; i < jiggle.Length; i++)
                jiggle[i] = (rand.NextDouble() - 0.5);
            for (int frameN = FrameCount/3; frameN < FrameCount; frameN++) {
                double k = frameN * maxK / FrameCount;
                byte[,] imgR = new byte[Res, Res];
                byte[,] imgG = new byte[Res, Res];
                byte[,] imgB = new byte[Res, Res];
                for (int ps = 0; ps < size; ps++) {
                    for (int xs = 0; xs < size; xs++) {
                        int r = (int)(ps / (double)size * 256);
                        int g = (int)(xs / (double)size * 256);
                        int b= (int)((ps - xs) / (double)size * 512 + 512) % 256;
                        foreach (ChirPoint px in
                            ChirIter.Generate(k, new ChirPoint(
                            (ps + 0.5) / (double)size, (xs + 0.5) / (double)size)).Take(trailLength)) {
                            int x = (int)(px.p * Res+jiggle[jiggleIndex]), y = (int)(px.x * Res+jiggle[jiggle.Length-1-jiggleIndex]);
                            x = (x + Res) % Res;
                            y = (y + Res) % Res;
                            jiggleIndex = (jiggleIndex + 1) % jiggle.Length;
                            imgR[x, y] = (byte)((ScaleConst * imgR[x, y] + r + ScaleConst/2) / (ScaleConst+1));
                            imgG[x, y] = (byte)((ScaleConst * imgG[x, y] + g + ScaleConst / 2) / (ScaleConst + 1));
                            imgB[x, y] = (byte)((ScaleConst * imgB[x, y] + b + ScaleConst / 2) / (ScaleConst + 1));
                        }
                    }
                }
                Bitmap bitmap = new Bitmap(Res, Res);
                for (int y = 0; y < Res; y++) {
                    for (int x = 0; x < Res; x++) {
                        bitmap.SetPixel(x, y, Color.FromArgb(255-imgR[x,y],255-imgG[x,y],255-imgB[x,y]));
                    }
                }
                bitmap.Save("C:\\chirout\\chir"+frameN.ToString("0000")+".png", System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("Wrote image " + frameN + ", ETA "+new TimeSpan(((TimeSpan)(DateTime.Now - start)).Ticks *(FrameCount-frameN-1)/ (frameN+1)));

            }
        }
    }
}
