using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataIO;
using System.Threading;
using EmnExtensions.DebugTools;
using HwrLibCliWrapper;

namespace DataIO
{

	public class TextLineCostOptimizer
	{
		const double lenCostFactor = 3;
		const double posCostFactor = 1.0 / 1500;
		const int margin = 75;
		const int rShift = 15;
		const double InterWordCostFactor = 1.0 / 50;
		const double intermedBrightnessCostFactor = 5;
		const double endPointCostFactor = 2;
		const int MaxBodyLines = 65;
		const double MaxBodyBrightness = 0.63;
		const double SpaceDarknessPower = 3;
		const double WordLightnessPower = 3;
		const int charPhases = 1;

		HwrOptimizer nativeOptimizer;

		//        const double WordLightn
		double[] blurWindowXDir, blurWindowYDir;
		HwrPageImage image;
		SymbolWidth[] availableChars;

		public TextLineCostOptimizer(HwrPageImage image, SymbolWidth[] availableChars) {
			this.image = image;
			InitializeBlurWindows();
			this.availableChars = availableChars;
			nativeOptimizer = new HwrOptimizer(availableChars.Length * charPhases);
		}

		void InitializeBlurWindows() {
			blurWindowXDir = mkBlurWindow(7);
			blurWindowYDir = mkBlurWindow(21);
		}
		static double[] mkBlurWindow(int length) {
			if (length % 2 != 1) throw new ArgumentException("must make odd sized convolution window for centering reasons.");
			var blurWindow = Enumerable.Range(-length / 2, length).Select(v => Math.Exp(-10 * Math.Abs(v) / (double)length));
			var blurWindowSum = blurWindow.Sum();
			return blurWindow.Select(x => x / blurWindowSum).ToArray();
		}




