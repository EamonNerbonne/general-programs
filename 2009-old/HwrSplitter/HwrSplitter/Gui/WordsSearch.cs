using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;
using HwrDataModel;
using System.Windows;

namespace HwrSplitter.Gui
{
    static class WordsSearch
    {
        struct TextLineVerticalComparer : IComparer<HwrTextLine>
        {
            //return 0 on any overlap;
            public int Compare(HwrTextLine x, HwrTextLine y) {
                if (x.bottom < y.top) return -1;
                if (x.top > y.bottom) return 1;
                return 0;
            }

        }
        struct WordHorizComparer : IComparer<HwrTextWord>
        {

            //return 0 on any overlap;
            public int Compare(HwrTextWord x, HwrTextWord y) {
                if (x.right < y.left) return -1;
                if (x.left > y.right) return 1;
                return 0;
            }
        }


    }
}
