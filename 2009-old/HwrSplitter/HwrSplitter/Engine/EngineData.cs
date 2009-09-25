using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HwrDataModel;
using System.Text.RegularExpressions;
using HwrSplitter.Gui;
using System.IO;
using System.Threading;
using EmnExtensions.Threading;
using EmnExtensions.DebugTools;

namespace HwrSplitter.Engine
{
	public sealed class EngineData : IDisposable
	{
		HwrPageOptimizer optimizer;
		Dictionary<int, HwrTextPage> annot_lines;
		int[] pages;
		int pageIndex;
		private MainManager mainManager;
		public EngineData(MainManager mainManager) { this.mainManager = mainManager; }

		public void Load() {
			HwrTextPage[] trainingData = null;
			//TODO:parallelizable:
			using (new DTimer("parInvoke"))
				Par.Invoke(
					() => { annot_lines = AnnotLinesParser.GetGuessWords(HwrResources.LineAnnotFile); pages = HwrResources.ImagePages.Where(num => annot_lines.ContainsKey(num)).ToArray(); },
					() => { optimizer = new HwrPageOptimizer(null); },//null==use default
					() => { trainingData = HwrResources.WordsTrainingExamples.ToArray(); });

			foreach (var wordsImage in trainingData)
				if (annot_lines.ContainsKey(wordsImage.pageNum))
					annot_lines[wordsImage.pageNum].SetFromManualExample(wordsImage);
			var symbolWidthLookup = optimizer.MakeSymbolWidthEstimate();
			foreach (var wordsImage in annot_lines.Values)
				wordsImage.EstimateWordBoundariesViaSymbolLength(symbolWidthLookup);
		}

		private HwrPageImage LearnInBackground(HwrPageImage currentPageImage, HwrTextPage nextPage) {
			mainManager.DisplayPage(currentPageImage);

			HwrPageImage precachedNextPage =
				optimizer.ImproveGuess(currentPageImage, currentPageImage.TextPage, nextPage, line => {
					mainManager.ImageAnnotater.BackgroundLineUpdate(line);
				});

			

			using (Stream stream = HwrResources.WordsGuessFile(currentPageImage.TextPage.pageNum).OpenWrite())
			using (TextWriter writer = new StreamWriter(stream))
				writer.Write(currentPageImage.TextPage.AsXml().ToString());
			return precachedNextPage;
		}


		public void Dispose() { if (optimizer != null) optimizer.Dispose(); }

		public void StartLearning() {
			var firstPageIndex = pages.Select((pageNum, index) => new { PageNum = pageNum, Index = index }).FirstOrDefault(p => p.PageNum > optimizer.SymbolClasses.LastPage);
			pageIndex = firstPageIndex == null ? 0 : firstPageIndex.Index;

			HwrPageImage currentPageImage = HwrResources.ImageForText(annot_lines[pages[pageIndex]]);

			while (true) {
				int nextPageIndex = (pageIndex + 1) % pages.Length;
				HwrTextPage nextPage = annot_lines[pages[nextPageIndex]];

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