		public void ImproveGuess(WordsImage betterGuessWords, Action<TextLine> lineProcessed) {
			object sync = new object();
			double totalTime = 0.0;
			while (true) {
				//var textLine = betterGuessWords.textlines[0];
				//ImproveLineGuessNew(textLine);
				//lineProcessed(textLine);
				//			Semaphore doneSem = new Semaphore(0, betterGuessWords.textlines.Length);
				for (int lineI = 0; lineI < betterGuessWords.textlines.Length; lineI++) {
					var textLine = betterGuessWords.textlines[lineI];
					//ThreadPool.QueueUserWorkItem((WaitCallback)((ignored) => {
					//	Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
					ImproveLineGuessNew(textLine);
					//using (var t = new DTimer((ts) => { lock (sync)	totalTime += ts.TotalSeconds; Console.WriteLine(ts);}))
					//    textLine.ComputeFeatures(image);
					lineProcessed(textLine);
					//				doneSem.Release();
					//}));
				}
			}
			//for (int lineI = 0; lineI < betterGuessWords.textlines.Length; lineI++)
			//    doneSem.WaitOne();
			Console.WriteLine("TotalFeatureExtractionTime == " + totalTime);
		}
		private void ImproveLineGuessNew(TextLine lineGuess) {
			lineGuess.DoHMMImprove(image, availableChars, charPhases, this.nativeOptimizer);
		}
		private void ImproveLineGuess(TextLine lineGuess) {
			int lineXBegin = (int)Math.Floor(lineGuess.left);
			int lineXEnd = (int)Math.Ceiling(lineGuess.right);
			int lineYBegin = (int)Math.Floor(lineGuess.top);
			int lineYEnd = (int)Math.Ceiling(lineGuess.bottom);

			int relativeBodyYEnd, relativeBodyYBegin;
			FindTextLineBody(lineXBegin, lineXEnd, lineYBegin, lineYEnd, out relativeBodyYBegin, out relativeBodyYEnd, lineGuess);

			//the shear-shift specifies how much the line-top will be right-shifted since it's the line body that's
			//centered in the line.
			int shearShift = (int)(relativeBodyYBegin * Math.Tan(2 * Math.PI * lineGuess.shear / 360.0) + 0.5)
				+ rShift;//since we see more stupid things at start than end.

			double[] processedShearedBodySum, processedShearedSum;
			FindContrastStretchedShearedSums(lineXBegin, lineXEnd, lineYBegin, lineYEnd, relativeBodyYBegin, relativeBodyYEnd, lineGuess.shear, out processedShearedSum, out processedShearedBodySum, lineGuess);


			Func<int, int, double> GetAverageBetween = CreateMemoizedAverager(processedShearedBodySum);



			int numWords = lineGuess.words.Length;
			int lineLength = lineXEnd - lineXBegin + 2 * margin;//TODO, this should probably be a little larger.
			double[,] endCosts = new double[numWords, lineLength];
			double[,] beginCosts = new double[numWords, lineLength];

			///The endcost of a word and a point is the cost of ending that word at that point,
			///including costs for all following words.
			///The startcost of a word and a point is the cost of starting that word at that point,
			///including costs for ending and all following words.
			///point 0 is at lineXBegin+shearShift - margin.


			//we initialize by doing the endpoint of the last word.
			int wordIndex = numWords - 1;
			for (int i = 0; i < lineLength; i++) {
				int imgPos = i + lineXBegin + shearShift - margin;
				//int posDiff = lineXEnd + shearShift - imgPos;
				endCosts[wordIndex, i] =
					// posDiff * (double)posDiff / posCostFactor/posCostFactor + //TODO, isn't really posCostFactor
					(1 - processedShearedSum[imgPos]) * endPointCostFactor;
			}

			for (int j = numWords - 1; j >= 0; j--) {
				//we'll treat j's start and j-1's end in this iteration. 0's _begin_ is a little special...

				//begin costs:
				wordIndex = j;
				Word word = lineGuess.words[wordIndex];
				double targetLength = word.symbolBasedLength.len;
				double target2Pos = word.left + word.right;
				for (int i = 0; i < lineLength; i++) {
					int imgPos = i + lineXBegin + shearShift - margin;
					double bestEndCosts = double.MaxValue;
					for (int k = i + 1; k < lineLength; k++) {
						int imgPosE = k + lineXBegin + shearShift - margin;
						int FewPercent = (imgPosE - imgPos) / 50;
						double wordLenScaled = (imgPosE - imgPos - targetLength) / (targetLength + 2) * lenCostFactor;
						double word2PosScaled = (imgPos + imgPosE - target2Pos) * posCostFactor;
						double wordLightness = GetAverageBetween(imgPos + FewPercent, imgPosE - FewPercent) * intermedBrightnessCostFactor; ;
						double endingHereCost =
							endCosts[wordIndex, k] +
							wordLenScaled * wordLenScaled +
							word2PosScaled * word2PosScaled +
							wordLightness;
						if (endingHereCost < bestEndCosts)
							bestEndCosts = endingHereCost;
					}

					beginCosts[wordIndex, i] =
						bestEndCosts +
						(1 - processedShearedSum[imgPos]) * endPointCostFactor;
				}

				if (wordIndex == 0) {
					//OK, we just calculated the begin costs of 0, but we should add costs for beginning far from the
					//line beginning...
					break;// there's no end of -1 here... ;-)
				}


				//end costs:
				wordIndex = j - 1;
				word = lineGuess.words[wordIndex];
				for (int i = 0; i < lineLength; i++) {
					int imgPos = i + lineXBegin + shearShift - margin;
					double bestBeginCosts = double.MaxValue;
					for (int k = i + 1; k < lineLength; k++) {
						int imgPosB = k + lineXBegin + shearShift - margin;
						double spaceLenScaled = (imgPosB - imgPos) * InterWordCostFactor;
						double beginningHereCost =
							beginCosts[wordIndex + 1, k] +
							(1 - GetAverageBetween(imgPos, imgPosB)) * intermedBrightnessCostFactor +
							spaceLenScaled * spaceLenScaled;
						if (beginningHereCost < bestBeginCosts)
							bestBeginCosts = beginningHereCost;
					}

					endCosts[wordIndex, i] =
						bestBeginCosts +
						(1 - processedShearedSum[imgPos]) * endPointCostFactor;
				}
			}

			//OK, we did precalculation! now we just follow the cheapest path!
			wordIndex = 0;
			double bestBeginCost = double.MaxValue;
			int bestBeginPos = -1;
			for (int i = 0; i < lineLength; i++) {
				if (beginCosts[wordIndex, i] < bestBeginCost) {
					bestBeginCost = beginCosts[wordIndex, i];
					bestBeginPos = i;
				}
			}

			lineGuess.cost = bestBeginCost;
			CostSummary summary = new CostSummary();
			CostSummary lastHit = new CostSummary();
			CostSummary lastSummary = new CostSummary();

			double bestEndCost = double.MaxValue;
			int bestEndPos = -1;

			for (int j = 0; j < numWords; j++) {


				wordIndex = j;
				//we have a beginpos, now find the matching end.
				//that's not the cheapest end, since that not might be reachable!
				//that's the end whose endcost+transition costs are the lowest.

				bestEndCost = double.MaxValue;
				bestEndPos = -1;
				Word word = lineGuess.words[wordIndex];
				double targetLength = word.symbolBasedLength.len;
				double target2Pos = word.left + word.right;
				int imgPos = bestBeginPos + lineXBegin + shearShift - margin;

				for (int k = 0; k < lineLength; k++) {
					int imgPosE = k + lineXBegin + shearShift - margin;
					int FewPercent = (imgPosE - imgPos) / 50;

					double wordLenScaled = (imgPosE - imgPos - targetLength) / (targetLength + 2) * lenCostFactor;
					double word2PosScaled = (imgPos + imgPosE - target2Pos) * posCostFactor;
					double wordLightness = GetAverageBetween(imgPos + FewPercent, imgPosE - FewPercent) * intermedBrightnessCostFactor;
					double endingHereCost =
						endCosts[wordIndex, k] +
						wordLenScaled * wordLenScaled +
						word2PosScaled * word2PosScaled +
						 wordLightness;
					if (endingHereCost < bestEndCost) {
						bestEndCost = endingHereCost;
						bestEndPos = k;

						lastHit.lengthErr = wordLenScaled * wordLenScaled;
						lastHit.posErr = word2PosScaled * word2PosScaled;
						lastHit.wordLightness = wordLightness;
					}
				}


				summary.spaceDarkness += (1 - processedShearedSum[bestBeginPos + lineXBegin + shearShift - margin]) * endPointCostFactor;
				summary.spaceDarkness += (1 - processedShearedSum[bestEndPos + lineXBegin + shearShift - margin]) * endPointCostFactor;
				// summary.spaceDarkness += lastHit.spaceDarkness;
				summary.lengthErr += lastHit.lengthErr;
				summary.posErr += lastHit.posErr;
				summary.spaceErr += lastHit.spaceErr;
				summary.wordLightness += lastHit.wordLightness;

				//beginCosts[wordIndex, bestBeginPos] == (1 - processedShearedSum[imgPos]) * endPointCostFactor - bestEndCost

				//we have an optimal find!
				word.costSummary = summary - lastSummary;
				word.left = bestBeginPos + lineXBegin + shearShift - margin;
				word.right = bestEndPos + lineXBegin + shearShift - margin;
				word.leftStat = TrackStatus.Calculated;
				word.rightStat = TrackStatus.Calculated;

				lastSummary = summary;
				//now find next beginning

				wordIndex = j + 1;
				if (wordIndex == numWords)
					break;//found em all.
				bestBeginCost = double.MaxValue;
				bestBeginPos = -1;
				word = lineGuess.words[wordIndex];
				imgPos = bestEndPos + lineXBegin + shearShift;

				for (int k = bestEndPos + 1; k < lineLength; k++) {
					int imgPosB = k + lineXBegin + shearShift;
					double spaceLenScaled = (imgPosB - imgPos) * InterWordCostFactor;
					double beginningHereCost =
						beginCosts[wordIndex, k] +
						(1 - GetAverageBetween(imgPos, imgPosB)) * intermedBrightnessCostFactor +
						spaceLenScaled * spaceLenScaled;
					if (beginningHereCost < bestBeginCost) {
						bestBeginCost = beginningHereCost;
						bestBeginPos = k;
						lastHit.spaceDarkness = (1 - GetAverageBetween(imgPos, imgPosB)) * intermedBrightnessCostFactor;
						lastHit.spaceErr = spaceLenScaled * spaceLenScaled;
					}
				}
				//endCosts[wordIndex-1, bestEndPos] == (1 - processedShearedSum[imgPos]) * endPointCostFactor - bestBeginCost
			}

			/*
				lastEnd = bestGuessEnd;
				lastEndWeight = 1.0 / InterWordCostFactor;
			*/

			lineGuess.costSummary = summary;
		}

