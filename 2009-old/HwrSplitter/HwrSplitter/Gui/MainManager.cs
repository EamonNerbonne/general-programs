using System;
using System.Linq;
using System.Threading;
using System.Windows;
using EmnExtensions.DebugTools;
using HwrDataModel;
using HwrSplitter.Engine;

namespace HwrSplitter.Gui {
	public class MainManager {
		public HwrPageOptimizer optimizer; //IDisposable

		public MainManager(MainWindow mainWindow) {
			Window = mainWindow;
			ImageAnnotater = new HwrImageAnnotater(this, Window.ImageAnnotViewbox);
			new ZoomRectManager(this, Window.ZoomRect);//hooks itself up
			wordDetailMan = new WordDetailManager(this, Window.WordDetail);
		}

		public MainWindow Window { get; private set; }
		public Point LastClickPoint { get; private set; }
		public HwrImageAnnotater ImageAnnotater { get; private set; }
		public HwrPageImage PageImage { get; private set; }

		readonly WordDetailManager wordDetailMan;
		readonly object pagesync = new object();
		public void DisplayPage(HwrPageImage pageImage) {
			lock (pagesync) {
				if (PageImage != null)
					foreach (var word in PageImage.TextPage.textlines.SelectMany(line => line.words))
						word.guiTag = null;
				PageImage = pageImage;
				Window.Dispatcher.BeginInvoke((Action)UpdatePageUI);
			}
		}

		void UpdatePageUI() {
			lock (pagesync) {
				Window.ImageAnnotViewbox.SetImage(PageImage);
				ImageAnnotater.DrawWords(PageImage.TextPage.textlines.SelectMany(line => line.words));
			}
		}

		public void SelectPoint(Point imagePoint) {
			using (new DTimer("click"))
				lock (pagesync) {
					LastClickPoint = imagePoint;
					HwrTextPage page = PageImage.TextPage;
					if (page != null) {
						HwrTextWord target = page.FindWord(imagePoint);
						if (target != null)
							wordDetailMan.WordDisplay(target);
					}
				}
		}

		readonly object pausesync = new object(); //not truly crucial.
		bool paused;
		readonly object pausedSync = new object();//used to implement pausing of background thread;

		public bool Paused {
			get {
				lock (pausesync) return paused;
			}
			set {
				lock (pausesync) {
					if (paused == value) return;
					if (value)
						Monitor.Enter(pausedSync, ref paused);
					else {
						Monitor.Exit(pausedSync);
						paused = false;
					}
				}
			}
		}

		public void WaitWhilePaused() { lock (pausedSync) { } }
	}
}
