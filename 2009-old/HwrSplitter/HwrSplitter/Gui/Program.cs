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
	class Program : Application
	{
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


		HwrPageImage hwrImg;//TODO:rename
		MainWindow mainWindow;
		int? imgForceNum;

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


		//unused as of now.
		public void LoadWords() {
			var xmlWords = XDocument.Load(wordsFileInfo.OpenText());
			manager.words = new WordsImage(xmlWords.Root);
			Log("Loaded .words file");
			manager.ImageAnnotater.ProcessLines(manager.words.textlines);
		}


		public void LoadImage() {
			hwrImg = new HwrPageImage(imageFileInfo);
			manager.SetImage(hwrImg);
			//Log("Image loaded: " + hwrImg.Width + "x" + hwrImg.Height);
		}

		void LoadInBackground() {
			manager.optimizer = LoadSymbolClasses();
			LoadAnnot();


			var imgNumStrs = (from filepath in Directory.GetFiles(HwrDataModel.Program.ImgPath, "NL_HaNa_H2_7823_*.tif")
							 let filename = System.IO.Path.GetFileName(filepath)
							 let m = Regex.Match(filename, @"^NL_HaNa_H2_7823_(?<num>\d+).tif$")
							 where m.Success
							 let numstr = m.Groups["num"].Value
							 let num = int.Parse(numstr)
							 where annot_lines.ContainsKey(num)
							 select numstr).ToArray();

			while(true)
			foreach (var possiblePage in imgNumStrs) {

				string imgPath = System.IO.Path.Combine(HwrDataModel.Program.ImgPath, "NL_HaNa_H2_7823_" + possiblePage + ".tif");
				imageFileInfo = new FileInfo(imgPath);

				//Log("Chose page:" + int.Parse(possiblePage));


				LoadImage();

				//            LoadWords();
				manager.words = annot_lines[int.Parse(possiblePage)];

				manager.ImageAnnotater.ProcessLines(manager.words.textlines);

				manager.optimizer.ImproveGuess(hwrImg, manager.words, line => {
					manager.ImageAnnotater.ProcessLine(line);
				});
				//mainWindow.Dispatcher.Invoke((Action)mainWindow.Close);
				using (Stream stream = new FileInfo(
						System.IO.Path.Combine(System.IO.Path.Combine(HwrDataModel.Program.DataPath, "words-train"), "NL_HaNa_H2_7823_" + possiblePage + ".wordsguess")
					).OpenWrite())
				using (TextWriter writer = new StreamWriter(stream))
					writer.Write(manager.words.AsXml().ToString());
			}
		}


		private TextLineCostOptimizer LoadSymbolClasses() {
			var symbolClasses = SymbolClassParser.Parse(new FileInfo(System.IO.Path.Combine(HwrDataModel.Program.DataPath, "char-width.txt")), TextLineCostOptimizer.CharPhases);

			return new TextLineCostOptimizer(symbolClasses);
		}

		private void Log(string logmsg) {
			Console.WriteLine(logmsg);
		}

		Dictionary<int, WordsImage> annot_lines;
		void LoadAnnot() {
			var annotFile = new FileInfo(System.IO.Path.Combine(HwrDataModel.Program.DataPath, "line_annot.txt"));

			//var annotLines = AnnotLinesParser.GetAnnotLines(annotFile);
			//Console.WriteLine("Lines higher than 255: {0} of {1}",  annotLines.Where(al => al.bottom - al.top > 255).Count(),annotLines.Length);
			Console.Write("Loading line_annot...");
			annot_lines = AnnotLinesParser.GetGuessWords(annotFile, num => true, manager.optimizer.MakeSymbolWidthEstimate());
			Console.WriteLine("done.");

			//ProcessLines( wordsGuess, Brushes.Blue);
		}


	}
}