		private static Func<int, int, double> CreateMemoizedAverager(double[] data) {

			List<double[]> sumByLength = new List<double[]> { data };
			//sumByLength[offset][start]  contains the sum of  'offset;+1 elements starting at index 'start'


			return (start, end) => {
				int length = end - start;
				if (length < 1) return 0;
				while (sumByLength.Count < length) {
					int nextSumLength = sumByLength.Count;
					double[] currentSumRow = sumByLength[nextSumLength - 1];
					double[] nextSumRow = new double[data.Length];

					for (int i = 0; i < currentSumRow.Length; i++)
						nextSumRow[i] = currentSumRow[i] + Math.Pow(data[(i + nextSumLength) % data.Length], WordLightnessPower);

					sumByLength.Add(nextSumRow);
				}
				return sumByLength[length - 1][start] / length; //calc average too!
			};

		}

		void FindTextLineBody(int lineXBegin, int lineXEnd, int lineYBegin, int lineYEnd, out int relativeBodyYBegin, out int relativeBodyYEnd, TextLine improvedGuess) {
			double[] rowAvg = new double[lineYEnd - lineYBegin];
			for (int y = lineYBegin; y < lineYEnd; y++) {
				for (int x = lineXBegin; x < lineXEnd; x++) { // a little tight around line ends....
					rowAvg[y - lineYBegin] += image[y, x];
				}
				rowAvg[y - lineYBegin] /= lineXEnd - lineXBegin;
			}
			rowAvg = BlurLine(rowAvg, blurWindowYDir);

			double rowAvgMin = double.MaxValue, rowAvgMax = double.MinValue;
			int rowAvgMinIndex = -1;
			foreach (int i in Enumerable.Range(0, rowAvg.Length)) {
				if (rowAvg[i] < rowAvgMin) {
					rowAvgMin = rowAvg[i];
					rowAvgMinIndex = i;
				}
				if (rowAvg[i] > rowAvgMax) rowAvgMax = rowAvg[i];
			}
			var rowAvgRange = rowAvgMax - rowAvgMin;
			var rowAvgContrastStretched = rowAvg.Select(x => (x - rowAvgMin) / rowAvgRange).ToArray();
			//ideally now, somewhere near the baseline the color will be darkest and that's where we'll look most.

			relativeBodyYBegin = rowAvgMinIndex;
			relativeBodyYEnd = rowAvgMinIndex;
			while (relativeBodyYEnd - relativeBodyYBegin < MaxBodyLines - 1 && relativeBodyYEnd < lineYEnd - lineYBegin - 1 && relativeBodyYBegin > 0) {
				if (rowAvgContrastStretched[relativeBodyYEnd + 1] < rowAvgContrastStretched[relativeBodyYBegin - 1]) {
					if (rowAvgContrastStretched[relativeBodyYEnd + 1] > MaxBodyBrightness)
						break;
					else
						relativeBodyYEnd++;
				} else {
					if (rowAvgContrastStretched[relativeBodyYBegin - 1] > MaxBodyBrightness)
						break;
					else
						relativeBodyYBegin--;
				}
			}

			improvedGuess.rowsum = rowAvgContrastStretched.Select(d => (float)d).ToArray();
			improvedGuess.bodyTop = relativeBodyYBegin;
			improvedGuess.bodyBot = relativeBodyYEnd;
		}

