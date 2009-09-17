using System;
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
	sealed class TextLineWorker : IDisposable
	{
		sealed class Worker : IDisposable
		{
			TextLineWorker manager;
			SymbolLearningData learningCache;
			Thread thread;
			public Worker(TextLineWorker manager) {
				this.manager = manager;
				learningCache = new SymbolLearningData(manager.optimizer.SymbolCount);
				thread = new Thread(ProcessLines) { IsBackground = true, };
				thread.Start();
			}

			private void ProcessLines() {
				while (true) {
					manager.workStart.WaitOne();
					try {
						if (manager.Running)
							for (int i = manager.ClaimLine(); i < manager.currentLines.Length; i = manager.ClaimLine())
								ProcessLine(manager.currentLines[i]);
						else
							break;
					} finally {
						manager.workDone.Release();
					}
				}
			}

			void ProcessLine(TextLine line) {
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
				manager.optimizer.SplitWords(croppedLine, x0, line, learningCache); //TODO:null should be non-null

				manager.lineDoneEvent(line);
			}

			public void Dispose() {
				learningCache.Dispose();
			}
		}

		HwrOptimizer optimizer;
		Worker[] workers;
		bool running = true;

		HwrPageImage currentImage;
		Action<TextLine> lineDoneEvent;
		TextLine[] currentLines;
		int nextLine = 0;

		object sync = new object();
		Semaphore workDone;
		Semaphore workStart;



		public TextLineWorker(HwrOptimizer optimizer) {
			this.optimizer = optimizer;
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
		}

		int ClaimLine() {
			lock (sync)
				return nextLine++;
		}

		bool Running { get { lock (sync) return running; } }

		public void ImproveGuess(HwrPageImage image, WordsImage betterGuessWords, Action<TextLine> lineProcessed) {
			currentImage = image;
			currentLines = betterGuessWords.textlines;
			lineDoneEvent = lineProcessed;
			workStart.Release(workers.Length);
			for (int i = 0; i < workers.Length; i++)
				workDone.WaitOne();
			//TODO: merge learning data back into model.
		}

	}

	public class TextLineCostOptimizer
	{
		HwrOptimizer nativeOptimizer;
		SymbolClasses symbolClasses;
		TextLineWorker bgWorker;

		public TextLineCostOptimizer() {
			symbolClasses = SymbolClasses.LoadWithFallback(HwrResources.DataDir, HwrResources.CharWidthFile);
			nativeOptimizer = new HwrOptimizer(symbolClasses);
			bgWorker = new TextLineWorker(nativeOptimizer);
		}

		public int NextPage { get { return symbolClasses.NextPage; } }

		static Regex fractionRegex = new Regex(@"\d/\d", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		public void ImproveGuess(HwrPageImage image, WordsImage betterGuessWords, Action<TextLine> lineProcessed) {
			bgWorker.ImproveGuess(image, betterGuessWords, line => {
				Console.Write("[p{0};l{1}=={2}], ", betterGuessWords.pageNum, line.no, line.ComputedLikelihood);
				lineProcessed(line);
			});

			nativeOptimizer.SaveToManaged(); //TODO: syncup!
			symbolClasses.Save(HwrResources.SymbolOutputDir);
		}

		public void ComputeFeatures(HwrPageImage image, TextLine line, out BitmapSource featureImage, out Point offset) {
			int topXoffset;

			int x0 = Math.Max(0, (int)(line.OuterExtremeLeft + 0.5));
			int x1 = Math.Min(image.Width, (int)(line.OuterExtremeRight + 0.5));
			int y0 = (int)(line.top + 0.5);
			int y1 = (int)(line.bottom + 0.5);

			ImageStruct<float> data = ImageProcessor.ExtractFeatures(image.Image.CropTo(x0, y0, x1, y1), line, out topXoffset);
			int featDataY = y0;
			int featDataX = (int)x0 + topXoffset;
			var featImgRGB = data.MapTo(f => (byte)(255.9 * f)).MapTo(b => new PixelArgb32(255, b, b, b));
			foreach (int wordBoundary in
							from word in line.words
							from edge in new[] { word.left, word.right }
							let edgeTrans = (int)(edge + 0.5) - featDataX
							where edgeTrans >= 0 && edgeTrans < featImgRGB.Width
							select edgeTrans) {
				for (int y = 0; y < featImgRGB.Height; y++) {
					var pix = featImgRGB[wordBoundary, y];
					pix.R = 255;
					pix.B = 255;
					featImgRGB[wordBoundary, y] = pix;
				}
			}

			for (int x = 0; x < featImgRGB.Width; x++) { //only useful for scaled version, not for features!
				var pix = featImgRGB[x, line.bodyTop];
				pix.G = 255;
				featImgRGB[x, line.bodyTop] = pix;
				var pixB = featImgRGB[x, line.bodyBot];
				pixB.G = 255;
				featImgRGB[x, line.bodyBot] = pixB;
			}


			featureImage = featImgRGB.MapTo(p => p.Data).ToBitmap();
			featureImage.Freeze();
			offset = new Point(featDataX, featDataY);
		}

		public Dictionary<char, GaussianEstimate> MakeSymbolWidthEstimate() { return symbolClasses.Symbol.ToDictionary(sym => sym.Letter, sym => sym.Length); }

		static TextLineCostOptimizer() { FeatureDistributionEstimate.FeatureNames = FeatureToString.FeatureNames(); }

	}
}
