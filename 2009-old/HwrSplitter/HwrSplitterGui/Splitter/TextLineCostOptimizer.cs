using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;
using DataIO;
using System.Threading;
using EmnExtensions.DebugTools;
using HwrLibCliWrapper;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows;

namespace DataIO
{

	public class TextLineCostOptimizer
	{
		const int charPhases = 1;
		HwrOptimizer nativeOptimizer;
		readonly SymbolWidth[] availableChars;

		public TextLineCostOptimizer( SymbolWidth[] availableChars) {
			this.availableChars = availableChars;
			nativeOptimizer = new HwrOptimizer(availableChars.Length * charPhases);
		}

		public void ImproveGuess(HwrPageImage image, WordsImage betterGuessWords, Action<TextLine> lineProcessed)
		{
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
					ImproveLineGuessNew(image,textLine);
					
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

		private void ImproveLineGuessNew(HwrPageImage image, TextLine lineGuess)
		{
			NiceTimer timer = new NiceTimer();
			timer.TimeMark("preparing lineguess");
			int topXoffset;

			int x0Est = Math.Max(0, (int)(lineGuess.left + lineGuess.BottomXOffset - 10 + 0.5));
			int x1Est = Math.Min(image.Width, (int)(lineGuess.right - lineGuess.BottomXOffset + 0.5));
			int y0 = (int)(lineGuess.top + 0.5);
			int y1 = (int)(lineGuess.bottom + 0.5);

			Func<char, bool> charKnown = c => availableChars.Where(sym => sym.c == c).Any();

			var basicLine = from word in lineGuess.words
							from letter in word.text.AsEnumerable().Concat(' ')
							select charKnown(letter) ? letter : (char)1;

			basicLine = basicLine.Prepend(' ');//first word should start with space too.

			basicLine = basicLine.Prepend((char)0).Concat((char)10);//overall line starts with 0 and ends with 10.

			var phaseCodeSeq = (
				from letter in basicLine
				let code = availableChars.Single(sym => sym.c == letter).code
				from phaseCode in Enumerable.Range((int)code * charPhases, charPhases)
				select (uint)phaseCode
				).ToArray();

			var croppedLine = image.Image.CropTo(x0Est, y0, x1Est, y1);

			timer.TimeMark(null);

			int[] charEndPos = nativeOptimizer.SplitWords(
									croppedLine,
									phaseCodeSeq,
									out topXoffset,
									(float)lineGuess.shear);
			int x0 = x0Est + topXoffset;

			charEndPos = charEndPos.Where((pos, i) => i % charPhases == charPhases - 1).ToArray();
			int currWord = -1;


			char[] charValue = basicLine.ToArray();

			for (int i = 0; i < charValue.Length; i++) {
				if (charValue[i] == ' ') { //found word boundary
					if (currWord >= 0) //then the previous char was the rightmost character of the current word.
					{
						lineGuess.words[currWord].right = x0 + charEndPos[i - 1];
						lineGuess.words[currWord].rightStat = TrackStatus.Calculated;
					}
					currWord++;//space means new word
					if (currWord < lineGuess.words.Length) //then the endpos of the space must be the beginning pos of the current word.
					{
						lineGuess.words[currWord].left = x0 + charEndPos[i];
						lineGuess.words[currWord].leftStat = TrackStatus.Calculated;
					}
				}
			}
			Debug.Assert(currWord == lineGuess.words.Length);
		}

		public void ComputeFeatures(HwrPageImage image, TextLine line, out BitmapSource featureImage, out Point offset)
		{
			int topXoffset;

			int x0Est = Math.Max(0, (int)(line.left + line.BottomXOffset - 500 + 0.5));
			int x1Est = Math.Min(image.Width, (int)(line.right + 500 + 0.5));
			int y0 = (int)(line.top + 0.5);
			int y1 = (int)(line.bottom + 0.5);

			ImageStruct<float> data = ImageProcessor.ExtractFeatures(image.Image.CropTo(x0Est, y0, x1Est, y1), out topXoffset);
			int featDataY = y0;
			int featDataX = (int)x0Est + topXoffset;
			var featImgRGB = data.MapTo(f => (byte)(255.9 * f)).MapTo(b => new PixelArgb32(255, b, b, b));
			foreach (Word w in line.words)
			{
				int l = (int)(w.left + 0.5) - featDataX;
				int r = (int)(w.right + 0.5) - featDataX;
				for (int y = 0; y < featImgRGB.Height; y++)
				{
					if (l >= 0 && l < featImgRGB.Width)
					{
						var pl = featImgRGB[l, y];
						pl.R = 255;
						featImgRGB[l, y] = pl;
					}
					if (r >= 0 && l < featImgRGB.Width)
					{
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


	}
}
