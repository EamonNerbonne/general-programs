﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;
using EmnExtensions;
using EmnExtensions.DebugTools;
using EmnExtensions.Filesystem;
using HwrDataModel;
using HwrLibCliWrapper;
using MoreLinq;

namespace HwrSplitter.Engine
{
	public sealed class HwrPageOptimizer : IDisposable
	{
		sealed class Worker : IDisposable
		{
			HwrPageOptimizer manager;
			public readonly SymbolLearningData learningCache;
			public readonly Thread thread;
			public Worker(HwrPageOptimizer manager) {
				this.manager = manager;
				learningCache = manager.optimizer.ConstructLearningCache();
				thread = new Thread(ProcessLines) { IsBackground = true, };
				thread.Start();
			}

			private void ProcessLines() {
				while (true) {
					manager.workStart.WaitOne();
					try {
						if (manager.Running)
							for (int i = manager.ClaimLine(); i < manager.currentLines.textlines .Length; i = manager.ClaimLine())
								ProcessLine(manager.currentLines.textlines[i]);
						else
							break;
					} finally {
						manager.workDone.Release();
					}
				}
			}

			public void ProcessLine(TextLine line) {
				string linetext = line.FullText;
				if (linetext.Length < 30) {
					Console.Write("skipped:len=={0}, ", linetext.Length);
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
				Console.Write("[p{0};l{1}=={2}], ", manager.currentLines.pageNum, line.no, line.ComputedLikelihood);
				manager.lineDoneEvent(line);
			}

			public void Dispose() {
				learningCache.Dispose();
			}
		}

		HwrOptimizer optimizer;//IDisposable
		Worker[] workers;//IDisposable[]
		bool running = true;

		HwrPageImage currentImage;
		WordsImage currentLines;
		int nextLine = 0;
		Action<TextLine> lineDoneEvent;

		object sync = new object();
		Semaphore workDone;//IDisposable
		Semaphore workStart;//IDisposable

		static Regex fractionRegex = new Regex(@"\d/\d", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

		int ClaimLine() { lock (sync)	 return nextLine++; }

		bool Running { get { lock (sync) return running; } }
		static HwrPageOptimizer() { FeatureDistributionEstimate.FeatureNames = FeatureToString.FeatureNames(); } //handy before save for readability.
		public HwrPageOptimizer(SymbolClasses symbolClasses = null) {
			if (symbolClasses == null)
				symbolClasses = SymbolClasses.LoadWithFallback(HwrResources.DataDir, HwrResources.CharWidthFile);
			optimizer = new HwrOptimizer(symbolClasses);

			int parallelCount = Math.Max(1, System.Environment.ProcessorCount);

			workers = new Worker[parallelCount];
			workDone = new Semaphore(0, parallelCount);
			workStart = new Semaphore(0, parallelCount);
			for (int i = 0; i < parallelCount; i++)
				workers[i] = new Worker(this);
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

		public void ImproveGuess(HwrPageImage image, WordsImage betterGuessWords, Action<TextLine> lineProcessed) {
			currentImage = image;
			currentLines = betterGuessWords;
			lineDoneEvent = lineProcessed;
#if !DEBUG
			workStart.Release(workers.Length);
			for (int i = 0; i < workers.Length; i++)
				workDone.WaitOne();
#else
			foreach (var line in betterGuessWords.textlines)
				workers[0].ProcessLine(line);
#endif
			this.currentImage = null;
			this.currentLines = null;
			this.lineDoneEvent = null;

			foreach (Worker worker in workers) {
				optimizer.MergeInLearningCache(worker.learningCache); //this also resets the learning cache to 0;
			}
		}
		public SymbolClasses SymbolClasses { get { return optimizer.ManagedSymbols; } }
		public void SaveToManaged() { optimizer.SaveToManaged(); }
		public void Save(DirectoryInfo dir = null) { SaveToManaged(); optimizer.ManagedSymbols.Save(dir ?? HwrResources.SymbolOutputDir); }

		public int NextPage { get { return SymbolClasses.NextPage; } }
		public Dictionary<char, GaussianEstimate> MakeSymbolWidthEstimate() { return SymbolClasses.Symbol.ToDictionary(sym => sym.Letter, sym => sym.Length); }
	}

}
