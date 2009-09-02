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
		public const int CharPhases = 1;
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
			BoxBlur(image.XProjectionSmart, 6);
			BoxBlur(image.XProjectionSmart, 6);
			BoxBlur(image.XProjectionSmart, 6);//sideeffect!
			BoxBlur(image.XProjectionRaw, 4);
			BoxBlur(image.XProjectionRaw, 4);//sideeffect!
			LocateLineBodiesImpl(image.XProjectionRaw, betterGuessWords, (tl, range) => {
				tl.bodyTopAlt = range.start;
				tl.bodyBotAlt = range.end;
			});
			LocateLineBodiesImpl(image.XProjectionSmart, betterGuessWords, (tl, range) => {
				tl.bodyTop = range.start;
				tl.bodyBot = range.end;
			});
		}

		public struct Range {public  int start, end;}
		public void LocateLineBodiesImpl(double[] xProjection, WordsImage betterGuessWords, Action<TextLine, Range> setBody) {
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

				double mean = (cum[y1] - cum[y0]) / (y1 - y0);

				int bodyY0 = (y0 + y1) / 2;
				int bodyY1 = (y0 + y1) / 2 + 1;

				Func<int, double> bodyRating0 =
				(by0) => ((cum[bodyY1] - cum[by0]) - mean * (bodyY1 - by0)) - ((cum[by0] - cum[y0]) - mean * (by0 - y0));

				Func<int, double> bodyRating1 =
				(by1) => ((cum[by1] - cum[bodyY0]) - mean * (by1 - bodyY0)) - ((cum[y1] - cum[by1]) - mean * (y1 - by1));

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
				setBody(textLine, new Range { start = bodyY0 - y0, end = bodyY1 - y0 });
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
				if (fractionRegex.IsMatch(linetext)) {
					Console.Write("skipped:fraction, ", linetext.Length);
					continue;
				}

				//if (iteration % 100 == 0) {
				//    nativeOptimizer.SaveToManaged(symbolClasses);
				//    using (var stream = HwrResources.SymbolDir.GetRelativeFile("symbolsDebug-" + DateTime.Now.ToString("u", CultureInfo.InvariantCulture).Replace(' ', '_').Replace(':', '.') + ".xml").Open(FileMode.Create))
				//        new SymbolClasses { Symbol = symbolClasses, Iteration = iteration, LastPage = betterGuessWords.pageNum }.SerializeTo(stream);
				//}

				ImproveLineGuessNew(image, textLine);

				lineProcessed(textLine);
				//Console.Write("{0}[{1}], ", iteration,textLine.ComputedLikelihood);

				if (iteration % 100 == 0) {
					nativeOptimizer.SaveToManaged(symbolClasses);
					using (var stream = HwrResources.SymbolDir.GetRelativeFile("symbols-" + DateTime.Now.ToString("u", CultureInfo.InvariantCulture).Replace(' ', '_').Replace(':', '.') + ".xml.gz").Open(FileMode.Create))
					using (var zipStream = new GZipStream(stream, CompressionMode.Compress))
						new SymbolClasses { Symbol = symbolClasses, Iteration = iteration, LastPage = betterGuessWords.pageNum }.SerializeTo(zipStream);
					Console.WriteLine();
					nativeOptimizer.GetFeatureWeights()
						.Zip(FeatureDistributionEstimate.FeatureNames, (weight, name) => name + ": " + weight)
						.Zip(nativeOptimizer.GetFeatureVariances(), (str, variance) => str + " (" + variance + ")")
						.ForEach(Console.WriteLine);
				}
			}
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

			var phaseCodeSeq = (
				from letter in basicLine
				let code = symbolClasses.Single(sym => sym.Letter == letter).Code
				from phaseCode in Enumerable.Range((int)code * CharPhases, CharPhases)
				select (uint)phaseCode
				).ToArray();

			var croppedLine = image.Image.CropTo(x0Est, y0, x1Est, y1);
#if LOGLINESPEED
			timer.TimeMark(null);
#endif

			double likelihood;
			int[] charEndPos = nativeOptimizer.SplitWords(
									croppedLine,
									phaseCodeSeq,
									(float)lineGuess.shear, iteration++, lineGuess, out topXoffset, out likelihood);
			lineGuess.ComputedLikelihood = likelihood;
			int x0 = x0Est + topXoffset;

			charEndPos = charEndPos.Where((pos, i) => i % CharPhases == CharPhases - 1).ToArray();
			int currWord = -1;


			char[] charValue = basicLine.ToArray();

			for (int i = 0; i < charValue.Length; i++) {
				if (charValue[i] == ' ') { //found word boundary
					if (currWord >= 0) //then the previous char was the rightmost character of the current word.
					{
						lineGuess.words[currWord].right = x0 + charEndPos[i - 1];
						lineGuess.words[currWord].rightStat = Word.TrackStatus.Calculated;
					}
					currWord++;//space means new word
					if (currWord < lineGuess.words.Length) //then the endpos of the space must be the beginning pos of the current word.
					{
						lineGuess.words[currWord].left = x0 + charEndPos[i];
						lineGuess.words[currWord].leftStat = Word.TrackStatus.Calculated;
					}
				}
			}
			Debug.Assert(currWord == lineGuess.words.Length);

		}

		public void ComputeFeatures(HwrPageImage image, TextLine line, out BitmapSource featureImage, out Point offset) {
			int topXoffset;

			int x0Est = Math.Max(0, (int)(line.left + line.BottomXOffset - 500 + 0.5));
			int x1Est = Math.Min(image.Width, (int)(line.right + 500 + 0.5));
			int y0 = (int)(line.top + 0.5);
			int y1 = (int)(line.bottom + 0.5);

			ImageStruct<float> data = ImageProcessor.ExtractFeatures(image.Image.CropTo(x0Est, y0, x1Est, y1), out topXoffset);
			int featDataY = y0;
			int featDataX = (int)x0Est + topXoffset;
			var featImgRGB = data.MapTo(f => (byte)(255.9 * f)).MapTo(b => new PixelArgb32(255, b, b, b));
			foreach (Word w in line.words) {
				int l = (int)(w.left + 0.5) - featDataX;
				int r = (int)(w.right + 0.5) - featDataX;
				for (int y = 0; y < featImgRGB.Height; y++) {
					if (l >= 0 && l < featImgRGB.Width) {
						var pl = featImgRGB[l, y];
						pl.R = 255;
						featImgRGB[l, y] = pl;
					}
					if (r >= 0 && l < featImgRGB.Width) {
						var pr = featImgRGB[r, y];
						pr.G = 255;
						featImgRGB[r, y] = pr;
					}
				}
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
