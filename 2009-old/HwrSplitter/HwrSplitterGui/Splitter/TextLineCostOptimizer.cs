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

namespace DataIO
{

	public class TextLineCostOptimizer
	{
		const int charPhases = 1;
		HwrOptimizer nativeOptimizer;
		readonly HwrPageImage image;
		readonly SymbolWidth[] availableChars;

		public TextLineCostOptimizer(HwrPageImage image, SymbolWidth[] availableChars) {
			this.image = image;
			this.availableChars = availableChars;
			nativeOptimizer = new HwrOptimizer(availableChars.Length * charPhases);
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

			var phaseCodeSeq =
				from letter in basicLine
				let code = availableChars.Single(sym => sym.c == letter).code
				from phaseCode in Enumerable.Range((int)code * charPhases, charPhases)
				select (uint)phaseCode;


			int[] charEndPos = nativeOptimizer.SplitWords(
									image.Image.CropTo(x0Est, y0, x1Est, y1),
									phaseCodeSeq.ToArray(),
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

	}
}
