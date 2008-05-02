using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EmnImaging;
using System.Runtime.InteropServices;

namespace EmnImagingTestConverter {
    class Program {
        static void Main(string[] args) {
            if (args.Length !=3)
                Console.WriteLine("Usage: EmnImagingTestConverter.exe <inputfile> <outputjpgfile> <quality-percentage>");
            FileInfo inp = new FileInfo(args[0]);
            FileInfo outp = new FileInfo(args[1]);
            int quality = int.Parse(args[2]);
            var image =ImageIO.Load(inp);
            var reds=image.Cast<PixelArgb32>().Select(p=>p.R);
            var greens=image.Cast<PixelArgb32>().Select(p=>p.G);
            var blues=image.Cast<PixelArgb32>().Select(p=>p.B);

            Console.WriteLine("Average: ({0},{1},{2})", reds.Cast<int>().Average(), greens.Cast<int>().Average(), blues.Cast<int>().Average());

            ImageIO.SaveAsJpeg(image, outp, quality);
            
        }
    }
}
