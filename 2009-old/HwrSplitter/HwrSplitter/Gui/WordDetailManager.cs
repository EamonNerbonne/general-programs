using System;
using System.Linq;
using System.Text;
using System.Windows;
using HwrDataModel;

namespace HwrSplitter.Gui
{
	class WordDetailManager
	{
		readonly MainManager man;
		readonly WordDetail wordDetail;

		readonly ClickableTextBlock wordSelector;
		HwrTextLine currentTextLine;


		public WordDetailManager(MainManager man, WordDetail wordDetail) {
			this.man = man;
			this.wordDetail = wordDetail;

			wordSelector = new ClickableTextBlock(wordDetail.WordSelectorTextBlock);
			wordSelector.WordClicked += wordSelector_WordClicked;
		}

		void wordSelector_WordClicked(int wordIndex, string word) {
			WordDisplay(currentTextLine.words[wordIndex]);
		}

		public void WordDisplay(HwrTextWord word) {
			currentTextLine = word.line;
			wordDetail.DisplayLine(man.PageImage, currentTextLine, word);

			wordSelector.WireUpTextBlock(currentTextLine.words.Select(w => w.text).ToArray());
			wordSelector.SelectedIndex = Enumerable.Range(0,currentTextLine.words.Length).Where(i=>currentTextLine.words[i]== word).Single();

			wordDetail.wordContent.Content = DescribeLine(currentTextLine, word);

			wordDetail.imgRect = new Rect(
				currentTextLine.left + Math.Min(0, currentTextLine.BottomXOffset), //x
				currentTextLine.top, //y
				currentTextLine.right - currentTextLine.left + Math.Abs(currentTextLine.BottomXOffset),
				currentTextLine.bottom - currentTextLine.top);
			wordDetail.redisplay();
			wordDetail.displayFeatures(currentTextLine);
		}

		static string DescribeLine(HwrTextLine textline, HwrTextWord word) {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Line: [{0:f2},{1:f2}), length={2:f2}, likelihood={3}\n", textline.left, textline.right, textline.right - textline.left,textline.ComputedLikelihood);
			sb.AppendFormat("Word: [{0:f2},{1:f2}), length={2:f2}, est={3:f2} ~ {4:f2}\n", word.left, word.right, word.right - word.left, word.symbolBasedLength.Mean, Math.Sqrt(word.symbolBasedLength.Variance));
			return sb.ToString();
		}
	}
}
