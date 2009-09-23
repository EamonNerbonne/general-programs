using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HwrDataModel;
using MoreLinq;

namespace HwrSplitter.Engine
{
	public static class TextLineYPosition
	{
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

		public static void LocateLineBodies(HwrPageImage image, WordsImage betterGuessWords)
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

				int extraLength = Math.Max(30 - biggestHighDensitySection.DensityRunLength, 0);

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
	}
}
