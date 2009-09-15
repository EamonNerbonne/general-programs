using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using EmnExtensions;
using EmnExtensions.DebugTools;
using EmnExtensions.Filesystem;
using HwrDataModel;
using HwrLibCliWrapper;
using MoreLinq;
using System.Xml;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace HwrSplitter.Engine
{
	public class SymbolClasses : XmlSerializableBase<SymbolClasses>
	{
		public int Iteration = -1;
		public int LastPage = -1;
		public SymbolClass[] Symbol;
	}

	public class TextLineCostOptimizer
	{
		static TextLineCostOptimizer() {
			FeatureDistributionEstimate.FeatureNames = FeatureToString.FeatureNames();
		}
		HwrOptimizer nativeOptimizer;
		SymbolClass[] symbolClasses;
		int iteration = 0;
		public int StartPastPage;

		public TextLineCostOptimizer() {

			if (HwrResources.SymbolsGZ != null) {
				Console.WriteLine("Loading: {0}", HwrResources.SymbolsGZ.FullName);
				using (var fileStream = HwrResources.SymbolsGZ.OpenRead())
				using (var zipStream = new GZipStream(fileStream, CompressionMode.Decompress))
				using (var xmlreader = XmlReader.Create(zipStream)) {
					SymbolClasses fromDisk = SymbolClasses.Deserialize(xmlreader);
					this.iteration = fromDisk.Iteration == -1 ? 0 : fromDisk.Iteration;
					this.StartPastPage = fromDisk.LastPage;
					this.symbolClasses = fromDisk.Symbol;
				}
			} else if (HwrResources.Symbols != null) {
				Console.WriteLine("Loading: {0}", HwrResources.Symbols.FullName);
				using (var fileStream = HwrResources.Symbols.OpenRead())
				using (var xmlreader = XmlReader.Create(fileStream)) {
					SymbolClasses fromDisk = SymbolClasses.Deserialize(xmlreader);
					this.iteration = fromDisk.Iteration == -1 ? 0 : fromDisk.Iteration;
					this.StartPastPage = fromDisk.LastPage;
					this.symbolClasses = fromDisk.Symbol;
				}
			} else
				this.symbolClasses = SymbolClassParser.Parse(HwrResources.CharWidthFile, TextLineCostOptimizer.CharPhases);

			nativeOptimizer = new HwrOptimizer(symbolClasses);
		}

		static void BoxBlur(double[] arr, int window) {
			double[] cum = new double[arr.Length + 1];
			double sum = 0.0;
			for (int i = 0; true; i++) {
				cum[i] = sum;
				if (i == arr.Length) break;
				sum += arr[i];
			}

			int botWindow = window / 2, topWindow = (window + 1) / 2;
			for (int i = 0; i < arr.Length; i++) {
				int minI = Math.Max(0, i - botWindow), maxI = Math.Min(cum.Length - 1, i + topWindow);
				arr[i] = (cum[maxI] - cum[minI]) / (maxI - minI);
			}
		}

		public void LocateLineBodies(HwrPageImage image, WordsImage betterGuessWords) {
			image.ComputeXProjection((int)(betterGuessWords.textlines[0].left + 0.5), (int)(betterGuessWords.textlines[0].right + 0.5));
			BoxBlur(image.XProjectionSmart, 4);
			BoxBlur(image.XProjectionSmart, 4);
			BoxBlur(image.XProjectionSmart, 4);//sideeffect!
			LocateLineBodiesImpl(image.XProjectionSmart, betterGuessWords);

		}

		public struct Range { public int start, end;}
		public void LocateLineBodiesImpl(double[] xProjection, WordsImage betterGuessWords) {
			double[] cum = new double[xProjection.Length + 1];
			double sum = 0.0;
			for (int i = 0; true; i++) {
				cum[i] = sum;
				if (i == xProjection.Length) break;
				sum += xProjection[i];
			}

			for (int lineI = 0; lineI < betterGuessWords.textlines.Length; lineI++) {
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

				for (int y = highDens0 - xHeight * 3/2; y > y0; y--) {
					if (xProjection[y] <= emptyThreshold) {
						y0 = y;
						break;
					}
				}
				for (int y = highDens1 + xHeight * 3 / 2; y < y1; y++) {
					if (xProjection[y] <= emptyThreshold) {
						y1 = y;
						break;
					}
				}

				//now, we may need to move words and thus their x-coordinates as well.
				double yShift = y0 - textLine.top; //shift line from textLine.top to y0 - usually negative, not always.
				double xShift = textLine.XOffsetForYOffset(yShift);
				foreach (var word in textLine.words) {
					word.top = y0;
					word.left += xShift;
					word.right += xShift;
					word.bottom = y1;
				}

				if (textLine.bodyTop != 0) {
					textLine.bodyBot += -(int)(yShift + 0.5);//we shouldn't shift the body;
					textLine.bodyTop += -(int)(yShift + 0.5);//we shouldn't shift the body;
				}
				if (textLine.bodyTopAlt != 0) {
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

				while (improvement) {
					improvement = false;
					double bestRating = bodyRating0(bodyY0);

					for (int newBY0 = y0 + 1; newBY0 < bodyY1; newBY0++) {
						if (bodyRating0(newBY0) > bestRating) {
							bodyY0 = newBY0;
							bestRating = bodyRating0(newBY0);
							improvement = true;
						}
					}

					bestRating = bodyRating1(bodyY1);

					for (int newBY1 = bodyY0 + 1; newBY1 < y1; newBY1++) {
						if (bodyRating1(newBY1) > bestRating) {
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
		public void ImproveGuess(HwrPageImage image, WordsImage betterGuessWords, Action<TextLine> lineProcessed) {
			for (int lineI = 0; lineI < betterGuessWords.textlines.Length; lineI++) {
				var textLine = betterGuessWords.textlines[lineI];
				StringBuilder linetextB = new StringBuilder();
				textLine.words.ForEach(w => { linetextB.Append(w.text); linetextB.Append(' '); });
				string linetext = linetextB.ToString();
				if (linetext.Length < 30) {
					Console.Write("skipped:len=={0}, ", linetext.Length);
					continue;
				}
				//if (fractionRegex.IsMatch(linetext)) {
				//    Console.Write("skipped:fraction, ", linetext.Length);
				//    continue;
				//}

				//if (iteration % 100 == 0) {
				//    nativeOptimizer.SaveToManaged(symbolClasses);
				//    using (var stream = HwrResources.SymbolDir.GetRelativeFile("symbolsDebug-" + DateTime.Now.ToString("u", CultureInfo.InvariantCulture).Replace(' ', '_').Replace(':', '.') + ".xml").Open(FileMode.Create))
				//        new SymbolClasses { Symbol = symbolClasses, Iteration = iteration, LastPage = betterGuessWords.pageNum }.SerializeTo(stream);
				//}

				ImproveLineGuessNew(image, textLine);

				lineProcessed(textLine);
				Console.Write("{0}[p{1};l{2}=={3}], ", iteration, betterGuessWords.pageNum, textLine.no, textLine.ComputedLikelihood);

			}
			nativeOptimizer.SaveToManaged(symbolClasses);
			using (var stream = HwrResources.SymbolDir.GetRelativeFile("symbols-" + DateTime.Now.ToString("u", CultureInfo.InvariantCulture).Replace(' ', '_').Replace(':', '.') + "-p" + betterGuessWords.pageNum + ".xml.gz").Open(FileMode.Create))
			using (var zipStream = new GZipStream(stream, CompressionMode.Compress))
				new SymbolClasses { Symbol = symbolClasses, Iteration = iteration, LastPage = betterGuessWords.pageNum }.SerializeTo(zipStream);
			//Console.WriteLine();
			//nativeOptimizer.GetFeatureWeights()
			//    .Zip(FeatureDistributionEstimate.FeatureNames, (weight, name) => name + ": " + weight)
			//    .Zip(nativeOptimizer.GetFeatureVariances(), (str, variance) => str + " (" + variance + ")")
			//    .ForEach(Console.WriteLine);
		}

		private void ImproveLineGuessNew(HwrPageImage image, TextLine lineGuess) {
#if LOGLINESPEED
			NiceTimer timer = new NiceTimer();
			timer.TimeMark("preparing lineguess");
#endif
			int topXoffset;

			int x0Est = Math.Max(0, (int)(lineGuess.OuterExtremeLeft + 0.5));
			int x1Est = Math.Min(image.Width, (int)(lineGuess.OuterExtremeRight + 0.5));
			int y0 = (int)(lineGuess.top + 0.5);
			int y1 = (int)(lineGuess.bottom + 0.5);

			Func<char, bool> charKnown = c => symbolClasses.Where(sym => sym.Letter == c).Any();

			var basicLine = lineGuess.TextWithTerminators.Select(letter => charKnown(letter) ? letter : (char)1).ToArray();

			var overrideEnds = from word in lineGuess.words
							   from oe in Enumerable.Repeat(-1, word.text.Length - 1)
											.Prepend(word.leftStat == Word.TrackStatus.Manual ? (int)(word.left - x0Est + 0.5) : -1)
											.Concat(word.rightStat == Word.TrackStatus.Manual ? (int)(word.right - x0Est + 0.5) : -1)
							   //ok, each word has accounted for its preceeding space and itself
							   select oe;
			//we're missing the end of the startSymbol, and the ends of the final space and end symbol.

			overrideEnds = (-1).Concat(overrideEnds).Concat(-1).Concat(-1).ToArray();

			var phaseCodeSeq = (
				from letter in basicLine
				from phaseCode in symbolClasses.Where(sym => sym.Letter == letter).Select(sym => sym.Code).OrderBy(code => code)
				select (uint)phaseCode
				).ToArray();

			var overrideEndsArray = (from end in overrideEnds
									 from phase in Enumerable.Range(0, CharPhases)
									 select phase == CharPhases - 1 ? end : -1).ToArray();

			var croppedLine = image.Image.CropTo(x0Est, y0, x1Est, y1);
#if LOGLINESPEED
			timer.TimeMark(null);
#endif

			double likelihood;
			int[] charEndPos = nativeOptimizer.SplitWords(
									croppedLine,
									phaseCodeSeq,
									overrideEndsArray,
									(float)lineGuess.shear, iteration++, lineGuess, out topXoffset, out likelihood);
			lineGuess.ComputedLikelihood = likelihood;
			int x0 = x0Est + topXoffset;

			charEndPos = charEndPos.Where((pos, i) => i % CharPhases == CharPhases - 1).Select(x => x + x0).ToArray(); //correct for extra char phases.
			int currWord = -1;

			lineGuess.computedCharEndpoints = charEndPos;

			char[] charValue = basicLine.ToArray();

			for (int i = 0; i < charValue.Length; i++) {
				if (charValue[i] == ' ') { //found word boundary
					if (currWord >= 0) { //then the previous char was the rightmost character of the current word.
						lineGuess.words[currWord].right = charEndPos[i - 1];
						lineGuess.words[currWord].rightStat = Word.TrackStatus.Calculated;
					}
					currWord++;//space means new word
					if (currWord < lineGuess.words.Length) //then the endpos of the space must be the beginning pos of the current word.
					{
						lineGuess.words[currWord].left = charEndPos[i];
						lineGuess.words[currWord].leftStat = Word.TrackStatus.Calculated;
					}
				}
			}
			Debug.Assert(currWord == lineGuess.words.Length);

		}

		public void ComputeFeatures(HwrPageImage image, TextLine line, out BitmapSource featureImage, out Point offset) {
			int topXoffset;

			int x0Est = Math.Max(0, (int)(line.OuterExtremeLeft + 0.5));
			int x1Est = Math.Min(image.Width, (int)(line.OuterExtremeRight + 0.5));
			int y0 = (int)(line.top + 0.5);
			int y1 = (int)(line.bottom + 0.5);

			ImageStruct<float> data = ImageProcessor.ExtractFeatures(image.Image.CropTo(x0Est, y0, x1Est, y1), line, out topXoffset);
			int featDataY = y0;
			int featDataX = (int)x0Est + topXoffset;
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

		public Dictionary<char, GaussianEstimate> MakeSymbolWidthEstimate() {
			return (
					from symbolClass in symbolClasses
					group symbolClass.Length by symbolClass.Letter into symbolsByLetter
					select symbolsByLetter
				).ToDictionary(
					symbolGroup => symbolGroup.Key,
					symbolGroup => symbolGroup.Aggregate((a, b) => a + b)
				);
		}
	}
}
