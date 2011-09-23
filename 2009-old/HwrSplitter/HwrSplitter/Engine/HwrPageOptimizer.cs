#define PARALLEL_LEARNING
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmnExtensions;
using HwrDataModel;
using HwrLibCliWrapper;

namespace HwrSplitter.Engine {
	public sealed class HwrPageOptimizer : IDisposable {
		readonly HwrOptimizer optimizer;//IDisposable

		static HwrPageOptimizer() { FeatureDistributionEstimate.FeatureNames = FeatureToString.FeatureNames(); } //handy before save for readability.
		public HwrPageOptimizer(SymbolClasses symbolClasses) {
			if (symbolClasses == null)
				symbolClasses = SymbolClasses.LoadWithFallback(HwrResources.DataDir, HwrResources.CharWidthFile);
			optimizer = new HwrOptimizer(symbolClasses);
		}

		public void Dispose() { optimizer.Dispose(); }

		public HwrPageImage ImproveGuess(HwrPageImage currentHwrPageImage, HwrTextPage betterGuessWords, HwrTextPage nextPage, Action<HwrTextLine> lineProcessed) {
			SaveSymbols(null);//we save the results of the previous page while computing the current page null == use default

			var lineTasks = betterGuessWords.textlines.Select(textLine =>
					Task.Factory.StartNew(() => {
						SymbolLearningData learningCache = optimizer.ConstructLearningCache();

						string linetext = textLine.FullText;
						if (linetext.Length < 25 && textLine.words.All(word => word.leftStat != HwrEndpointStatus.Manual && word.rightStat != HwrEndpointStatus.Manual)) {
							textLine.ProcessorMessage = string.Format("skipped:len=={0}, ", linetext.Length);
						} else {
							int x0 = Math.Max(0, (int)(textLine.OuterExtremeLeft + 0.5));
							int x1 = Math.Min(currentHwrPageImage.Width, (int)(textLine.OuterExtremeRight + 0.5));
							int y0 = (int)(textLine.top + 0.5);
							int y1 = (int)(textLine.bottom + 0.5);

							var croppedLine = currentHwrPageImage.Image.CropTo(x0, y0, x1, y1);
							optimizer.SplitWords(croppedLine, x0, textLine, learningCache);
							//Console.Write("[p{0};l{1}=={2}], ", manager.currentLines.pageNum, line.no, line.ComputedLikelihood);
							learningCache.AssertConsistency("Line: " + linetext);
						}

						lineProcessed(textLine);
						return learningCache;
					}, CancellationToken.None, TaskCreationOptions.None, LowPriorityTaskScheduler.DefaultLowPriorityScheduler)
				).ToArray();


			var allLinesTask = Task.Factory.ContinueWhenAll(lineTasks, lineTasksDone => {
				foreach (var completedLineTask in lineTasksDone) {
					SymbolLearningData learningCache = completedLineTask.Result;
					optimizer.MergeInLearningCache(learningCache); //this also resets the learning cache to 0;
					learningCache.Dispose();
				}
				SymbolClasses.LastPage = currentHwrPageImage.TextPage.pageNum;
			});

			var loadNextPageTask = Task.Factory.StartNew(() => HwrResources.ImageForText(nextPage));

			//current page computed, next page image preloaded, current symbols saved, now time to update symbols!
			//symbols will be saved during next page processing.
			allLinesTask.Wait();
			Console.WriteLine("Finished page {0}; total lines learnt {1}.", betterGuessWords.pageNum, SymbolClasses.Iteration);
			return loadNextPageTask.Result;
		}
		public SymbolClasses SymbolClasses { get { return optimizer.ManagedSymbols; } }
		public void SaveToManaged() { optimizer.SaveToManaged(); }
		/// <param name="dir">null for default</param>
		public void SaveSymbols(DirectoryInfo dir) { SaveToManaged(); SymbolClasses.Save(dir ?? HwrResources.SymbolOutputDir); }

		public Dictionary<char, GaussianEstimate> MakeSymbolWidthEstimate() { return SymbolClasses.Symbol.ToDictionary(sym => sym.Letter, sym => sym.Length); }
	}
}
