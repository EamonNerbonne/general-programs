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
			public Worker(TextLineWorker manager)
			{
				this.manager = manager;
				learningCache = new SymbolLearningData(manager.optimizer.SymbolCount);
				thread = new Thread(ProcessLines) { IsBackground = true, };
				thread.Start();
			}

			private void ProcessLines()
			{
				while (true)
				{
					manager.workStart.WaitOne();
					try
					{
						if (manager.Running)
							for (int i = manager.ClaimLine(); i < manager.currentLines.Length; i = manager.ClaimLine())
								ProcessLine(manager.currentLines[i]);
						else
							break;
					}
					finally
					{
						manager.workDone.Release();
					}
				}
			}

			void ProcessLine(TextLine line)
			{
				string linetext = line.FullText;
				if (linetext.Length < 30)
				{
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

			public void Dispose()
			{
				learningCache.Dispose();
			}
		}

		HwrOptimizer optimizer;
		Worker[] workers;
		bool running = true;

		HwrPageImage currentImage;
		TextLine[] currentLines;
		Action<TextLine> lineDoneEvent;
		int nextLine = 0;
		int linesDone = 0;

		object sync = new object();
		Semaphore workDone;
		Semaphore workStart;



		public TextLineWorker(HwrOptimizer optimizer)
		{
			int parallelCount = Math.Max(1, System.Environment.ProcessorCount);
			workers = new Worker[parallelCount];
			workDone = new Semaphore(0, parallelCount);
			workStart = new Semaphore(0, parallelCount);
			for (int i = 0; i < parallelCount; i++)
				workers[i] = new Worker(this);
		}

		public void Dispose()
		{
			lock (sync)
				running = false;
			workStart.Release(workers.Length);
			for (int i = 0; i < workers.Length; i++)
				workDone.WaitOne();

			workDone.Close();
			workStart.Close();
		}

		int ClaimLine()
		{
			lock (sync)
				return nextLine++;
		}

		bool Running { get { lock (sync) return running; } }

		public void ImproveGuess(HwrPageImage image, WordsImage betterGuessWords, Action<TextLine> lineProcessed)
		{
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
		static TextLineCostOptimizer()
		{
			FeatureDistributionEstimate.FeatureNames = FeatureToString.FeatureNames();
		}
		HwrOptimizer nativeOptimizer;
		SymbolClasses symbolClasses;
		TextLineWorker bgWorker;

		public TextLineCostOptimizer()
		{
			symbolClasses = SymbolClasses.LoadWithFallback(HwrResources.SymbolDir, HwrResources.CharWidthFile);
			nativeOptimizer = new HwrOptimizer(symbolClasses);
			bgWorker = new TextLineWorker(nativeOptimizer);
		}

		static void BoxBlur(double[] arr, int window)
		{
			double[] cum = new double[arr.Length + 1];
			double sum = 0.0;
			for (int i = 0; true; i++)
			{
				cum[i] = sum;
				if (i == arr.Length) break;
				sum += arr[i];
			}

			int botWindow = window / 2, topWindow = (window + 1) / 2;
			for (int i = 0; i < arr.Length; i++)
			{
				int minI = Math.Max(0, i - botWindow), maxI = Math.Min(cum.Length - 1, i + topWindow);
				arr[i] = (cum[maxI] - cum[minI]) / (maxI - minI);
			}
		}

		public void LocateLineBodies(HwrPageImage image, WordsImage betterGuessWords)
		{
			image.ComputeXProjection((int)(betterGuessWords.textlines[0].left + 0.5), (int)(betterGuessWords.textlines[0].right + 0.5));
			BoxBlur(image.XProjectionSmart, 4);
			BoxBlur(image.XProjectionSmart, 4);
			BoxBlur(image.XProjectionSmart, 4);//sideeffect!
			LocateLineBodiesImpl(image.XProjectionSmart, betterGuessWords);
		}

		//public struct Range { public int start, end;}
		static void LocateLineBodiesImpl(double[] xProjection, WordsImage betterGuessWords)
		{
			double[] cum = new double[xProjection.Length + 1];
			double sum = 0.0;
			for (int i = 0; true; i++)
			{
				cum[i] = sum;
				if (i == xProjection.Length) break;
				sum += xProjection[i];
			}

			for (int lineI = 0; lineI < betterGuessWords.textlines.Length; lineI++)
			{
				TextLine textLine = betterGuessWords.textlines[lineI];
				int y0 = (int)(textLine.top + 0.5);
				int y1 = (int)(textLine.bottom + 0.5);
				var origProjection = xProjection.Skip(y0).Take(y1 - y0);

				double origMean = (cum[y1] - cum[y0]) / (y1 - y0);
				double orig95Percentile = origProjection.OrderBy(x => x).ToArray()[(int)((y1 - y0) * 0.95)];
				double threshold = (6 * orig95Percentile * 0.40 + 4 * origMean * 0.95) / 10;


				var biggestHighDensitySection =
					xProjection.Skip(y0).Take(y1 - y0) //select the pixel rows of the current line
					.Select(density => density > threshold ? 1 : 0) // 1 where high density, 0 where low density
					.Scan((cursum, current) => cursum * current + current) //accumulate: value == number of consecutive high-density rows
					.Select((densityRunLength, relativeLineNum) => new { Line = y0 + relativeLineNum, DensityRunLength = densityRunLength }) //add line index
					.Aggregate((lineA, lineB) => lineA.DensityRunLength > lineB.DensityRunLength ? lineA : lineB); //select maximal run of high-density lines.

				int extraLength = Math.Max(40 - biggestHighDensitySection.DensityRunLength, 0);

				int highDens0 = biggestHighDensitySection.Line + 1 - biggestHighDensitySection.DensityRunLength - (extraLength + 1) / 2;
				int highDens1 = biggestHighDensitySection.Line + 1 + extraLength / 2;

				int xHeight = highDens1 - highDens0;


				while (y0 > 0 && xProjection[y0 - 1] < threshold * 0.9) y0--; //expand row to cover fairly empty places.
				while (y1 < xProjection.Length - 1 && xProjection[y1 + 1] < threshold * 0.9) y1++;//expand row to cover fairly empty places.
				y0 = Math.Max(y0, highDens0 - 2 * xHeight); //no more than 2 xHeights above body;
				y1 = Math.Min(y1, highDens1 + 2 * xHeight); //no more than 2 xHeights below body;

				double highDensMean = (cum[highDens1] - cum[highDens0]) / (highDens1 - highDens0);
				double emptyThreshold = 0.04 * highDensMean;

				for (int y = highDens0 - xHeight * 3 / 2; y > y0; y--)
				{
					if (xProjection[y] <= emptyThreshold)
					{
						y0 = y;
						break;
					}
				}
				for (int y = highDens1 + xHeight * 3 / 2; y < y1; y++)
				{
					if (xProjection[y] <= emptyThreshold)
					{
						y1 = y;
						break;
					}
				}

				//now, we may need to move words and thus their x-coordinates as well.
				double yShift = y0 - textLine.top; //shift line from textLine.top to y0 - usually negative, not always.
				double xShift = textLine.XOffsetForYOffset(yShift);
				foreach (var word in textLine.words)
				{
					word.top = y0;
					word.left += xShift;
					word.right += xShift;
					word.bottom = y1;
				}

				if (textLine.bodyTop != 0)
				{
					textLine.bodyBot += -(int)(yShift + 0.5);//we shouldn't shift the body;
					textLine.bodyTop += -(int)(yShift + 0.5);//we shouldn't shift the body;
				}
				if (textLine.bodyTopAlt != 0)
				{
					textLine.bodyBotAlt += -(int)(yShift + 0.5);//we shouldn't shift the body;
					textLine.bodyTopAlt += -(int)(yShift + 0.5);//we shouldn't shift the body;
				}

				textLine.top = y0;
				textLine.bodyTop = highDens0 - y0;
				textLine.bodyBot = highDens1 - y0;
				textLine.left += xShift;
				textLine.right += xShift;
				textLine.bottom = y1;
				Console.Write("{0}, ", highDens1 - highDens0);

				int bodyY0 = (y0 + y1) / 2;
				int bodyY1 = (y0 + y1) / 2 + 1;
				double mean = (cum[y1] - cum[y0]) / (y1 - y0);

				Func<int, double> bodyRating0 =
				(by0) => ((cum[bodyY1] - cum[by0]) - threshold * (bodyY1 - by0)) - ((cum[by0] - cum[y0]) - threshold * (by0 - y0));

				Func<int, double> bodyRating1 =
				(by1) => ((cum[by1] - cum[bodyY0]) - threshold * (by1 - bodyY0)) - ((cum[y1] - cum[by1]) - threshold * (y1 - by1));

				bool improvement = true;

				while (improvement)
				{
					improvement = false;
					double bestRating = bodyRating0(bodyY0);

					for (int newBY0 = y0 + 1; newBY0 < bodyY1; newBY0++)
					{
						if (bodyRating0(newBY0) > bestRating)
						{
							bodyY0 = newBY0;
							bestRating = bodyRating0(newBY0);
							improvement = true;
						}
					}

					bestRating = bodyRating1(bodyY1);

					for (int newBY1 = bodyY0 + 1; newBY1 < y1; newBY1++)
					{
						if (bodyRating1(newBY1) > bestRating)
						{
							bodyY1 = newBY1;
							bestRating = bodyRating1(newBY1);
							improvement = true;
						}
					}
				}
				textLine.bodyTopAlt = bodyY0 - y0;
				textLine.bodyBotAlt = bodyY1 - y0;
			}
		}

		static Regex fractionRegex = new Regex(@"\d/\d", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		public void ImproveGuess(HwrPageImage image, WordsImage betterGuessWords, Action<TextLine> lineProcessed)
		{
			bgWorker.ImproveGuess(image, betterGuessWords, line =>
			{
				Console.Write("[p{0};l{1}=={2}], ", betterGuessWords.pageNum, line.no, line.ComputedLikelihood);
				lineProcessed(line);
			});

			nativeOptimizer.SaveToManaged(); //TODO: syncup!
			symbolClasses.Save(HwrResources.SymbolDir);
		}

		public void ComputeFeatures(HwrPageImage image, TextLine line, out BitmapSource featureImage, out Point offset)
		{
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
							select edgeTrans)
			{
				for (int y = 0; y < featImgRGB.Height; y++)
				{
					var pix = featImgRGB[wordBoundary, y];
					pix.R = 255;
					pix.B = 255;
					featImgRGB[wordBoundary, y] = pix;
				}
			}

			for (int x = 0; x < featImgRGB.Width; x++)
			{ //only useful for scaled version, not for features!
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
	}
}
