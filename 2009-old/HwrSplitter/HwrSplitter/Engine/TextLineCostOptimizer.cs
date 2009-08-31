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

			if (HwrResources.SymbolsGZ.Exists) {
				using (var fileStream = HwrResources.SymbolsGZ.OpenRead())
				using (var zipStream = new GZipStream(fileStream, CompressionMode.Decompress))
				using (var xmlreader = XmlReader.Create(zipStream)) {
					SymbolClasses fromDisk = SymbolClasses.Deserialize(xmlreader);
					this.iteration = fromDisk.Iteration == -1 ? 0 : fromDisk.Iteration;
					this.StartPastPage = fromDisk.LastPage;
					this.symbolClasses = fromDisk.Symbol;
				}
			} else if (HwrResources.Symbols.Exists) {
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
		static Regex fractionRegex = new Regex(@"\d/\d", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		public void ImproveGuess(HwrPageImage image, WordsImage betterGuessWords, Action<TextLine> lineProcessed) {
			//object sync = new object();
			//double totalTime = 0.0;
			//while (true) {
			//var textLine = betterGuessWords.textlines[0];
			//ImproveLineGuessNew(textLine);
			//lineProcessed(textLine);
			//			Semaphore doneSem = new Semaphore(0, betterGuessWords.textlines.Length);
			for (int lineI = 0; lineI < betterGuessWords.textlines.Length; lineI++) {
				var textLine = betterGuessWords.textlines[lineI];
				StringBuilder linetextB = new StringBuilder();
				textLine.words.ForEach(w => { linetextB.Append(w.text); linetextB.Append(' '); });
				string linetext = linetextB.ToString();
				if (linetext.Length < 30 ) {
					Console.Write("skipped:len=={0}, ", linetext.Length);
					continue;
				}
				if (fractionRegex.IsMatch(linetext)) {
					Console.Write("skipped:fraction, ", linetext.Length);
					continue;
				}
				//ThreadPool.QueueUserWorkItem((WaitCallback)((ignored) => {
				//	Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

#if DEBUG
				if (iteration % 100 == 0) {
					nativeOptimizer.SaveToManaged(symbolClasses);
					using (var stream = HwrResources.SymbolDir.GetRelativeFile("symbolsD-" + DateTime.Now.ToString("u", CultureInfo.InvariantCulture).Replace(' ', '_').Replace(':', '.') + ".xml").Open(FileMode.Create))
						new SymbolClasses { Symbol = symbolClasses, Iteration = iteration, LastPage = betterGuessWords.pageNum }.SerializeTo(stream);
				}
#endif

				ImproveLineGuessNew(image, textLine);

				//using (var t = new DTimer((ts) => { lock (sync)	totalTime += ts.TotalSeconds; Console.WriteLine(ts);}))
				//    textLine.ComputeFeatures(image);
				lineProcessed(textLine);
				//Console.Write("{0}[{1}], ", iteration,textLine.ComputedLikelihood);

				if (iteration % 100 == 0) {
					nativeOptimizer.SaveToManaged(symbolClasses);
					using (var stream = HwrResources.SymbolDir.GetRelativeFile("symbols-" + DateTime.Now.ToString("u", CultureInfo.InvariantCulture).Replace(' ', '_').Replace(':', '.') + ".xml.gz").Open(FileMode.Create))
					using (var zipStream = new GZipStream(stream,CompressionMode.Compress))
						new SymbolClasses { Symbol = symbolClasses, Iteration = iteration, LastPage = betterGuessWords.pageNum }.SerializeTo(zipStream);
					Console.WriteLine();
					nativeOptimizer.GetFeatureWeights().Zip(FeatureDistributionEstimate.FeatureNames, (weight, name) => name + ": " + weight).ForEach(Console.WriteLine);

				}


				//				doneSem.Release();
				//}));
			}
			//}
			//for (int lineI = 0; lineI < betterGuessWords.textlines.Length; lineI++)
			//    doneSem.WaitOne();
			//Console.WriteLine("TotalFeatureExtractionTime == " + totalTime);
		}

		private void ImproveLineGuessNew(HwrPageImage image, TextLine lineGuess) {
#if LOGLINESPEED
			NiceTimer timer = new NiceTimer();
			timer.TimeMark("preparing lineguess");
#endif
			int topXoffset;

			int x0Est = Math.Max(0, (int)(lineGuess.left + lineGuess.BottomXOffset - 10 + 0.5));
			int x1Est = Math.Min(image.Width, (int)(lineGuess.right - lineGuess.BottomXOffset + 0.5));
			int y0 = (int)(lineGuess.top + 0.5);
			int y1 = (int)(lineGuess.bottom + 0.5);

			Func<char, bool> charKnown = c => symbolClasses.Where(sym => sym.Letter == c).Any();

			var basicLine = from word in lineGuess.words
							from letter in word.text.AsEnumerable().Concat(' ')
							select charKnown(letter) ? letter : (char)1;

			basicLine = basicLine.Prepend(' ');//first word should start with space too.

			basicLine = basicLine.Prepend((char)0).Concat((char)10);//overall line starts with 0 and ends with 10.

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
									(float)lineGuess.shear, iteration++, out topXoffset, out likelihood);
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
