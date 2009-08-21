using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MoreLinq;
using System.Xml.Linq;
using HwrLibCliWrapper;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;

namespace DataIO
{
	public class TextLine : ShearedBox, IAsXml
	{
		public Word[] words;
		public int no;

		public float[] shearedsum;
		public float[] shearedbodysum;
		public float[] rowsum;//TODO better name
		public int bodyTop;
		public int bodyBot;
		public float[,] features;

		public double cost = double.NaN;
		public CostSummary? costSummary;

		public TextLine() { }
		public TextLine(string text, int no, double top, double bottom, double left, double right, double shear, Dictionary<char, SymbolWidth> symbolWidths)
			: base(top, bottom, left, right, shear)
		{
			this.no = no;
			this.words = GuessWordsInString(text, symbolWidths).ToArray();
		}
		public string costSummaryString() { return costSummary == null ? "" : costSummary.ToString(); }

		private LengthEstimate EstimateCharLength(char c, Dictionary<char, SymbolWidth> symbolWidths)
		{
			SymbolWidth sym;
			if (symbolWidths.TryGetValue(c, out sym))
				return sym.estimate;
			sym = symbolWidths[(char)1];
			return sym.estimate;
		}

		private LengthEstimate EstimateWordLength(string word, Dictionary<char, SymbolWidth> symbolWidths, bool isFirst, bool isLast)
		{
			LengthEstimate estimate = EstimateCharLength(' ', symbolWidths);
			if (isFirst) estimate += EstimateCharLength((char)0, symbolWidths);
			if (isLast) estimate += EstimateCharLength((char)10, symbolWidths);
			foreach (char c in word)
			{
				estimate += EstimateCharLength(c, symbolWidths);
			}
			return estimate;
		}
		private IEnumerable<Word> GuessWordsInString(string text, Dictionary<char, SymbolWidth> symbolWidths)
		{
			int no = 1;//"number" starts with 1!
			double width = right - left;
			var wordStrs = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			LengthEstimate[] lengthEstimates = wordStrs.Select((w, i) =>
				EstimateWordLength(w, symbolWidths, i == 0, i == wordStrs.Length - 1)).ToArray();
			var totalEstimate = lengthEstimates.Aggregate((a, b) => a + b);
			//ok, so we have a total line length and a per word estimate
			double estErr = totalEstimate.len - width;
			double correctionPerVar = -estErr / totalEstimate.var;
			double lengthToLeft = 0;
			for (int i = 0; i < wordStrs.Length; i++)
			{
				string wordStr = wordStrs[i];

				double lengthToRight = lengthToLeft + lengthEstimates[i].len + lengthEstimates[i].var * correctionPerVar;
				yield return new Word(wordStr, no, top, bottom,
					left + lengthToLeft,
					left + lengthToRight,
					shear)
					{
						symbolBasedLength = lengthEstimates[i]
					};
				no++;
				lengthToLeft = lengthToRight;
			}
			Debug.Assert(Math.Abs(lengthToLeft - width) < 1, "math error");
		}
		public TextLine(XElement fromXml)
			: base(fromXml)
		{
			no = (int)fromXml.Attribute("no");
			words = fromXml.Elements("Word").Select(xmlWord => new Word(xmlWord)).ToArray();
		}



		public XNode AsXml()
		{
			return new XElement("TextLine",
				new XAttribute("no", no),
				base.MakeXAttrs(),
				words.Select(word => word.AsXml())
					);
		}


		BitmapSource featImg;
		int featDataY, featDataX;
		public void ComputeFeatures(HwrPageImage hwrPage)
		{
			int topXoffset;

			int x0Est = Math.Max(0, (int)(left + BottomXOffset - 500 + 0.5));
			int x1Est = Math.Min(hwrPage.Width, (int)(right + 500 + 0.5));
			int y0 = (int)(top + 0.5);
			int y1 = (int)(bottom + 0.5);

			ImageStruct<float> data = ImageProcessor.ExtractFeatures(hwrPage.Image.CropTo(x0Est, y0, x1Est, y1), out topXoffset);
			featDataY = y0;
			featDataX = (int)x0Est + topXoffset;
			var featImgRGB = data.MapTo(f => (byte)(255.9 * f)).MapTo(b => new PixelArgb32(255, b, b, b));
			foreach (Word w in words)
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
			featImg = featImgRGB.MapTo(p => p.Data).ToBitmap();
			featImg.Freeze();
		}

		public void Retrieve(out BitmapSource featureImage, out Point offset)
		{
			featureImage = featImg;
			offset = new Point(featDataX, featDataY);
		}


		public void DoHMMImprove(HwrPageImage hwrPage, SymbolWidth[] map, int charPhases, HwrOptimizer nativeOptimizer)
		{
			int topXoffset;

			int x0Est = Math.Max(0, (int)(left + BottomXOffset - 10 + 0.5));
			int x1Est = Math.Min(hwrPage.Width, (int)(right - BottomXOffset + 0.5));
			int y0 = (int)(top + 0.5);
			int y1 = (int)(bottom + 0.5);

			Func<char, bool> charKnown = c => map.Where(sym => sym.c == c).Any();

			var basicLine = from word in words
							from letter in word.text.AsEnumerable().Concat(' ')
							select charKnown(letter) ? letter : (char)1;

			basicLine = basicLine.Prepend(' ');//first word should start with space too.

			basicLine = basicLine.Prepend((char)0).Concat((char)10);//overall line starts with 0 and ends with 10.

			var phaseCodeSeq =
				from letter in basicLine
				let code = map.Single(sym => sym.c == letter).code
				from phaseCode in Enumerable.Range((int)code * charPhases, charPhases)
				select (uint)phaseCode;


			int[] charEndPos = nativeOptimizer.SplitWords(
									hwrPage.Image.CropTo(x0Est, y0, x1Est, y1), 
									phaseCodeSeq.ToArray(), 
									out topXoffset, 
									(float)this.shear);
			int x0 = x0Est + topXoffset;

			charEndPos = charEndPos.Where((pos, i) => i % charPhases == charPhases - 1).ToArray();
			int currWord = -1;


			char[] charValue = basicLine.ToArray();

			for (int i = 0; i < charValue.Length; i++)
			{
				if (charValue[i] == ' ')
				{ //found word boundary
					if (currWord >= 0) //then the previous char was the rightmost character of the current word.
					{
						words[currWord].right = x0 + charEndPos[i - 1];
						words[currWord].rightStat = TrackStatus.Calculated;
					}
					currWord++;//space means new word
					if (currWord < words.Length) //then the endpos of the space must be the beginning pos of the current word.
					{
						words[currWord].left = x0 + charEndPos[i];
						words[currWord].leftStat = TrackStatus.Calculated;
					}
				}
			}
			Debug.Assert(currWord == words.Length);
		}
	}
}
