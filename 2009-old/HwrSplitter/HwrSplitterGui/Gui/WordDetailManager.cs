using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataIO;
using System.Windows;

namespace HwrSplitter.Gui
{
	class WordDetailManager
	{
		MainManager man;
		WordDetail wordDetail;

		ClickableTextBlock wordSelector;
		TextLine currentTextLine;


		public WordDetailManager(MainManager man, WordDetail wordDetail) {
			this.man = man;
			this.wordDetail = wordDetail;

			wordSelector = new ClickableTextBlock(wordDetail.WordSelectorTextBlock);
			wordSelector.WordClicked += new Action<int, string>(wordSelector_WordClicked);
		}

		void wordSelector_WordClicked(int wordIndex, string word) {
			WordDisplay(currentTextLine, wordIndex);
		}

		public void WordDisplay(TextLine textline, int wordIndex) {
			currentTextLine = textline;
			Word word = textline.words[wordIndex];
			wordDetail.DisplayLine(man.optimizer,man.PageImage, textline, word);

			wordSelector.WireUpTextBlock(textline.words.Select(w => w.text).ToArray());
			wordSelector.SelectedIndex = wordIndex;

			wordDetail.wordContent.Content = DescribeLine(textline, word);

			wordDetail.imgRect = new Rect(
				textline.left + Math.Min(0, textline.BottomXOffset), //x
				textline.top, //y
				textline.right - textline.left + Math.Abs(textline.BottomXOffset),
				textline.bottom - textline.top);
			wordDetail.redisplay();
			wordDetail.displayFeatures(textline);
		}

		private string DescribeLine(TextLine textline, Word word) {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Line: [{0:f2},{1:f2}), length={2:f2}\n", textline.left, textline.right, textline.right - textline.left);
			sb.AppendFormat("Word: [{0:f2},{1:f2}), length={2:f2}, est={3:f2} ~ {4:f2}\n", word.left, word.right, word.right - word.left, word.symbolBasedLength.len, Math.Sqrt(word.symbolBasedLength.var));
			return sb.ToString();
		}


	}
}
