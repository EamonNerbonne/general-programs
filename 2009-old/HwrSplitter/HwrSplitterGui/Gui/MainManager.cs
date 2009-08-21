using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using DataIO;
using System.Windows;
using System.Windows.Threading;

namespace HwrSplitter.Gui
{
    public class MainManager
    {
        MainWindow window;
        HwrImageAnnotater annotater;
        ZoomRectManager zoomRectMan;
        WordDetailManager wordDetailMan;
		HwrPageImage currentPage;

        public Point LastClickPoint { get; private set; }
        public WordsImage words;

        public MainManager(MainWindow mainWindow) {
            window = mainWindow;
            annotater = new HwrImageAnnotater(this,window.ImageAnnotViewbox);
            zoomRectMan = new ZoomRectManager(this, window.ZoomRect);
            wordDetailMan = new WordDetailManager(this, window.WordDetail);

        }
        public MainWindow Window { get { return window; } }
        public HwrImageAnnotater ImageAnnotater { get { return annotater; } }



        public void SelectPoint(Point imagePoint) {
            LastClickPoint = imagePoint;
            int lineIndex, wordIndex;
            WordsSearch.FindWord(words, imagePoint, out lineIndex, out wordIndex);

            if (lineIndex >= 0 && wordIndex >= 0) {
                wordDetailMan.WordDisplay(words.textlines[lineIndex], wordIndex);
            }
        }


        public void SetImage(HwrPageImage hwrImage) {
			currentPage = hwrImage;
            Window.ImageAnnotViewbox.SetImage(hwrImage);
        }


    }
}
