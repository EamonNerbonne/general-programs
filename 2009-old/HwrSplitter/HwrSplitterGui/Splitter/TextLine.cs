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

		public int bodyTop;
		public int bodyBot;
		public float[,] features;

		public double cost = double.NaN;

		public TextLine() { }
		public TextLine(string text, int no, double top, double bottom, double left, double right, double shear, Dictionary<char, SymbolWidth> symbolWidths)
			: base(top, bottom, left, right, shear) {
			this.no = no;
			this.words = text
				.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
				.Select((t, i) => new Word(t, i + 1, top, bottom, 0.0, 0.0, shear))
			.ToArray();
			GuessWordsInString(symbolWidths);
		}


		private void GuessWordsInString( Dictionary<char, SymbolWidth> symbolWidths) {
			foreach (var word in words)
				word.EstimateLength(symbolWidths);

			var lengthEstimates = words.Select(word => word.symbolBasedLength);

			LengthEstimate
				start = symbolWidths[(char)0].estimate,
				end = symbolWidths[(char)10].estimate;

			LengthEstimate totalEstimate = start + lengthEstimates.Aggregate((a, b) => a + b) + end;
			double wordwiseStddevTotal = start.stddev + lengthEstimates.Select(est=>est.stddev).Sum() + end.stddev;

			//ok, so we have a total line length and a per word estimate
			double correctionPerStdDev = (right - left-totalEstimate.len) / wordwiseStddevTotal;
			double position = left + start.len+ start.stddev *correctionPerStdDev;
			foreach(Word word in words) {
				word.left = position;
				position += word.symbolBasedLength.len + word.symbolBasedLength.stddev * correctionPerStdDev;
				word.right = position;
			}
			position += end.len+ end.stddev * correctionPerStdDev;
			Debug.Assert(Math.Abs(position - right) < 1, "math error");
		}
		public TextLine(XElement fromXml)
			: base(fromXml) {
			no = (int)fromXml.Attribute("no");
			words = fromXml.Elements("Word").Select(xmlWord => new Word(xmlWord)).ToArray();
		}

		public XNode AsXml() {
			return new XElement("TextLine",
				new XAttribute("no", no),
				base.MakeXAttrs(),
				words.Select(word => word.AsXml())
					);
		}

		BitmapSource featImg;
		int featDataY, featDataX;
		public void ComputeFeatures(HwrPageImage hwrPage) {
			int topXoffset;

			int x0Est = Math.Max(0, (int)(left + BottomXOffset - 500 + 0.5));
			int x1Est = Math.Min(hwrPage.Width, (int)(right + 500 + 0.5));
			int y0 = (int)(top + 0.5);
			int y1 = (int)(bottom + 0.5);

			ImageStruct<float> data = ImageProcessor.ExtractFeatures(hwrPage.Image.CropTo(x0Est, y0, x1Est, y1), out topXoffset);
			featDataY = y0;
			featDataX = (int)x0Est + topXoffset;
			var featImgRGB = data.MapTo(f => (byte)(255.9 * f)).MapTo(b => new PixelArgb32(255, b, b, b));
			foreach (Word w in words) {
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
			featImg = featImgRGB.MapTo(p => p.Data).ToBitmap();
			featImg.Freeze();
		}

		public void Retrieve(out BitmapSource featureImage, out Point offset) {
			featureImage = featImg;
			offset = new Point(featDataX, featDataY);
		}


	}
}
