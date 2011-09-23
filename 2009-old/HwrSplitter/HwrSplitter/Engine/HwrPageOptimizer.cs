﻿#define PARALLEL_LEARNING
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using HwrDataModel;
using HwrLibCliWrapper;

namespace HwrSplitter.Engine {
	public sealed class HwrPageOptimizer : IDisposable {
		sealed class Worker : IDisposable {
			readonly HwrPageOptimizer manager;
			readonly int workerIndex;
			public readonly SymbolLearningData learningCache;
			readonly Thread thread;
			bool active ;
			public Worker(HwrPageOptimizer manager, int workerIndex) {
				this.manager = manager;
				learningCache = manager.optimizer.ConstructLearningCache();
				thread = new Thread(ProcessLines) { IsBackground = true };
				thread.Start();
				this.workerIndex = workerIndex;
			}

			void ProcessLines() {
				while (true) {
					thread.Priority = workerIndex == 0 ? ThreadPriority.Normal : workerIndex == 1 ? ThreadPriority.BelowNormal : ThreadPriority.Lowest;
					manager.workStart.WaitOne();
					lock (manager.sync)
						active = true;
					try {
						learningCache.AssertConsistency("Initialization.");
						if (manager.Running)
							for (int i = manager.ClaimLine(); i < manager.currentLines.textlines.Length; i = manager.ClaimLine()) {
								ProcessLine(manager.currentLines.textlines[i]);
								learningCache.AssertConsistency("Line: " + manager.currentLines.textlines[i].FullText);
							} else
							break;
						HwrTextPage nextPage;
						lock (manager.sync) {
							nextPage = manager.m_nextPage;
							manager.m_nextPage = null;
						}
						if (nextPage != null)
							manager.cachedNextPage = HwrResources.ImageForText(nextPage);
					} catch (Exception e) {
						Console.WriteLine(e);
					} finally {
						lock (manager.sync) {
							active = false;
							if (thread.Priority != ThreadPriority.Lowest)
								for (int i = workerIndex + 1; i < manager.workers.Length; i++)
									if (manager.workers[i].active) {
										manager.workers[i].thread.Priority = thread.Priority;
										break;
									}
						}
						manager.workDone.Release();
					}
				}
			}

			void ProcessLine(HwrTextLine line) {
				string linetext = line.FullText;
				if (linetext.Length < 25 && !line.words.Where(word => word.leftStat == HwrEndpointStatus.Manual || word.leftStat == HwrEndpointStatus.Manual).Any()) {
					line.ProcessorMessage = string.Format("skipped:len=={0}, ", linetext.Length);
					return;
				}

#if LOGLINESPEED
			NiceTimer timer = new NiceTimer();
			timer.TimeMark("preparing lineguess");
#endif
				int x0 = Math.Max(0, (int)(line.OuterExtremeLeft + 0.5));
				int x1 = Math.Min(manager.currentImage.Width, (int)(line.OuterExtremeRight + 0.5));
				int y0 = (int)(line.top + 0.5);
				int y1 = (int)(line.bottom + 0.5);

				var croppedLine = manager.currentImage.Image.CropTo(x0, y0, x1, y1);
#if LOGLINESPEED
			timer.TimeMark(null);
#endif
				manager.optimizer.SplitWords(croppedLine, x0, line, learningCache);
				//Console.Write("[p{0};l{1}=={2}], ", manager.currentLines.pageNum, line.no, line.ComputedLikelihood);
				manager.lineDoneEvent(line);
			}

			public void Dispose() {
				learningCache.Dispose();
			}
		}

		readonly HwrOptimizer optimizer;//IDisposable
		readonly Worker[] workers;//IDisposable[]
		bool running = true;

		HwrPageImage currentImage;
		HwrTextPage currentLines;
		int nextLine;
		Action<HwrTextLine> lineDoneEvent;

		readonly object sync = new object();
		readonly Semaphore workDone;//IDisposable
		readonly Semaphore workStart;//IDisposable

		int ClaimLine() { lock (sync)	 return nextLine++; }

		bool Running { get { lock (sync) return running; } }
		static HwrPageOptimizer() { FeatureDistributionEstimate.FeatureNames = FeatureToString.FeatureNames(); } //handy before save for readability.
		public HwrPageOptimizer(SymbolClasses symbolClasses) {
			if (symbolClasses == null)
				symbolClasses = SymbolClasses.LoadWithFallback(HwrResources.DataDir, HwrResources.CharWidthFile);
			optimizer = new HwrOptimizer(symbolClasses);

			int parallelCount = Math.Max(1, Environment.ProcessorCount);

			workers = new Worker[parallelCount];
			workDone = new Semaphore(0, parallelCount);
			workStart = new Semaphore(0, parallelCount);
			for (int i = 0; i < parallelCount; i++)
				workers[i] = new Worker(this, i);
		}

		public void Dispose() {
			lock (sync)
				running = false;
			workStart.Release(workers.Length);
			for (int i = 0; i < workers.Length; i++)
				workDone.WaitOne();

			workDone.Close();
			workStart.Close();

			for (int i = 0; i < workers.Length; i++)
				workers[i].Dispose();
			optimizer.Dispose();
		}
		HwrTextPage m_nextPage;
		HwrPageImage cachedNextPage;
		public HwrPageImage ImproveGuess(HwrPageImage image, HwrTextPage betterGuessWords, HwrTextPage nextPage, Action<HwrTextLine> lineProcessed) {
			currentImage = image;
			currentLines = betterGuessWords;
			m_nextPage = nextPage;
			cachedNextPage = null;
			nextLine = 0;
			lineDoneEvent = lineProcessed;
#if PARALLEL_LEARNING
			workStart.Release(workers.Length);
			SaveSymbols(null);//we save the results of the previous page while computing the current page null == use default
			for (int i = 0; i < workers.Length; i++)
				workDone.WaitOne();
#else
			SaveSymbols(null);//null == use default
			while(nextLine <  betterGuessWords.textlines.Length)
				workers[0].ProcessLine(betterGuessWords.textlines[nextLine]);
			if(nextPage!=null)
				cachedNextPage = HwrResources.ImageForText(nextPage);
#endif
			currentImage = null;
			currentLines = null;
			lineDoneEvent = null;

			//current page computed, next page image preloaded, current symbols saved, now time to update symbols!

			//All SymbolClass modifications START!
			foreach (Worker worker in workers)
				optimizer.MergeInLearningCache(worker.learningCache); //this also resets the learning cache to 0;
			SymbolClasses.LastPage = image.TextPage.pageNum;
			//All SymbolClass modifications END!
			//symbols will be saved during next page processing.

			Console.WriteLine("Finished page {0}; total lines learnt {1}.", betterGuessWords.pageNum, optimizer.ManagedSymbols.Iteration);
			return cachedNextPage;
		}
		public SymbolClasses SymbolClasses { get { return optimizer.ManagedSymbols; } }
		public void SaveToManaged() { optimizer.SaveToManaged(); }
		/// <param name="dir">null for default</param>
		public void SaveSymbols(DirectoryInfo dir) { SaveToManaged(); optimizer.ManagedSymbols.Save(dir ?? HwrResources.SymbolOutputDir); }

		public Dictionary<char, GaussianEstimate> MakeSymbolWidthEstimate() { return SymbolClasses.Symbol.ToDictionary(sym => sym.Letter, sym => sym.Length); }
	}
}
