using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using HwrDataModel;
using System.Windows;
using System.Windows.Threading;
using HwrSplitter.Engine;
using System.Threading;

namespace HwrSplitter.Gui
{
	public class MainManager
	{
		public TextLineCostOptimizer optimizer;

		public Point LastClickPoint { get; private set; }
		public WordsImage words;


		public MainManager(MainWindow mainWindow) {
			Window = mainWindow;
			ImageAnnotater = new HwrImageAnnotater(this, Window.ImageAnnotViewbox);
			zoomRectMan = new ZoomRectManager(this, Window.ZoomRect);
			wordDetailMan = new WordDetailManager(this, Window.WordDetail);

		}
		public MainWindow Window { get; private set; }
		ZoomRectManager zoomRectMan;
		WordDetailManager wordDetailMan;

		public HwrImageAnnotater ImageAnnotater { get; private set; }
		HwrPageImage pageImage;
		public HwrPageImage PageImage { get { return pageImage; } set { pageImage = value; Window.ImageAnnotViewbox.SetImage(pageImage); } }


		public void SelectPoint(Point imagePoint) {
			LastClickPoint = imagePoint;
			if (words != null) {
				Word target = words.FindWord(imagePoint);
				if (target != null) {
					wordDetailMan.WordDisplay(target);
				}
			}
		}

		object sync = new object(); //not truly crucial.
		bool paused;
		readonly object pausedSync = new object();//used to implement pausing of background thread;

		public bool Paused {
			get {
				lock (sync) return paused;
			}
			set {
				lock (sync) {
					if (paused == value)
						return;
					paused = value;
					if (paused)
						Monitor.Enter(pausedSync);
					else
						Monitor.Exit(pausedSync);
				}
			}
		}

		public void WaitWhilePaused() {
			lock (pausedSync) { }
		}


	}
}