		void FindContrastStretchedShearedSums(int lineXBegin, int lineXEnd, int lineYBegin, int lineYEnd, int relativeBodyYBegin, int relativeBodyYEnd, double shear, out double[] processedShearedSum, out double[] processedShearedBodySum, TextLine improvedGuess) {
			int relevantXBegin = lineXBegin - margin,
				relevantXEnd = lineXEnd + margin;

			//calculate the average intensity of each (sheared) column inside the entire line body:
			double[] shearedSum = ShearedSum(lineYBegin, lineYBegin, lineYEnd, shear);

			//calculate the average intensity of each (sheared) column inside the main section of the line body:
			double[] shearedBodySum = ShearedSum(lineYBegin, lineYBegin + relativeBodyYBegin, lineYBegin + relativeBodyYEnd, shear);

			shearedSum = shearedSum.Select((x, i) => x + 2 * shearedBodySum[i]).Select(x => Math.Pow(x, SpaceDarknessPower)).ToArray();//do weight center a little more.
			processedShearedSum = ContrastStretchAndBlur(shearedSum, blurWindowXDir, relevantXBegin, relevantXEnd);
			processedShearedBodySum = ContrastStretchAndBlur(shearedBodySum, blurWindowXDir, relevantXBegin, relevantXEnd);

			improvedGuess.shearedbodysum = processedShearedBodySum.Select(d => (float)d).ToArray();
			improvedGuess.shearedsum = processedShearedSum.Select(d => (float)d).ToArray();
		}

