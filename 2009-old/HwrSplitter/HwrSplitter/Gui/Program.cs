using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using HwrDataModel;
using System.Xml.Linq;
using System.IO;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using EmnExtensions.DebugTools;
using EmnExtensions.Filesystem;
using EmnExtensions.Text;
using System.Windows.Input;
using HwrSplitter.Engine;

namespace HwrSplitter.Gui
{
	class Program : Application
	{
		[STAThread]
		public static void Main(string[] args) { new Program().Exec(); }
		MainWindow mainWindow;
		public Program() {
			mainWindow = new MainWindow();
			this.ShutdownMode = ShutdownMode.OnMainWindowClose;//TODO:add save warning.
		}
		public void Exec() { this.Run(mainWindow); } //TODO:MainWindow.

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			new Thread(EngineDataLoader) {
				IsBackground = true,
				Name = "EngineDataLoader"
			}.Start();
		}

		void EngineDataLoader() {
			using (var engineData = new EngineData(mainWindow.Manager)) {
				engineData.Load();
				engineData.StartLearning();
			}
		}
	}
}
