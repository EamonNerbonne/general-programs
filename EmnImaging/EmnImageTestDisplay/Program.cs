﻿using System;
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
            Program app;
            if (args.Length > 0)
                app = new Program(args[0]);
            else
                app = new Program();
            app.Exec();
        }

        public void ChooseImage() {
            var imgNumStrs = from filepath in Directory.GetFiles(HWRsplitter.Program.ImgPath, "NL_HaNa_H2_7823_*.tif")
                             let filename = System.IO.Path.GetFileName(filepath)
                             let m = Regex.Match(filename, @"^NL_HaNa_H2_7823_(?<num>\d+).tif$")
                             where m.Success
                             let numstr = m.Groups["num"].Value
                             select numstr;
            /*var wordNumStrs = from filepath in Directory.GetFiles(
                                  System.IO.Path.Combine(HWRsplitter.Program.DataPath, "words-train"), "NL_HaNa_H2_7823_*.words")
                              let filename = System.IO.Path.GetFileName(filepath)
                              let m = Regex.Match(filename, @"^NL_HaNa_H2_7823_(?<num>\d+).words$")
                              where m.Success
                              let numstr = m.Groups["num"].Value
                              select numstr;
            var bothNumStrs = imgNumStrs.Intersect(wordNumStrs).ToArray();


            string wordsPath = System.IO.Path.Combine(HWRsplitter.Program.DataPath, @"words-train\NL_HaNa_H2_7823_" + numStr + ".words");
            wordsFileInfo = new FileInfo(wordsPath);

            */
            if(imgForceNum.HasValue)
            numStr = imgNumStrs.First(numStrs=>int.Parse(numStrs) == imgForceNum.Value);
            else
                numStr = imgNumStrs.First(s=>54<=int.Parse(s));

            string imgPath = System.IO.Path.Combine(HWRsplitter.Program.ImgPath, "NL_HaNa_H2_7823_" + numStr + ".tif");
            imageFileInfo = new FileInfo(imgPath);

            pageNum = int.Parse(numStr);
            Log("Chose page:" + pageNum);
        }
        FileInfo wordsFileInfo=null, imageFileInfo;
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
            if (true) {
                var imgBack = (float[,])image.Clone();
                foreach (int i in Enumerable.Range(0, 6)) {
                    image = BoxBlurV(image); UpdateImage();
                }
                Log("blurred.");
                var imgBlur = (float[,])image.Clone();

                Invert(image); UpdateImage();


                foreach (int i in Enumerable.Range(0, 30)) {
                    image = MinV (image); UpdateImage();
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
        int? imgForceNum;
        public Program(string imgNum):this() {
            imgForceNum = int.Parse(imgNum);
        }
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
            blurWindowXDir = mkBlurWindow(7);
            blurWindowYDir = mkBlurWindow(21);
        }


        const double lenCostFactor = 3;
        const double posCostFactor = 1.0/1500;
        const int margin = 75;
        const int rShift = 15;
        const double InterWordCostFactor = 1.0/50;
        const double intermedBrightnessCostFactor = 5;
        const double endPointCostFactor = 2;
        const int MaxBodyLines = 65;
        const double MaxBodyBrightness = 0.63;
        const double SpaceDarknessPower = 3;
        const double WordLightnessPower = 3;
//        const double WordLightn
        double[] blurWindowXDir, blurWindowYDir;
        public struct CostSummary {
            public double
                lengthErr, posErr, spaceErr,
                spaceDarkness, wordLightness;
            public double Total { get { return lengthErr + posErr + spaceErr + spaceDarkness + wordLightness; } }
            public static CostSummary operator +(CostSummary a, CostSummary b) {
                return new CostSummary {
                    lengthErr = a.lengthErr + b.lengthErr,
                    posErr = a.posErr + b.posErr,
                    spaceErr = a.spaceErr + b.spaceErr,
                    spaceDarkness = a.spaceDarkness + b.spaceDarkness,
                    wordLightness = a.wordLightness + b.wordLightness
                };
            }
            public static CostSummary operator -(CostSummary a, CostSummary b) {
                return new CostSummary {
                    lengthErr = a.lengthErr - b.lengthErr,
                    posErr = a.posErr - b.posErr,
                    spaceErr = a.spaceErr - b.spaceErr,
                    spaceDarkness = a.spaceDarkness - b.spaceDarkness,
                    wordLightness = a.wordLightness - b.wordLightness
                };
            }

            public override string ToString() {
                return string.Format("spaceE: {2:f2}, spaceDark: {3:f2}, wordLight: {4:f2}\nlenE: {0:f2}, posE: {1:f2}, TOT: {5:f2}",
                    lengthErr, posErr, spaceErr,
                spaceDarkness, wordLightness,Total
                    );
            }
        }


        void ImproveGuess() {
            InitializeBlurWindows(); //to reduce noise, and encourage distance margins.

            List<TextLine> betterGuessLine = new List<TextLine>();
            foreach (TextLine lineGuess in wordsGuess.textlines) {

                TextLine improvedGuess = new TextLine();

                int lineXBegin = (int)Math.Floor(lineGuess.left);
                int lineXEnd = (int)Math.Ceiling(lineGuess.right);
                int lineYBegin = (int)Math.Floor(lineGuess.top);
                int lineYEnd = (int)Math.Ceiling(lineGuess.bottom);

                int relativeBodyYEnd, relativeBodyYBegin;
                FindTextLineBody(lineXBegin, lineXEnd, lineYBegin, lineYEnd, out relativeBodyYBegin, out relativeBodyYEnd, improvedGuess);

                //the shear-shift specifies how much the line-top will be right-shifted since it's the line body that's
                //centered in the line.
                int shearShift = (int)(relativeBodyYBegin * Math.Tan(2 * Math.PI * lineGuess.shear / 360.0) + 0.5)
                    +rShift;//since we see more stupid things at start than end.


                double[] processedShearedBodySum, processedShearedSum;
                FindContrastStretchedShearedSums(lineXBegin, lineXEnd, lineYBegin, lineYEnd, relativeBodyYBegin, relativeBodyYEnd, lineGuess.shear, out processedShearedSum, out processedShearedBodySum, improvedGuess);


                Func<int, int, double> GetAverageBetween = CreateMemoizedAverager(processedShearedBodySum);



                int numWords = lineGuess.words.Length;
                int lineLength = lineXEnd - lineXBegin + 2*margin;//TODO, this should probably be a little larger.
                double[,] endCosts = new double[numWords, lineLength];
                double[,] beginCosts = new double[numWords, lineLength];

                ///The endcost of a word and a point is the cost of ending that word at that point,
                ///including costs for all following words.
                ///The startcost of a word and a point is the cost of starting that word at that point,
                ///including costs for ending and all following words.
                ///point 0 is at lineXBegin+shearShift - margin.


                //we initialize by doing the endpoint of the last word.
                int wordIndex = numWords - 1;
                for (int i = 0; i < lineLength; i++) {
                    int imgPos = i + lineXBegin + shearShift - margin;
                    //int posDiff = lineXEnd + shearShift - imgPos;
                    endCosts[wordIndex, i] =
                       // posDiff * (double)posDiff / posCostFactor/posCostFactor + //TODO, isn't really posCostFactor
                        (1 - processedShearedSum[imgPos]) * endPointCostFactor;
                }

                for (int j = numWords - 1; j >= 0; j--) {
                    //we'll treat j's start and j-1's end in this iteration. 0's _begin_ is a little special...

                    //begin costs:
                    wordIndex = j;
                    Word word = lineGuess.words[wordIndex];
                    double targetLength = word.symbolBasedLength.len;
                    double target2Pos = word.left + word.right;
                    for (int i = 0; i < lineLength; i++) {
                        int imgPos = i + lineXBegin + shearShift - margin;
                        double bestEndCosts = double.MaxValue;
                        for (int k = i + 1; k < lineLength; k++) {
                            int imgPosE = k + lineXBegin + shearShift - margin;
                            int FewPercent = (imgPosE - imgPos) / 50;
                            double wordLenScaled = (imgPosE - imgPos - targetLength) / (targetLength + 2) * lenCostFactor;
                            double word2PosScaled = (imgPos + imgPosE - target2Pos) * posCostFactor;
                            double wordLightness = GetAverageBetween(imgPos + FewPercent, imgPosE - FewPercent) * intermedBrightnessCostFactor; ;
                            double endingHereCost =
                                endCosts[wordIndex, k] +
                                wordLenScaled * wordLenScaled  +
                                word2PosScaled * word2PosScaled +
                                wordLightness;
                            if (endingHereCost < bestEndCosts)
                                bestEndCosts = endingHereCost;
                        }

                        beginCosts[wordIndex, i] =
                            bestEndCosts +
                            (1 - processedShearedSum[imgPos]) * endPointCostFactor;
                    }

                    if (wordIndex == 0) {
                        //OK, we just calculated the begin costs of 0, but we should add costs for beginning far from the
                        //line beginning...
                        break;// there's no end of -1 here... ;-)
                    }


                    //end costs:
                    wordIndex = j - 1;
                    word = lineGuess.words[wordIndex];
                    for (int i = 0; i < lineLength; i++) {
                        int imgPos = i + lineXBegin + shearShift - margin;
                        double bestBeginCosts = double.MaxValue;
                        for (int k = i + 1; k < lineLength; k++) {
                            int imgPosB = k + lineXBegin + shearShift - margin;
                            double spaceLenScaled = (imgPosB - imgPos) * InterWordCostFactor;
                            double beginningHereCost =
                                beginCosts[wordIndex + 1, k] +
                                (1-GetAverageBetween(imgPos, imgPosB)) * intermedBrightnessCostFactor +
                                spaceLenScaled * spaceLenScaled;
                            if (beginningHereCost < bestBeginCosts)
                                bestBeginCosts = beginningHereCost;
                        }

                        endCosts[wordIndex, i] =
                            bestBeginCosts +
                            (1 - processedShearedSum[imgPos]) * endPointCostFactor;
                    }
                }

                //OK, we did precalculation! now we just follow the cheapest path!
                wordIndex = 0;
                double bestBeginCost = double.MaxValue;
                int bestBeginPos=-1;
                for (int i = 0; i < lineLength; i++) {
                    if (beginCosts[wordIndex, i] < bestBeginCost) {
                        bestBeginCost = beginCosts[wordIndex, i];
                        bestBeginPos = i;
                    }
                }

                improvedGuess.cost = bestBeginCost;
                CostSummary summary = new CostSummary();
                CostSummary lastHit = new CostSummary();
                CostSummary lastSummary = new CostSummary();

                double bestEndCost = double.MaxValue;
                int bestEndPos = -1;

                List<Word> betterGuessWord = new List<Word>();
                for (int j = 0; j < numWords; j++) {


                    wordIndex = j;
                    //we have a beginpos, now find the matching end.
                    //that's not the cheapest end, since that not might be reachable!
                    //that's the end whose endcost+transition costs are the lowest.

                    bestEndCost = double.MaxValue;
                    bestEndPos = -1;
                    Word word = lineGuess.words[wordIndex];
                    double targetLength = word.symbolBasedLength.len;
                    double target2Pos = word.left + word.right;
                    int imgPos = bestBeginPos + lineXBegin + shearShift - margin;

                    for (int k = 0; k < lineLength; k++) {
                        int imgPosE = k + lineXBegin + shearShift - margin;
                        int FewPercent = (imgPosE - imgPos) / 50;

                        double wordLenScaled = (imgPosE - imgPos - targetLength) / (targetLength + 2) * lenCostFactor;
                        double word2PosScaled = (imgPos + imgPosE - target2Pos) * posCostFactor;
                        double wordLightness = GetAverageBetween(imgPos + FewPercent, imgPosE - FewPercent) * intermedBrightnessCostFactor;
                        double endingHereCost =
                            endCosts[wordIndex, k] +
                            wordLenScaled * wordLenScaled +
                            word2PosScaled * word2PosScaled +
                             wordLightness;
                        if (endingHereCost < bestEndCost) {
                            bestEndCost = endingHereCost;
                            bestEndPos = k;

                            lastHit.lengthErr = wordLenScaled * wordLenScaled ;
                            lastHit.posErr = word2PosScaled * word2PosScaled;
                            lastHit.wordLightness = wordLightness;
                        }
                    }


                    summary.spaceDarkness += (1 - processedShearedSum[bestBeginPos + lineXBegin + shearShift - margin]) * endPointCostFactor;
                    summary.spaceDarkness += (1 - processedShearedSum[bestEndPos + lineXBegin + shearShift - margin]) * endPointCostFactor;
                   // summary.spaceDarkness += lastHit.spaceDarkness;
                    summary.lengthErr += lastHit.lengthErr;
                    summary.posErr += lastHit.posErr;
                    summary.spaceErr += lastHit.spaceErr;
                    summary.wordLightness += lastHit.wordLightness;

                    //beginCosts[wordIndex, bestBeginPos] == (1 - processedShearedSum[imgPos]) * endPointCostFactor - bestEndCost

                    //we have an optimal find!
                    betterGuessWord.Add(
                        new Word(word) {
                            costSummary = summary - lastSummary,
                            
                            left = bestBeginPos + lineXBegin + shearShift - margin,
                            right = bestEndPos + lineXBegin + shearShift - margin
                        });
                    ProcessLines(Enumerable.Repeat(betterGuessWord[betterGuessWord.Count - 1], 1), Brushes.Red);

                    lastSummary = summary;
                    //now find next beginning

                    wordIndex = j + 1;
                    if (wordIndex == numWords)
                        break;//found em all.
                    bestBeginCost = double.MaxValue;
                    bestBeginPos = -1;
                    word = lineGuess.words[wordIndex];
                    imgPos = bestEndPos + lineXBegin + shearShift;

                    for (int k = bestEndPos + 1; k < lineLength; k++) {
                        int imgPosB = k + lineXBegin + shearShift;
                        double spaceLenScaled = (imgPosB - imgPos) * InterWordCostFactor;
                        double beginningHereCost =
                            beginCosts[wordIndex, k] +
                            (1 - GetAverageBetween(imgPos, imgPosB)) * intermedBrightnessCostFactor +
                            spaceLenScaled * spaceLenScaled;
                        if (beginningHereCost < bestBeginCost) {
                            bestBeginCost = beginningHereCost;
                            bestBeginPos = k;
                            lastHit.spaceDarkness = (1 - GetAverageBetween(imgPos, imgPosB)) * intermedBrightnessCostFactor;
                            lastHit.spaceErr = spaceLenScaled * spaceLenScaled;
                        }
                    }
                    //endCosts[wordIndex-1, bestEndPos] == (1 - processedShearedSum[imgPos]) * endPointCostFactor - bestBeginCost
                }

                /*
                    lastEnd = bestGuessEnd;
                    lastEndWeight = 1.0 / InterWordCostFactor;
                */

                improvedGuess.words = betterGuessWord.ToArray();
                improvedGuess.shear = lineGuess.shear;
                improvedGuess.bottom = lineGuess.bottom;
                improvedGuess.top = lineGuess.top;
                improvedGuess.left = lineGuess.left;
                improvedGuess.right = lineGuess.right;
                improvedGuess.no = lineGuess.no;
                improvedGuess.costSummary = summary;


                betterGuessLine.Add(improvedGuess);
            }
            betterGuessWords = new WordsImage {
                name = wordsGuess.name,
                pageNum = wordsGuess.pageNum,
                textlines = betterGuessLine.ToArray()
            };
            using (Stream stream = new FileInfo(
                System.IO.Path.Combine(System.IO.Path.Combine(HWRsplitter.Program.DataPath, "words-train"), "NL_HaNa_H2_7823_" + numStr + ".wordsguess")
                ).OpenWrite()) 
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
                        nextSumRow[i] = currentSumRow[i] + Math.Pow( data[(i + nextSumLength) % data.Length],WordLightnessPower);
                    
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
            int relevantXBegin = lineXBegin - margin,
                relevantXEnd = lineXEnd + margin;

            //calculate the average intensity of each (sheared) column inside the entire line body:
            double[] shearedSum = ShearedSum(lineYBegin, lineYBegin, lineYEnd, shear);

            //calculate the average intensity of each (sheared) column inside the main section of the line body:
            double[] shearedBodySum = ShearedSum(lineYBegin, lineYBegin + relativeBodyYBegin, lineYBegin + relativeBodyYEnd, shear);

            shearedSum = shearedSum.Select((x, i) => x + 2*shearedBodySum[i]).Select(x=>Math.Pow(x,SpaceDarknessPower)).ToArray();//do weight center a little more.
            processedShearedSum = ContrastStretchAndBlur(shearedSum, blurWindowXDir, relevantXBegin, relevantXEnd);
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

            return blurredLine.Select(x => (x - min) / (max - min)).ToArray();
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
