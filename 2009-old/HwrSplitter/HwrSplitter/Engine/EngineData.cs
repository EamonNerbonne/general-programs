using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmnExtensions.DebugTools;
using EmnExtensions;
using EmnExtensions.Threading;
using HwrDataModel;
using HwrSplitter.Gui;

namespace HwrSplitter.Engine {
	public sealed class EngineData : IDisposable {
		HwrPageOptimizer pageoptimizer;
		Dictionary<int, HwrTextPage> annot_lines;
		int[] pages;
		int[] trainingPages;
		int pageIndex;
		readonly MainManager mainManager;
		public EngineData(MainManager mainManager) { this.mainManager = mainManager; }

		public void Load() {
			HwrTextPage[] trainingData = null;
			//TODO:parallelizable:
			using (new DTimer("parInvoke"))
				Par.Invoke(
					() => {
						annot_lines = AnnotLinesParser.GetGuessWords(HwrResources.LineAnnotFile);
						pages = HwrResources.ImagePages.Where(num => annot_lines.ContainsKey(num)).ToArray();
					},
					() => { pageoptimizer = new HwrPageOptimizer(null); },//null==use default
					() => {
						trainingData = HwrResources.WordsTrainingExamples.ToArray();
						trainingPages = trainingData.Select(data => data.pageNum).ToArray();
					});

			foreach (var wordsImage in trainingData)
				if (annot_lines.ContainsKey(wordsImage.pageNum))
					annot_lines[wordsImage.pageNum].SetFromManualExample(wordsImage);
			var symbolWidthLookup = pageoptimizer.MakeSymbolWidthEstimate();
			foreach (var wordsImage in annot_lines.Values)
				wordsImage.EstimateWordBoundariesViaSymbolLength(symbolWidthLookup);
		}

		HwrPageImage LearnInBackground(HwrPageImage currentPageImage, HwrTextPage nextPage) {
			mainManager.DisplayPage(currentPageImage);

			HwrPageImage precachedNextPage =
				pageoptimizer.ImproveGuess(currentPageImage, currentPageImage.TextPage, nextPage, line => mainManager.ImageAnnotater.BackgroundLineUpdate(line));


			using (Stream stream = HwrResources.WordsGuessFile(currentPageImage.TextPage.pageNum).OpenWrite())
			using (TextWriter writer = new StreamWriter(stream))
				writer.Write(currentPageImage.TextPage.AsXml().ToString());
			return precachedNextPage;
		}

		public void Dispose() { if (pageoptimizer != null) pageoptimizer.Dispose(); }

		public void StartLearning() {
			var pageset2use = trainingPages;

			var firstPageIndex = pageoptimizer.SymbolClasses.LastPage == 0 ? 50 :
				pageset2use.IndexOf(p => p > pageoptimizer.SymbolClasses.LastPage);
			pageIndex = (firstPageIndex < 0 ? 0 : firstPageIndex) % pageset2use.Length;

			HwrPageImage currentPageImage = HwrResources.ImageForText(annot_lines[pageset2use[pageIndex]]);

			while (true) {
				int nextPageIndex = (pageIndex + 1) % pageset2use.Length;
				HwrTextPage nextPage = annot_lines[pageset2use[nextPageIndex]];

				try {
					currentPageImage = LearnInBackground(currentPageImage, nextPage);
				} catch (Exception e) {
					Console.WriteLine("Learning failed on page {0} with the following Exception:\n{1}", currentPageImage.TextPage.pageNum, e);
				}

				mainManager.WaitWhilePaused();
				pageIndex = nextPageIndex;
			}
		}
	}
}
