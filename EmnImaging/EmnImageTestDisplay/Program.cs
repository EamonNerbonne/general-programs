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
            numStr = bothNumStrs[1];//TODO:make real choice


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
            ProcessLines( words, Brushes.DarkRed);
        }
        PixelArgb32[,] image;
        public void LoadImage() {
            image = ImageIO.Load(imageFileInfo);
            imgWin.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(this.ProcessImageUI));


            /*
            Invert(image);
            imgWin.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(this.ProcessImageUI));
            var avgImg = BoxBlur(BoxBlur(BoxBlur(image)));
            Log("blurred.");
            var localMin = RepeatFunc<PixelArgb32[,]>(img => BoxMin(img), 15)(avgImg);
            Log("minimum found");
            localMin.ForEach((y, x, p) => {
                image[y, x] = PixelArgb32.Combine(
                    image[y, x], p,
                    (a, b) => (byte)Math.Max(a - b, 0)
                    );
            });
            Log("subtracted");
            imgWin.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(this.ProcessImageUI));
            MaxContrast(image);
            Log("contrast stretched");
            imgWin.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(this.ProcessImageUI));
            Invert(image);
             */
            Log("Image loaded: " + image.Width() + "x" + image.Height());
            imgWin.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(this.ProcessImageUI));

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
            LoadSymbolWidths();

            ChooseImage();
            LoadImage();
