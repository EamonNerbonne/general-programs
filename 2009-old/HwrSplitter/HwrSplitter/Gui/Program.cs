using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using HwrDataModel;
using System.Xml.Linq;
using System.IO;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using EmnExtensions.DebugTools;
using EmnExtensions.Text;
using System.Windows.Input;
using HwrSplitter.Engine;

namespace HwrSplitter.Gui
{
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

        MainManager manager;
        FileInfo wordsFileInfo = null, imageFileInfo;
        string numStr;
        int pageNum;
        HwrPageImage hwrImg;//TODO:rename
        MainWindow mainWindow;
        int? imgForceNum;
        WordsImage words;

        public Program(string imgNum)
            : this() {
            imgForceNum = int.Parse(imgNum);
        }
        public Program() {
            mainWindow = new MainWindow();
            manager = mainWindow.Manager;
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;//TODO:add save warning.
        }

        public void Exec() { this.Run(mainWindow); } //TODO:MainWindow.
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            Thread worker = new Thread(this.LoadInBackground);
            worker.Name = "WorkerHWR";
            worker.IsBackground = true; //if background processing has not completed, exit anyway.
            //worker.Priority = ThreadPriority.BelowNormal;
            worker.Start();
            Log("Started Worker Thread");
        }

        public void ChooseImage() {
            var imgNumStrs = from filepath in Directory.GetFiles(HwrDataModel.Program.ImgPath, "NL_HaNa_H2_7823_*.tif")
                             let filename = System.IO.Path.GetFileName(filepath)
                             let m = Regex.Match(filename, @"^NL_HaNa_H2_7823_(?<num>\d+).tif$")
                             where m.Success
                             let numstr = m.Groups["num"].Value
                             select numstr;
            /*var wordNumStrs = from filepath in Directory.GetFiles(
                                  System.IO.Path.Combine(DataIO.Program.DataPath, "words-train"), "NL_HaNa_H2_7823_*.words")
                              let filename = System.IO.Path.GetFileName(filepath)
                              let m = Regex.Match(filename, @"^NL_HaNa_H2_7823_(?<num>\d+).words$")
                              where m.Success
                              let numstr = m.Groups["num"].Value
                              select numstr;
            var bothNumStrs = imgNumStrs.Intersect(wordNumStrs).ToArray();


            string wordsPath = System.IO.Path.Combine(DataIO.Program.DataPath, @"words-train\NL_HaNa_H2_7823_" + numStr + ".words");
            wordsFileInfo = new FileInfo(wordsPath);

            */
            if(imgForceNum.HasValue)
            numStr = imgNumStrs.First(numStrs=>int.Parse(numStrs) == imgForceNum.Value);
            else
                numStr = imgNumStrs.First(s=>54<=int.Parse(s));

            string imgPath = System.IO.Path.Combine(HwrDataModel.Program.ImgPath, "NL_HaNa_H2_7823_" + numStr + ".tif");
            imageFileInfo = new FileInfo(imgPath);

            pageNum = int.Parse(numStr);
            Log("Chose page:" + pageNum);
        }
        
        //unused as of now.
        public void LoadWords() {
            var xmlWords = XDocument.Load(wordsFileInfo.OpenText());
            words = new WordsImage(xmlWords.Root);
            Log("Loaded .words file");
            manager.ImageAnnotater.ProcessLines( words.textlines);
        }


        public void LoadImage() {
            hwrImg = new HwrPageImage(imageFileInfo);
            manager.SetImage(hwrImg);
            Log("Image loaded: " + hwrImg.Width + "x" + hwrImg.Height);
        }

        void LoadInBackground() {
            LoadSymbolWidths();
			manager.optimizer = new TextLineCostOptimizer( symbolWidth);

            ChooseImage();
            LoadImage();

            //            LoadWords();
            LoadAnnot();
            manager.ImageAnnotater.ProcessLines(words.textlines);
			
			manager.optimizer.ImproveGuess(hwrImg, words, line =>
			{
                manager.ImageAnnotater.ProcessLine(line);
            });
			//mainWindow.Dispatcher.Invoke((Action)mainWindow.Close);
            using (Stream stream = new FileInfo(
                    System.IO.Path.Combine(System.IO.Path.Combine(HwrDataModel.Program.DataPath, "words-train"), "NL_HaNa_H2_7823_" + numStr + ".wordsguess")
                ).OpenWrite())
            using (TextWriter writer = new StreamWriter(stream))
                writer.Write(words.AsXml().ToString());
        }


        //0 is begin line, 10 is line end, and each word includes one 32 for space.  Unknown letters get 1.
        SymbolWidth[] symbolWidth;
        private void LoadSymbolWidths() {
            symbolWidth = SymbolWidthParser.Parse(new FileInfo(System.IO.Path.Combine(HwrDataModel.Program.DataPath, "char-width.txt")));
        }

        private void Log(string logmsg) {
            Console.WriteLine(logmsg);
        }
        void LoadAnnot() {
            var annotFile = new FileInfo(System.IO.Path.Combine(HwrDataModel.Program.DataPath, "line_annot.txt"));
            var annotLines = AnnotLinesParser.GetAnnotLines(annotFile);
            Console.WriteLine("Lines higher than 255: {0} of {1}",  annotLines.Where(al => al.bottom - al.top > 255).Count(),annotLines.Length);

            words = AnnotLinesParser.GetGuessWord(annotFile, pageNum, symbolWidth.ToDictionary(symW=>symW.c) );
            manager.words = words;

            Log("Loaded line_annot and parsed it");
            //ProcessLines( wordsGuess, Brushes.Blue);
        }


    }
}
