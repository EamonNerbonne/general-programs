using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnImaging;
using System.Windows;
using HWRsplitter;
using System.Xml.Linq;
using System.IO;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using EamonExtensionsLinq.DebugTools;
using EamonExtensionsLinq.Text;

namespace EmnImageTestDisplay {
    class Program : Application {


        [STAThread]
        public static void Main(string[] args) {


            Program app = new Program();
            app.Exec();
        }

        public void ChooseImage() {
            var imgNumStrs = from filepath in Directory.GetFiles(HWRsplitter.Program.ImgPath, "NL_HaNa_H2_7823_*.tif")
                             let filename = System.IO.Path.GetFileName(filepath)
                             let m = Regex.Match(filename, @"^NL_HaNa_H2_7823_(?<num>\d+).tif$")
                             where m.Success
                             let numstr = m.Groups["num"].Value
                             select numstr;
            var wordNumStrs = from filepath in Directory.GetFiles(
                                  System.IO.Path.Combine(HWRsplitter.Program.DataPath, "words-train"), "NL_HaNa_H2_7823_*.words")
                              let filename = System.IO.Path.GetFileName(filepath)
                              let m = Regex.Match(filename, @"^NL_HaNa_H2_7823_(?<num>\d+).words$")
                              where m.Success
                              let numstr = m.Groups["num"].Value
                              select numstr;
            var bothNumStrs = imgNumStrs.Intersect(wordNumStrs).ToArray();
            numStr = bothNumStrs[2];//TODO:make real choice


            string wordsPath = System.IO.Path.Combine(HWRsplitter.Program.DataPath, @"words-train\NL_HaNa_H2_7823_" + numStr + ".words");
            wordsFileInfo = new FileInfo(wordsPath);


            string imgPath = System.IO.Path.Combine(HWRsplitter.Program.ImgPath, "NL_HaNa_H2_7823_" + numStr + ".tif");
            imageFileInfo = new FileInfo(imgPath);

            pageNum = int.Parse(numStr);
            Log("Chose page:" + pageNum);
        }
        FileInfo wordsFileInfo, imageFileInfo;
        string numStr;
        ImageWindow imgWin;
        int pageNum;
        WordsImage words;

        public void LoadWords() {
            var xmlWords = XDocument.Load(wordsFileInfo.OpenText());
            words = new WordsImage(xmlWords.Root);
            Log("Loaded .words file");
            imgWin.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<WordsImage, Brush>(this.ProcessLinesUI), words, Brushes.DarkRed);
        }
        PixelArgb32[,] image;
        public void LoadImage() {
            image = ImageIO.Load(imageFileInfo);
            imgWin.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(this.ProcessImageUI));