		static double[] ContrastStretchAndBlur(double[] line, double[] blurWindow, int relevantXBegin, int relevantXEnd) {
			double average = line.Skip(relevantXBegin).Take(relevantXEnd - relevantXBegin).Average();
			for (int i = 0; i < relevantXBegin; i++)
				line[i] = average;
			for (int i = relevantXEnd; i < line.Length; i++)
				line[i] = average;
			double[] blurredLine = BlurLine(line, blurWindow);
			double min = blurredLine.Min(), max = blurredLine.Max();

			return blurredLine.Select(x => (x - min) / (max - min)).ToArray();
		}

		static double[] BlurLine(double[] data, double[] window) {
			double[] retval = new double[data.Length];
			for (int di = 0; di < data.Length; di++)
				for (int wi = 0; wi < window.Length; wi++) {
					int pos = di + wi - window.Length / 2;
					if (pos < 0)
						pos = 0;
					else if (pos >= data.Length)
						pos = data.Length - 1;
					retval[di] += data[pos] * window[wi];
				}
			return retval;
		}

		double[] ShearedSum(int shearTop, int top, int bottom, double shear) {
			var xOffsetLookup = Enumerable.Range(top - shearTop, bottom - top).Select(
				yOffset => -yOffset * Math.Tan(2 * Math.PI * shear / 360.0)
					).ToArray();
			var height = bottom - top;
			var midpoint = height / 2.0;
			var divFactor =
				Enumerable.Range(0, height)
				.Select(y => (y - midpoint) / midpoint)
				.Select(x => 1 - x * x)
				.Sum();
			divFactor = height;
			return (
				from x in Enumerable.Range(0, image.Width)
				select (
						from yOffset in Enumerable.Range(0, height)
						let xOffset = xOffsetLookup[yOffset]
						let xNet = Math.Min(Math.Max(0, x + xOffset), image.Width - 1)
						let yRelFromMid = (yOffset - midpoint) / midpoint
						let pixelVal = image.Interpolate(top + yOffset, xNet)
						let avgVal = pixelVal
						select avgVal //*(1-yRelFromMid*yRelFromMid)
					).Sum() / divFactor
				).ToArray();
		}
	}
}
