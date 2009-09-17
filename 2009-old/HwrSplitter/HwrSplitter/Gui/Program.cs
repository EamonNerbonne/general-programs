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
using EmnExtensions.Filesystem;
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



		void LoadInBackground() {
			manager.optimizer = new TextLineCostOptimizer();
			LoadAnnot();


			var imgNumStrs = (from filepath in HwrResources.ImageDir.GetFiles("NL_HaNa_H2_7823_*.tif")
							  let m = Regex.Match(filepath.Name, @"^NL_HaNa_H2_7823_(?<num>\d+).tif$")
							  where m.Success
							  let numstr = m.Groups["num"].Value
							  let num = int.Parse(numstr)
							  where annot_lines.ContainsKey(num)
							  select numstr).ToArray();

			imgNumStrs = imgNumStrs.Where(page => int.Parse(page) > manager.optimizer.StartPastPage)
				.Concat(imgNumStrs.Where(page => int.Parse(page) <= manager.optimizer.StartPastPage))
				.ToArray();//cycle the pages so we start learning past where we last learnt from.



			while (true) {
				foreach (var possiblePage in imgNumStrs) {
					imageFileInfo = HwrResources.ImageDir.GetRelativeFile("NL_HaNa_H2_7823_" + possiblePage + ".tif");

					//Log("Chose page:" + int.Parse(possiblePage));

					manager.PageImage = new HwrPageImage(imageFileInfo);


					//            LoadWords();
					manager.words = annot_lines[int.Parse(possiblePage)];


					WordsImage handChecked=null;
					FileInfo trainFile = HwrResources.WordsTrainDir.GetRelativeFile("NL_HaNa_H2_7823_" + possiblePage + ".words");
					try {
						if (trainFile.Exists) {
							handChecked = new WordsImage(trainFile);
							foreach(var line in handChecked.textlines)
								foreach (var word in line.words) 
									word.rightStat = word.leftStat = word.topStat = word.botStat = Word.TrackStatus.Manual;
						}
					} catch (Exception e) { Console.WriteLine(e.ToString()); }//if this fails, we don't use the manually entered xml.

					manager.optimizer.LocateLineBodies(manager.PageImage, manager.words);

					manager.words.SetFromManualExample(handChecked);

					manager.ImageAnnotater.ProcessLines(manager.words.textlines);

					manager.optimizer.ImproveGuess(manager.PageImage, manager.words, line => {
						manager.ImageAnnotater.ProcessLine(line);
					});

					manager.words.MarkIfManual(handChecked);

					//mainWindow.Dispatcher.Invoke((Action)mainWindow.Close);
					using (Stream stream = HwrResources.WordsGuessDir.GetRelativeFile("NL_HaNa_H2_7823_" + possiblePage + ".wordsguess").OpenWrite())
					using (TextWriter writer = new StreamWriter(stream))
						writer.Write(manager.words.AsXml().ToString());
					manager.WaitWhilePaused();
				}

			}
		}


		private void Log(string logmsg) {
			Console.WriteLine(logmsg);
		}

		Dictionary<int, WordsImage> annot_lines;
		void LoadAnnot() {
			var annotFile = HwrResources.DataDir.GetRelativeFile("line_annot.txt");

			//var annotLines = AnnotLinesParser.GetAnnotLines(annotFile);
			//Console.WriteLine("Lines higher than 255: {0} of {1}",  annotLines.Where(al => al.bottom - al.top > 255).Count(),annotLines.Length);
			Console.Write("Loading line_annot...");
			annot_lines = AnnotLinesParser.GetGuessWords(annotFile, num => true, manager.optimizer.MakeSymbolWidthEstimate());
			Console.WriteLine("done.");

			//ProcessLines( wordsGuess, Brushes.Blue);
		}


	}
}