            /* Really slow, sigh, need to implement a better matrix handling for images.
 * 
 * Invert(image);
var avgImg = BoxBlur(BoxBlur(BoxBlur(image)));
var localMin = RepeatFunc<PixelArgb32[,]>(img=>BoxMin(img), 15)(avgImg);

localMin.ForEach((y, x, p) => { 
    image[y, x] = PixelArgb32.Combine(
        image[y, x], p, 
        (a, b) => (byte)Math.Max(a - b, 0)
        );
});
MaxContrast(image);
Invert(image);*/
            Log("Image loaded: " + image.Width() + "x" + image.Height());
        }
        ProgressWindow progressWindow;
        public Program() {
            imgWin = new ImageWindow();
            progressWindow = new ProgressWindow();
            Console.SetOut(new DelegateTextWriter(s => { progressWindow.Append(s); }));
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        public void Exec() { this.Run(imgWin); }
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            progressWindow.Show();
            Thread worker = new Thread(this.LoadInBackground);
            worker.Name = "WorkerHWR";
            worker.Start();
            Log("Started Worker Thread");
        }
        void LoadInBackground() {
            ChooseImage();
            progressWindow.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(progressWindow.Close));
            LoadImage();
            LoadWords();
            LoadAnnot();
            ImproveGuess();
        }
        class MyWriter : TextWriter {
            Program prog;
            public MyWriter(Program prog) {
                this.prog = prog;
            }
            public override Encoding Encoding { get { return Encoding.Unicode; } }
            override public void Write(string str) {
                prog.progressWindow.Append(str);
            }

        }
        private void ImproveGuess() {

        }
        private void Log(string logmsg) {
            progressWindow.AppendLine(logmsg);
        }
        private static Line mkLine(double x1, double y1, double x2, double y2, Brush brush) {
            return new Line {
                Stroke = brush,
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeThickness = 5
            };
        }
        private static IEnumerable<Line> WordToLines(Word word, Brush brush) {
            var height = word.bottom - word.top;
            var xcorr = height * Math.Tan(2 * Math.PI * word.shear / 360.0);
            yield return mkLine(word.left, word.top, word.right, word.top, brush);
            yield return mkLine(word.right, word.top, word.right - xcorr, word.bottom, brush);
            yield return mkLine(word.right - xcorr, word.bottom, word.left - xcorr, word.bottom, brush);
            yield return mkLine(word.left - xcorr, word.bottom, word.left, word.top, brush);
        }
        WordsImage wordsGuess;
        void LoadAnnot() {
            wordsGuess = LinesAnnot.GuessWord(new FileInfo(System.IO.Path.Combine(HWRsplitter.Program.DataPath, "line_annot.txt")), pageNum);
            Log("Loaded line_annot and parsed it");
            imgWin.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<WordsImage, Brush>(this.ProcessLinesUI), wordsGuess, Brushes.Blue);


        }

        void ProcessLinesUI(WordsImage words, Brush brush) {
            var lines = from textline in words.textlines
                        from word in textline.words
                        from line in WordToLines(word, brush)
                        select line;
            imgWin.AddShapes(lines.Cast<UIElement>());
        }

        void ProcessImageUI() {
            imgWin.SetImage(image);
        }

        public static Func<T, T> RepeatFunc<T>(Func<T, T> f, int num) {
            return (t => { while (num-- > 0) t = f(t); return t; });
        }
        public static void Invert(PixelArgb32[,] image) {
            image.ForEach((y, x, p) => {
                image[y, x].Data = p.Data ^ (uint.MaxValue ^ PixelArgb32.AMask);
            });
        }
        public static PixelArgb32[,] BoxBlur(PixelArgb32[,] image) {
            return BoxBlurH(BoxBlurV(image));
        }

        public static PixelArgb32[,] BoxBlurV(PixelArgb32[,] image) {
            PixelArgb32[,] retval = (PixelArgb32[,])image.Clone();
            for (int x = 0; x < image.Width(); x++) {
                var R = (byte)(((uint)image[0, x].R + image[1, x].R + 1) / 2);
                var G = (byte)(((uint)image[0, x].G + image[1, x].G + 1) / 2);
                var B = (byte)(((uint)image[0, x].B + image[1, x].B + 1) / 2);
                retval[0, x] = new PixelArgb32(255, R, G, B);
            }

            for (int y = 1; y < image.Height() - 1; y++)
                for (int x = 0; x < image.Width(); x++) {
                    var R = (byte)((image[y - 1, x].R + (uint)image[y, x].R + image[y + 1, x].R + 1) / 3);
                    var G = (byte)((image[y - 1, x].G + (uint)image[y, x].G + image[y + 1, x].G + 1) / 3);
                    var B = (byte)((image[y - 1, x].B + (uint)image[y, x].B + image[y + 1, x].B + 1) / 3);
                    retval[y, x] = new PixelArgb32(255, R, G, B);
                }

            for (int x = 0; x < image.Width(); x++) {
                var R = (byte)(((uint)image[image.Height() - 2, x].R + image[image.Height() - 1, x].R + 1) / 2);
                var G = (byte)(((uint)image[image.Height() - 2, x].G + image[image.Height() - 1, x].G + 1) / 2);
                var B = (byte)(((uint)image[image.Height() - 2, x].B + image[image.Height() - 1, x].B + 1) / 2);
                retval[image.Height() - 1, x] = new PixelArgb32(255, R, G, B);
            }

            return retval;
        }

        public static PixelArgb32[,] BoxBlurH(PixelArgb32[,] image) {
            PixelArgb32[,] retval = (PixelArgb32[,])image.Clone();
            for (int y = 0; y < image.Height(); y++) {
                var R = (byte)(((uint)image[y, 0].R + image[y, 1].R + 1) / 2);
                var G = (byte)(((uint)image[y, 0].G + image[y, 1].G + 1) / 2);
                var B = (byte)(((uint)image[y, 0].B + image[y, 1].B + 1) / 2);
                retval[y, 0] = new PixelArgb32(255, R, G, B);
            }

            for (int y = 0; y < image.Height(); y++)
                for (int x = 1; x < image.Width() - 1; x++) {
                    var R = (byte)((image[y, x - 1].R + (uint)image[y, x].R + image[y, x + 1].R + 1) / 3);
                    var G = (byte)((image[y, x - 1].G + (uint)image[y, x].G + image[y, x + 1].G + 1) / 3);
                    var B = (byte)((image[y, x - 1].B + (uint)image[y, x].B + image[y, x + 1].B + 1) / 3);
                    retval[y, x] = new PixelArgb32(255, R, G, B);
                }

            for (int y = 0; y < image.Height(); y++) {
                var R = (byte)(((uint)image[y, image.Width() - 2].R + image[y, image.Width() - 1].R + 1) / 2);
                var G = (byte)(((uint)image[y, image.Width() - 2].G + image[y, image.Width() - 1].G + 1) / 2);
                var B = (byte)(((uint)image[y, image.Width() - 2].B + image[y, image.Width() - 1].B + 1) / 2);
                retval[y, image.Width() - 1] = new PixelArgb32(255, R, G, B);
            }

            return retval;
        }

        public static PixelArgb32[,] MinH(PixelArgb32[,] image) {
            PixelArgb32[,] retval = (PixelArgb32[,])image.Clone();
            byte r, g, b;
            for (int y = 0; y < image.Height(); y++) {

                var R = (byte)Math.Min((uint)image[y, 0].R, image[y, 1].R);
                var G = (byte)Math.Min((uint)image[y, 0].G, image[y, 1].G);
                var B = (byte)Math.Min((uint)image[y, 0].B, image[y, 1].B);
                retval[y, 0] = new PixelArgb32(255, R, G, B);
            }

            for (int y = 0; y < image.Height(); y++)
                for (int x = 1; x < image.Width() - 1; x++) {
                    r = g = b = 255;

                    for (int d = -1; d < 2; d++) {
                        if (image[y, x + d].R < r)
                            r = image[y, x + d].R;
                        if (image[y, x + d].G < g)
                            g = image[y, x + d].G;
                        if (image[y, x + d].B < b)
                            b = image[y, x + d].B;
                    }
                    retval[y, x] = new PixelArgb32(255, r, g, b);

                }

            for (int y = 0; y < image.Height(); y++) {
                var R = (byte)Math.Min((uint)image[y, image.Width() - 2].R, image[y, image.Width() - 1].R);
                var G = (byte)Math.Min((uint)image[y, image.Width() - 2].G, image[y, image.Width() - 1].G);
                var B = (byte)Math.Min((uint)image[y, image.Width() - 2].B, image[y, image.Width() - 1].B);
                retval[y, image.Width() - 1] = new PixelArgb32(255, R, G, B);
            }

            return retval;
        }

        public static PixelArgb32[,] BoxMin(PixelArgb32[,] image) {
            return MinH(MinV(image));
        }

        public static PixelArgb32[,] MinV(PixelArgb32[,] image) {
            PixelArgb32[,] retval = (PixelArgb32[,])image.Clone();
            byte r, g, b;
            for (int x = 0; x < image.Width(); x++) {

                var R = (byte)Math.Min((uint)image[0, x].R, image[1, x].R);
                var G = (byte)Math.Min((uint)image[0, x].G, image[1, x].G);
                var B = (byte)Math.Min((uint)image[0, x].B, image[1, x].B);
                retval[0, x] = new PixelArgb32(255, R, G, B);
            }

            for (int y = 1; y < image.Height() - 1; y++)
                for (int x = 0; x < image.Width(); x++) {
                    r = g = b = 255;

                    for (int d = -1; d < 2; d++) {
                        if (image[y + d, x].R < r)
                            r = image[y + d, x].R;
                        if (image[y + d, x].G < g)
                            g = image[y + d, x].G;
                        if (image[y + d, x].B < b)
                            b = image[y + d, x].B;
                    }
                    retval[y, x] = new PixelArgb32(255, r, g, b);

                }

            for (int x = 0; x < image.Width(); x++) {
                var R = (byte)Math.Min((uint)image[image.Height() - 2, x].R, image[image.Height() - 1, x].R);
                var G = (byte)Math.Min((uint)image[image.Height() - 2, x].G, image[image.Height() - 1, x].G);
                var B = (byte)Math.Min((uint)image[image.Height() - 2, x].B, image[image.Height() - 1, x].B);
                retval[image.Height() - 1, x] = new PixelArgb32(255, R, G, B);
            }

            return retval;
        }


        public static PixelArgb32[,] Median(PixelArgb32[,] image) {
            PixelArgb32[,] retval = (PixelArgb32[,])image.Clone();
            uint[] r = new uint[9], g = new uint[9], b = new uint[9];
            for (int y = 1; y < image.Height() - 1; y++)
                for (int x = 1; x < image.Width() - 1; x++) {

                    var pixs = new[]{image[y-1,x-1],image[y-1,x+0],image[y-1,x+1],
                        image[y+0,x-1],image[y+0,x+0],image[y+0,x+1],
                        image[y+1,x-1],image[y+1,x+0],image[y+1,x+1]};
                    for (int i = 0; i < 9; i++) {
                        r[i] = pixs[i].R;
                        g[i] = pixs[i].G;
                        b[i] = pixs[i].B;
                    }
                    Array.Sort(r);
                    Array.Sort(g);
                    Array.Sort(b);

                    retval[y, x] = new PixelArgb32(255, (byte)r[4], (byte)g[4], (byte)b[4]);

                }

            return retval;
        }

        public static void MaxContrast(PixelArgb32[,] image) {
            var minMax = image.AsEnumerable().Aggregate(
                new {
                    Max = new PixelArgb32(0, 0, 0, 0),
                    Min = new PixelArgb32(255, 255, 255, 255)
                },
                (cur, pix) =>
                new {
                    Max = PixelArgb32.Combine(cur.Max, pix, Math.Max),
                    Min = PixelArgb32.Combine(cur.Min, pix, Math.Min)
                });
            var range = PixelArgb32.Combine(minMax.Max, minMax.Min, (x, y) => (byte)(x - y));
            //  range = PixelArgb32.Combine(range, new PixelArgb32(1, 1, 1, 1), Math.Max);
            image.ForEach((y, x, p) => {
                image[y, x] = PixelArgb32.Combine(
                   PixelArgb32.Combine(p, minMax.Min, (pix, min) => (byte)(pix - min)),
                   range, (oldval, oldrange) => oldrange == 0 ? (byte)255 : (byte)(((uint)oldval) * 255 / oldrange)
                   );
            });

        }
    }
}