//            LoadWords();
            LoadAnnot();
            ImproveGuess();
        }

        //0 is begin line, 10 is line end, and each word includes one 32 for space.  Unknown letters get 1.
        Dictionary<char, SymbolWidth> symbolWidth;
        private void LoadSymbolWidths() {
            symbolWidth = SymbolWidthParser.Parse(new FileInfo(System.IO.Path.Combine(HWRsplitter.Program.DataPath, "char-width.txt")));
        }


        const double lenCostFactor = 100;
        const double posCostFactor = 400;
        const double intermedBrightnessCostFactor = 2;
        const double endPointCostFactor = 1;

        void ImproveGuess() {
            double[] blurwindow = Enumerable.Range(-20, 41).Select(v => Math.Exp(-0.3 * Math.Abs(v))).ToArray();
            var blurwindowSum = blurwindow.Sum();
            blurwindow = blurwindow.Select(x => x / blurwindowSum).ToArray();

            List<TextLine> betterGuessLine = new List<TextLine>();
            foreach (TextLine lineGuess in wordsGuess.textlines) {
                var shearedSum = ShearedSum((int)lineGuess.top, (int)lineGuess.bottom, lineGuess.shear);
                var blurredSum = BlurLine(shearedSum, blurwindow);
                var minVal = blurredSum.Min();
                var range = blurredSum.Max() - minVal;
                var contrastedSum = blurredSum.Select(x => (x - minVal) / range).ToArray();

                //now I should make a dual-deep lookup for good measure
                List<double[]> lightnessLookahead = new List<double[]>();
                //i.e. lightnessLookahed[lookahead][pos] returns the sum of contrastedSum from pos to pos+lookahead.
                lightnessLookahead.Add(contrastedSum);
                Action addOneLookahead = () => {
                    int nextLookahead = lightnessLookahead.Count;
                    double[] lookaheadrow = new double[contrastedSum.Length];
                    double[] oldrow = lightnessLookahead[nextLookahead - 1];
                    for (int i = 0; i < oldrow.Length; i++) {
                        lookaheadrow[i] = oldrow[i] + contrastedSum[(i + nextLookahead) % oldrow.Length];
                    }
                    lightnessLookahead.Add(lookaheadrow);
                };

                Func<int, int, double> Lookaheadsum = (pos, lookahead) => {
                    while (lightnessLookahead.Count <= lookahead) addOneLookahead();
                    return lightnessLookahead[lookahead][pos] / (lookahead+1); //calc average too!
                };


                double lastEnd=0;
                double lastEndWeight=0;
                List<Word> betterGuessWord = new List<Word>();
                foreach (Word wordGuess in lineGuess.words) {
                    double targetStart = wordGuess.left;
                    double targetEnd = wordGuess.right;
                    double targetLen = targetEnd - targetStart;
                    double targetPos = targetStart + targetEnd;
                    int MinLength = Math.Max(0, (int)Math.Ceiling((targetEnd - targetStart) - lenCostFactor));
                    int MaxLength = (int)((targetEnd - targetStart) + lenCostFactor);
                    int MinStartPos = Math.Max(0, (int)Math.Ceiling(targetStart - posCostFactor));
                    int MaxStartPos = (int)(targetStart + posCostFactor);


                    int bestGuessStart = -1;
                    int bestGuessEnd = -1;
                    double bestGuessCost = double.MaxValue;
                    for (int tryStart = MinStartPos; tryStart < MaxStartPos; tryStart++) {
                        int LastEndPos = Math.Min(image.Width() - 1, tryStart + MaxLength);
                        for (int tryEnd = tryStart+MinLength; tryEnd <= LastEndPos; tryEnd++) {
                            int tryLen = tryEnd-tryStart;
                            double lenDiff = (tryLen - targetLen) / lenCostFactor;//lower better
                            double posDiff = ((tryEnd + tryStart) - targetPos) / (posCostFactor * 2);//lower better
                            double intermedBrightness = Lookaheadsum(tryStart, tryLen);//lower better
                            double cost = lenDiff * lenDiff
                                + posDiff * posDiff
                                + Math.Abs(tryStart - lastEnd) * lastEndWeight
                                + intermedBrightness * intermedBrightnessCostFactor
                                - (contrastedSum[tryEnd] + contrastedSum[tryStart]) * endPointCostFactor;

                            if (cost < bestGuessCost) {
                                bestGuessStart = tryStart;
                                bestGuessEnd = tryEnd;
                                bestGuessCost = cost;
                            }
                        }

                    }
                    betterGuessWord.Add(
                        new Word(wordGuess.text, wordGuess.no, wordGuess.top, wordGuess.bottom,
                            bestGuessStart, bestGuessEnd, wordGuess.shear));
                    ProcessLines(Enumerable.Repeat(betterGuessWord[betterGuessWord.Count - 1], 1),Brushes.Green);
                    lastEnd = bestGuessEnd;
                    lastEndWeight = 1.0 / 30.0;

                }

                betterGuessLine.Add(new TextLine {
                    words = betterGuessWord.ToArray(),
                    shear = lineGuess.shear,
                    bottom = lineGuess.bottom,
                    top = lineGuess.top,
                    left = lineGuess.left,
                    right = lineGuess.right,
                    no = lineGuess.no
                });
            }
            betterGuessWords = new WordsImage {
                name = wordsGuess.name,
                pageNum = wordsGuess.pageNum,
                textlines = betterGuessLine.ToArray()
            };
            using (Stream stream = new FileInfo(wordsFileInfo.FullName + "guess").OpenWrite()) 
                using (TextWriter writer = new StreamWriter(stream))
                    writer.Write(betterGuessWords.AsXml().ToString());
            
        }
        WordsImage betterGuessWords;


        double[] BlurLine(double[] data, double[] window) {
            double[] retval = new double[data.Length];
            for(int di=0;di<data.Length;di++)
                for(int wi=0;wi<window.Length;wi++) {
                    int pos = di + wi - window.Length/2;
                    if (pos < 0)
                        pos = 0;
                    else if (pos >= data.Length)
                        pos = data.Length - 1;
                    retval[di] += data[pos] * window[wi];
                }
            return retval;
        }

        double[] ShearedSum(int top, int bottom, double shear) {
            var xOffsetLookup = Enumerable.Range(0, bottom - top).Select(
                yOffset => -yOffset * Math.Tan(2 * Math.PI * shear / 360.0)
                    ).ToArray();
            return (
                from x in Enumerable.Range(0, image.Width())
                select (
                        from yOffset in Enumerable.Range(0, bottom - top)
                        let xOffset = xOffsetLookup[yOffset]
                        let xNet = x + xOffset
                        where xNet >= 0 && xNet <= image.Width()
                        let yNet = top + yOffset
                        let pixelVal = image.Interpolate(yNet, xNet)
                        let avgVal = pixelVal.R / 255.0
                        select avgVal
                    ).Average()
                ).ToArray();


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
                StrokeThickness = 3
            };
        }
        private static IEnumerable<Line> WordToLines(Word word, Brush brush) {
            var height = word.bottom - word.top;
            var xcorr = height * Math.Tan(2 * Math.PI * word.shear / 360.0);
            //yield return mkLine(word.left, word.top, word.right, word.top, brush);
            yield return mkLine(word.right, word.top, word.right - xcorr, word.bottom, brush);
            //yield return mkLine(word.right - xcorr, word.bottom, word.left - xcorr, word.bottom, brush);
            yield return mkLine(word.left - xcorr, word.bottom, word.left, word.top, brush);
        }
        WordsImage wordsGuess;
        void LoadAnnot() {
            wordsGuess = AnnotLinesParser.GetGuessWord(new FileInfo(System.IO.Path.Combine(HWRsplitter.Program.DataPath, "line_annot.txt")), pageNum, symbolWidth);
            Log("Loaded line_annot and parsed it");
            ProcessLines( wordsGuess, Brushes.Blue);
        }

        void ProcessLines(WordsImage words, Brush brush) {
            ProcessLines(words.textlines.SelectMany(tl => tl.words),brush);
        }
        void ProcessLines(IEnumerable<Word> words, Brush brush) {
            imgWin.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action<IEnumerable<Word>, Brush>(this.ProcessLinesUI), 
                words, brush);
        }

        void ProcessLinesUI(IEnumerable<Word> words, Brush brush) {
            var lines = from word in words
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
