using System;
using System.Threading;
using System.Windows;
using HwrSplitter.Engine;

namespace HwrSplitter.Gui
{
	class Program : Application
	{
		[STAThread]
		public static void Main(string[] args) { new Program().Exec(); }

		readonly MainWindow mainWindow;
		public Program() {
			mainWindow = new MainWindow();
			this.ShutdownMode = ShutdownMode.OnMainWindowClose;//TODO:add save warning.
		}
		public void Exec() { Run(mainWindow); } //TODO:MainWindow.

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
