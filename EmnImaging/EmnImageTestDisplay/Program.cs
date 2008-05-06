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
using System.Windows.Input;

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
        int pageNum;
        WordsImage words;

        public void LoadWords() {
            var xmlWords = XDocument.Load(wordsFileInfo.OpenText());
            words = new WordsImage(xmlWords.Root);
            Log("Loaded .words file");
            ProcessLines( words, Brushes.DarkRed);
        }
        float[,] image;
        public void LoadImage() {
            image = ImageIO.Load(imageFileInfo).AsGreyscale();



            UpdateImage();
            if (false) {
                var imgBack = (float[,])image.Clone();
                foreach (int i in Enumerable.Range(0, 3)) {
                    image = BoxBlur(image); UpdateImage();
                }
                Log("blurred.");
                var imgBlur = (float[,])image.Clone();

                Invert(image); UpdateImage();


                foreach (int i in Enumerable.Range(0, 10)) {
                    image = BoxMin(image); UpdateImage();
                }
                Log("local maximum found");
                Invert(image);
                var maxImg = image;

                image = imgBack;


                foreach (var y in image.Yrange())
                    foreach (var x in image.Xrange()) {
                        image[y, x] = image[y, x] / maxImg[y, x];
                    }
                UpdateImage();

                Log("subtracted");
                foreach (var y in image.Yrange())
                    foreach (var x in image.Xrange()) {
                        image[y, x] = Math.Max(0, Math.Min(1, image[y, x]));
                    }
                UpdateImage();

                MaxContrast(image); UpdateImage();
                Log("contrast stretched");


                foreach (var y in image.Yrange())
                    foreach (var x in image.Xrange()) {
                        image[y, x] = Math.Max(0, image[y, x] * 3 - 2);
                    }
                UpdateImage();

            }
            
            Log("Image loaded: " + image.Width() + "x" + image.Height());
        }
        LogControl logControl;
        ZoomRect zoomRect;
        MainWindow mainWindow;
        ImageAnnotViewbox imgAnnotViewbox;
        public Program() {
            mainWindow = new MainWindow();
            imgAnnotViewbox = mainWindow.ImageAnnotViewbox;
            logControl = mainWindow.LogControl;
            zoomRect = mainWindow.ZoomRect;
            zoomRect.ToZoom = imgAnnotViewbox.ImageCanvas;
            mainWindow.WordDetail.ToZoom = imgAnnotViewbox.ImageCanvas;
            Console.SetOut(new DelegateTextWriter(s => { logControl.AppendThreadSafe(s); }));
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        public void Exec() { this.Run(mainWindow); } //TODO:MainWindow.
        protected override void OnStartup(StartupEventArgs e) {
                       base.OnStartup(e);
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
            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(SetupWordDetail));

        }

        private void SetupWordDetail() {
            ImageAnnotViewbox imgView = mainWindow.ImageAnnotViewbox;
            imgView.MouseLeftButtonDown += new MouseButtonEventHandler(imgView_MouseLeftButtonDown);
            imgView.MouseLeave += new MouseEventHandler(imgView_MouseLeave);
        }

        void imgView_MouseLeave(object sender, MouseEventArgs e) {
            zoomRect.ShowNewPoint(lastClickPoint);
        }

        struct TextLineComparer : IComparer<TextLine> {

            //return 0 on any overlap;
            public int Compare(TextLine x, TextLine y) {
                if(x.bottom < y.top) return -1;
                if (x.top > y.bottom) return 1;
                return 0;
            }

        }
        struct WordHorizComparer : IComparer<Word> {

            //return 0 on any overlap;
            public int Compare(Word x, Word y) {
                if (x.right < y.left) return -1;
                if (x.left > y.right) return 1;
                return 0;
            }
        }
        Point lastClickPoint;
        void imgView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            lastClickPoint = e.MouseDevice.GetPosition(mainWindow.ImageAnnotViewbox.ImageCanvas);
            int textLineIndex= Array.BinarySearch(betterGuessWords.textlines,
                new TextLine {
                    top = lastClickPoint.Y,
                    bottom = lastClickPoint.Y
                }, new TextLineComparer());
            if (textLineIndex >= 0) {
                TextLine target = betterGuessWords.textlines[textLineIndex];
                //fix shear:
                double yoff = lastClickPoint.Y - target.top;
                double shearEffect = yoff * Math.Tan(-2 * Math.PI * target.shear / 360.0);
                double correctedX = lastClickPoint.X - shearEffect;

                int wordIndex = Array.BinarySearch(target.words, new Word {
                    right = correctedX,
                    left = correctedX
                }, new WordHorizComparer());

                if (wordIndex >= 0) {
                    Word targetWord = target.words[wordIndex];

                    mainWindow.WordDetail.WordDisplay(imgAnnotViewbox, target, targetWord);

                }
            }
        }

        //0 is begin line, 10 is line end, and each word includes one 32 for space.  Unknown letters get 1.
        Dictionary<char, SymbolWidth> symbolWidth;
        private void LoadSymbolWidths() {
            symbolWidth = SymbolWidthParser.Parse(new FileInfo(System.IO.Path.Combine(HWRsplitter.Program.DataPath, "char-width.txt")));
        }

        static double[] mkBlurWindow(int length) {
            if (length % 2 != 1) throw new ArgumentException("must make odd sized convolution window for centering reasons.");
            var blurWindow = Enumerable.Range(-length/2, length).Select(v => Math.Exp(-10 * Math.Abs(v)/(double)length ));
            var blurWindowSum = blurWindow.Sum();
            return blurWindow.Select(x => x / blurWindowSum).ToArray();
        }

        void InitializeBlurWindows() {
            blurWindowXDir = mkBlurWindow(81);
            blurWindowYDir = mkBlurWindow(21);
        }


        const double lenCostFactor = 100;
        const int posCostFactor = 400;
        const double LineStartCostFactor = 100;
        const double InterWordCostFactor = 50;
        const double intermedBrightnessCostFactor = 2;
        const double endPointCostFactor = 2;
        const int MaxBodyLines = 60;
        const double MaxBodyBrightness = 0.6;
        double[] blurWindowXDir, blurWindowYDir;


        void ImproveGuess() {
            InitializeBlurWindows(); //to reduce noise, and encourage distance margins.

            List<TextLine> betterGuessLine = new List<TextLine>();
            foreach (TextLine lineGuess in wordsGuess.textlines) {

                TextLine improvedGuess = new TextLine();

                int lineXBegin = (int)Math.Floor(lineGuess.left );
                int lineXEnd = (int)Math.Ceiling (lineGuess.right);
                int lineYBegin = (int)Math.Floor(lineGuess.top);
                int lineYEnd = (int)Math.Ceiling(lineGuess.bottom);
                
                int relativeBodyYEnd, relativeBodyYBegin;
                FindTextLineBody(lineXBegin, lineXEnd, lineYBegin, lineYEnd, out relativeBodyYBegin, out relativeBodyYEnd, improvedGuess);

                //the shear-shift specifies how much the line-top will be right-shifted since it's the line body that's
                //centered in the line.
                int shearShift = (int)(relativeBodyYBegin * Math.Tan(2 * Math.PI * lineGuess.shear / 360.0) + 0.5);


                double[] processedShearedBodySum,processedShearedSum;
                FindContrastStretchedShearedSums(lineXBegin, lineXEnd, lineYBegin, lineYEnd, relativeBodyYBegin, relativeBodyYEnd, lineGuess.shear, out processedShearedSum, out processedShearedBodySum, improvedGuess);


                Func<int, int, double> GetAverageBetween = CreateMemoizedAverager(processedShearedBodySum);



             //   int numWords = lineGuess.words.Length;

              //  double[,] endCosts = new double[numWords,lineXEnd-lineXBegin];
             //   double[,] startCosts = new double[numWords, lineXEnd - lineXBegin];

                ///The endcost of a word and a point is the cost of ending that word at that point,
                ///including costs for all following words.
                ///The startcost of a word and a point is the cost of starting that word at that point,
                ///including costs for ending and all following words.

                double lastEnd=lineXBegin+shearShift;
                
                double lastEndWeight=1.0/LineStartCostFactor;
                List<Word> betterGuessWord = new List<Word>();
                foreach (Word wordGuess in lineGuess.words) {
                    double targetStart = wordGuess.left;
                    double targetEnd = wordGuess.right;
                    double targetLen = wordGuess.symbolBasedLength.len;
                    double targetPos = targetStart + targetEnd+shearShift;
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
                            double intermedBrightness = GetAverageBetween(tryStart, tryEnd);//lower better, TODO, offset?
                            double cost = 2*endPointCostFactor+
                                lenDiff * lenDiff
                                + posDiff * posDiff
                                + Math.Abs(tryStart - lastEnd) * lastEndWeight
                                + intermedBrightness * intermedBrightnessCostFactor
                                - (processedShearedBodySum[tryEnd] + processedShearedBodySum[tryStart]) * endPointCostFactor;

                            if (cost < bestGuessCost) {
                                bestGuessStart = tryStart;
                                bestGuessEnd = tryEnd;
                                bestGuessCost = cost;
                            }
                        }

                    }
                    betterGuessWord.Add(
                        new Word(wordGuess) {
                            imageBasedCost = bestGuessCost,
                            lookaheadSum = GetAverageBetween(bestGuessStart, bestGuessEnd),
                            startLightness= processedShearedSum[bestGuessStart],
                            endLightness = processedShearedSum[bestGuessEnd],
                            left = bestGuessStart,
                            right = bestGuessEnd

                            });
                    ProcessLines(Enumerable.Repeat(betterGuessWord[betterGuessWord.Count - 1], 1), Brushes.Green);
                    lastEnd = bestGuessEnd;
                    lastEndWeight = 1.0 / InterWordCostFactor;

                }
                
                
                    improvedGuess.words = betterGuessWord.ToArray();
                    improvedGuess.shear = lineGuess.shear;
                    improvedGuess.bottom = lineGuess.bottom;
                    improvedGuess.top = lineGuess.top;
                    improvedGuess.left = lineGuess.left;
                    improvedGuess.right = lineGuess.right;
                    improvedGuess.no = lineGuess.no;
                    improvedGuess.shearedsum = processedShearedSum.Cast<float>().ToArray();
                    improvedGuess.shearedbodysum = processedShearedBodySum.Cast<float>().ToArray();

                
                betterGuessLine.Add(improvedGuess);
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

        private static Func<int, int, double> CreateMemoizedAverager(double[] data) {
            
            List<double[]> sumByLength = new List<double[]> {data};
            //sumByLength[offset][start]  contains the sum of  'offset;+1 elements starting at index 'start'


            return (start, end) => {
                int length = end - start;
                if (length < 1) return 0;
                while (sumByLength.Count < length) {
                    int nextSumLength = sumByLength.Count;
                    double[] currentSumRow = sumByLength[nextSumLength - 1];
                    double[] nextSumRow = new double[data.Length];

                    for (int i = 0; i < currentSumRow.Length; i++)
                        nextSumRow[i] = currentSumRow[i] + data[(i + nextSumLength) % data.Length];
                    
                    sumByLength.Add(nextSumRow);
                }
                return sumByLength[length-1][start] / length; //calc average too!
            };

        }
        WordsImage betterGuessWords;

        void FindTextLineBody(int lineXBegin, int lineXEnd, int lineYBegin, int lineYEnd, out int relativeBodyYBegin, out int relativeBodyYEnd, TextLine improvedGuess) {
            double[] rowAvg = new double[lineYEnd - lineYBegin];
            for (int y = lineYBegin; y < lineYEnd; y++) {
                for (int x = lineXBegin; x < lineXEnd; x++) { // a little tight around line ends....
                    rowAvg[y - lineYBegin] += image[y, x];
                }
                rowAvg[y - lineYBegin] /= lineXEnd - lineXBegin;
            }
            rowAvg = BlurLine(rowAvg, blurWindowYDir);

            double rowAvgMin = double.MaxValue, rowAvgMax = double.MinValue;
            int rowAvgMinIndex = -1;
            foreach (int i in Enumerable.Range(0, rowAvg.Length)) {
                if (rowAvg[i] < rowAvgMin) {
                    rowAvgMin = rowAvg[i];
                    rowAvgMinIndex = i;
                }
                if (rowAvg[i] > rowAvgMax) rowAvgMax = rowAvg[i];
            }
            var rowAvgRange = rowAvgMax - rowAvgMin;
            var rowAvgContrastStretched = rowAvg.Select(x => (x - rowAvgMin) / rowAvgRange).ToArray();
            //ideally now, somewhere near the baseline the color will be darkest and that's where we'll look most.

            relativeBodyYBegin = rowAvgMinIndex;
            relativeBodyYEnd = rowAvgMinIndex;
            while (relativeBodyYEnd - relativeBodyYBegin < MaxBodyLines - 1 && relativeBodyYEnd < lineYEnd - lineYBegin - 1 && relativeBodyYBegin > 0) {
                if (rowAvgContrastStretched[relativeBodyYEnd + 1] < rowAvgContrastStretched[relativeBodyYBegin - 1]) {
                    if (rowAvgContrastStretched[relativeBodyYEnd + 1] > MaxBodyBrightness)
                        break;
                    else
                        relativeBodyYEnd++;
                } else {
                    if (rowAvgContrastStretched[relativeBodyYBegin - 1] > MaxBodyBrightness)
                        break;
                    else
                        relativeBodyYBegin--;
                }
            }

            improvedGuess.rowsum = rowAvgContrastStretched.Cast<float>().ToArray();
            improvedGuess.bodyTop = relativeBodyYBegin;
            improvedGuess.bodyBot = relativeBodyYEnd;
        }

        void FindContrastStretchedShearedSums(int lineXBegin, int lineXEnd, int lineYBegin, int lineYEnd, int relativeBodyYBegin, int relativeBodyYEnd, double shear, out double[] processedShearedSum, out double[] processedShearedBodySum, TextLine improvedGuess) {
            int relevantXBegin = lineXBegin - posCostFactor,
                relevantXEnd = lineXEnd + posCostFactor;

            //calculate the average intensity of each (sheared) column inside the entire line body:
            double[] shearedSum = ShearedSum(lineYBegin, lineYBegin, lineYEnd, shear);
            processedShearedSum = ContrastStretchAndBlur(shearedSum, blurWindowXDir, relevantXBegin, relevantXEnd);

            //calculate the average intensity of each (sheared) column inside the main section of the line body:
            double[] shearedBodySum = ShearedSum(lineYBegin, lineYBegin + relativeBodyYBegin, lineYBegin + relativeBodyYEnd, shear);
            processedShearedBodySum = ContrastStretchAndBlur(shearedBodySum, blurWindowXDir, relevantXBegin, relevantXEnd);

            improvedGuess.shearedbodysum = processedShearedBodySum.Cast<float>().ToArray();
            improvedGuess.shearedsum = processedShearedSum.Cast<float>().ToArray();
        }

        static double[] ContrastStretchAndBlur(double[] line, double[] blurWindow, int relevantXBegin, int relevantXEnd) {
            double average = line.Skip(relevantXBegin).Take(relevantXEnd - relevantXBegin).Average();
            for (int i = 0; i < relevantXBegin; i++)
                line[i] = average;
            for (int i = relevantXEnd; i < line.Length; i++)
                line[i] = average;
            double[] blurredLine = BlurLine(line, blurWindow);
            double min = blurredLine.Min(), max = blurredLine.Max();

            return line.Select(x => (x - min) / (max - min)).ToArray();
        }


        static double[] BlurLine(double[] data, double[] window) {
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

        double[] ShearedSum(int shearTop, int top, int bottom, double shear) {
            var xOffsetLookup = Enumerable.Range(top-shearTop, bottom - top).Select(
                yOffset => -yOffset * Math.Tan(2 * Math.PI * shear / 360.0)
                    ).ToArray();
            var height = bottom - top;
            var midpoint = height/2.0;
            var divFactor = 
                Enumerable.Range(0, height)
                .Select(y => (y - midpoint) / midpoint)
                .Select(x => 1 - x * x)
                .Sum();
            divFactor = height;
            return (
                from x in Enumerable.Range(0, image.Width())
                select (
                        from yOffset in Enumerable.Range(0, height)
                        let xOffset = xOffsetLookup[yOffset]
                        let xNet = Math.Min(Math.Max(0,x + xOffset),image.Width()-1)
                        let yRelFromMid = (yOffset-midpoint)/midpoint
                        let pixelVal = image.Interpolate(top+yOffset, xNet)
                        let avgVal = pixelVal
                        select avgVal //*(1-yRelFromMid*yRelFromMid)
                    ).Sum()/divFactor
                ).ToArray();
        }


        private void Log(string logmsg) {
            logControl.AppendLineThreadSafe(logmsg);
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
                StrokeThickness = 2
            };
        }
        private static IEnumerable<Line> WordToLines(Word word, Brush brush) {
            var xcorr = word.BottomXOffset;
            //yield return mkLine(word.left, word.top, word.right, word.top, brush);
            yield return mkLine(word.right, word.top, word.right + xcorr, word.bottom, brush);
            //yield return mkLine(word.right + xcorr, word.bottom, word.left + xcorr, word.bottom, brush);
            yield return mkLine(word.left + xcorr, word.bottom, word.left, word.top, brush);
        }
        WordsImage wordsGuess;
        void LoadAnnot() {
            wordsGuess = AnnotLinesParser.GetGuessWord(new FileInfo(System.IO.Path.Combine(HWRsplitter.Program.DataPath, "line_annot.txt")), pageNum, symbolWidth);
            Log("Loaded line_annot and parsed it");
            //ProcessLines( wordsGuess, Brushes.Blue);
        }

        void ProcessLines(WordsImage words, Brush brush) {
            ProcessLines(words.textlines.SelectMany(tl => tl.words),brush);
        }
        void ProcessLines(IEnumerable<Word> words, Brush brush) {
            imgAnnotViewbox.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action<IEnumerable<Word>, Brush>(this.ProcessLinesUI), 
                words, brush);
        }

        void ProcessLinesUI(IEnumerable<Word> words, Brush brush) {
            var lines = from word in words
                        from line in WordToLines(word, brush)
                        select line;
            imgAnnotViewbox.AddShapes(lines.Cast<UIElement>());
        }

        void UpdateImage() {
            imgAnnotViewbox.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action<float[,]>(imgAnnotViewbox.SetImage),image);
        }

        public static Func<T, T> RepeatFunc<T>(Func<T, T> f, int num) {
            return (t => { while (num-- > 0) t = f(t); return t; });
        }
        public static void Invert(float[,] image) {
            foreach (var y in image.Yrange())
                foreach (var x in image.Xrange()) {
                    image[y, x] = 1.0f - image[y,x];
                }
        }
        public static float[,] BoxBlur(float[,] image) {
            return BoxBlurH(BoxBlurV(image));
        }

        public static float[,] BoxBlurV(float[,] image) {
            float[,] retval = (float[,])image.Clone();
            for (int x = 0; x < image.Width(); x++) {
                retval[0, x] = (image[0, x] + image[1, x])/2.0f;
            }

            for (int y = 1; y < image.Height() - 1; y++)
                for (int x = 0; x < image.Width(); x++) {
                    retval[y, x] = (image[y - 1, x] + image[y, x] + image[y + 1, x])/3.0f;
                }

            for (int x = 0; x < image.Width(); x++) {
                retval[image.Height() - 1, x] = (image[image.Height() - 2, x] + image[image.Height() - 1, x])/2.0f;
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
                    retval[y, x] = (image[y, x - 1] + image[y, x] + image[y, x + 1])/3;
                }

            for (int y = 0; y < image.Height(); y++) {
                retval[y, image.Width() - 1] = (image[y, image.Width() - 2] + image[y, image.Width() - 1])/2;
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

                    retval[y, x] = Math.Min(image[y, x-1], Math.Min(image[y, x], image[y, x+1]));

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

                    retval[y, x] = Math.Min(Math.Min(image[y, x], image[y-1, x]), image[y+1, x]);

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


        public static void MaxContrast(float[,] image) {
            var minMax = image.AsEnumerable().Aggregate(
                new {
                    Max = 0.0f,
                    Min = 1.0f
                },
                (cur, pix) =>
                new {
                    Max = Math.Max(cur.Max, pix ),
                    Min = Math.Min(cur.Min, pix )
                });
            var range = minMax.Max - minMax.Min;
            //  range = PixelArgb32.Combine(range, new PixelArgb32(1, 1, 1, 1), Math.Max);
            foreach(var y in image.Yrange())
                foreach(var x in image.Xrange())
                  image[y, x] = range == 0 ? 1.0f : (image[y,x] - minMax.Min) / range;

        }
    }
}
