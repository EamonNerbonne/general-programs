using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataIO;
using System.Windows;

namespace HwrSplitter.Gui
{
    static class WordsSearch
    {
        struct TextLineVerticalComparer : IComparer<TextLine>
        {
            //return 0 on any overlap;
            public int Compare(TextLine x, TextLine y) {
                if (x.bottom < y.top) return -1;
                if (x.top > y.bottom) return 1;
                return 0;
            }

        }
        struct WordHorizComparer : IComparer<Word>
        {

            //return 0 on any overlap;
            public int Compare(Word x, Word y) {
                if (x.right < y.left) return -1;
                if (x.left > y.right) return 1;
                return 0;
            }
        }

        public static void FindWord(WordsImage words, Point point, out int lineIndex, out int wordIndex) {
            if (words == null) {
                lineIndex = -1;
                wordIndex = -1;
                return;
            }

            lineIndex = Array.BinarySearch(words.textlines,//TODO - threadsafe?
                new TextLine {
                    top = point.Y,
                    bottom = point.Y
                }, new TextLineVerticalComparer());
            if (lineIndex >= 0) {
                TextLine target = words.textlines[lineIndex];
                //fix shear:
                double yoff = point.Y - target.top;
                double shearEffect = yoff * Math.Tan(-2 * Math.PI * target.shear / 360.0);
                double correctedX = point.X - shearEffect;

                wordIndex = Array.BinarySearch(target.words, new Word {
                    right = correctedX,
                    left = correctedX
                }, new WordHorizComparer());

            } else
                wordIndex = -1;
        }

    }
}
