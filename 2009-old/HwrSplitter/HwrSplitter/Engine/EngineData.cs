using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HwrDataModel;
using System.Text.RegularExpressions;
using HwrSplitter.Gui;
using System.IO;

namespace HwrSplitter.Engine
{
	public sealed class EngineData : IDisposable
	{
		HwrPageOptimizer optimizer;
		Dictionary<int, WordsImage> annot_lines;
		int[] pages;
		private MainManager mainManager;
		public EngineData(MainManager mainManager) { this.mainManager = mainManager; }

		public void Load() {
			//TODO:parallelizable:
			optimizer = new HwrPageOptimizer(null);//null==use default
			annot_lines = AnnotLinesParser.GetGuessWords(HwrResources.LineAnnotFile);
			WordsImage[] trainingData = HwrResources.WordsTrainingExamples.ToArray();
			//barrier
			pages = HwrResources.ImagePages.Where(num => annot_lines.ContainsKey(num)).ToArray();
			foreach (var wordsImage in trainingData)
				if (annot_lines.ContainsKey(wordsImage.pageNum))
					annot_lines[wordsImage.pageNum].SetFromManualExample(wordsImage);
			var symbolWidthLookup = optimizer.MakeSymbolWidthEstimate();
			foreach (var wordsImage in annot_lines.Values)
				wordsImage.EstimateWordBoundariesViaSymbolLength(symbolWidthLookup);


		}

		public void LearnInBackground(int page) {
			HwrPageImage pageImage = HwrResources.ImageFile(page);
			mainManager.PageImage = pageImage;

			WordsImage words = annot_lines[page];
			mainManager.words = words;

			TextLineYPosition.LocateLineBodies(pageImage, words);

			mainManager.ImageAnnotater.ProcessLines(mainManager.words.textlines);

			optimizer.ImproveGuess(pageImage, words, line => {
				mainManager.ImageAnnotater.ProcessLine(line);
			});

			optimizer.SymbolClasses.NextPage = page + 1;
			optimizer.Save(null);//null == use default

			using (Stream stream = HwrResources.WordsGuessFile(page).OpenWrite())
			using (TextWriter writer = new StreamWriter(stream))
				writer.Write(words.AsXml().ToString());
		}


		public void Dispose() { if (optimizer != null) optimizer.Dispose(); }

		public void StartLearning() {
			int[] pagesInLearningOrder = pages.Where(pageNum => pageNum >= optimizer.NextPage).Concat(pages.Where(pageNum => pageNum < optimizer.NextPage)).ToArray();
			optimizer.Save(null);//null == use default -- to check for initialization errors.

			while (true) {
				foreach (int pageNum in pagesInLearningOrder) {
					try {
						LearnInBackground(pageNum);
					} catch (Exception e) {
						Console.WriteLine("Learning failed on page {0} with the following Exception:\n{1}", pageNum, e);
					}
					mainManager.WaitWhilePaused();
				}

			}

		}
	}
}
