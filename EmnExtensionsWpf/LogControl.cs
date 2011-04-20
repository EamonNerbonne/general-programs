// ReSharper disable UnusedMember.Global
using System;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using EmnExtensions.Text;
using System.IO;
using System.Threading;
using EmnExtensionsNative;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;

namespace EmnExtensions.Wpf {
	public class LogControl : FlowDocumentScrollViewer {

		public static Tuple<Window, TextWriter> ShowNewLogWindow(string windowTitle=null, double? width=null, double? height=null) {
			var logger = new LogControl();
			var win = new Window {
				Title = windowTitle ?? "Log Window",
				Content = logger,
				Background = Brushes.LightGray,
			};
			if (height.HasValue)
				win.Height = height.Value;
			if (width.HasValue)
				win.Width = width.Value;

			win.Show();

			return Tuple.Create(win, logger.Writer);
		}
		public static Tuple<Window, TextWriter> ShowNewLogWindow_NewDispatcher(string windowTitle = null, double? width = null, double? height = null) {
			var disp=WpfTools.StartNewDispatcher();
			return (Tuple<Window, TextWriter>)disp.Invoke((Func<Tuple<Window, TextWriter>>)(() => ShowNewLogWindow(windowTitle, width, height)));
		}
		

		readonly StringBuilder curLine = new StringBuilder();
		readonly DelegateTextWriter logger;
		public LogControl() {
			Document = new FlowDocument {
				TextAlignment = TextAlignment.Left,
				FontFamily = new FontFamily("Consolas"),
				FontSize = 10.0
			};
			logger = new DelegateTextWriter(AppendThreadSafe);
			VerticalContentAlignment = VerticalAlignment.Bottom;
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			Application.Current.MainWindow.Closed += LogControl_Unloaded;
			Application.Current.Exit += LogControl_Unloaded;
			Unloaded += LogControl_Unloaded;
		}


		void LogControl_Unloaded(object sender, EventArgs e) {
			ClaimStandardOut = false;
			ClaimStandardError = false;
		}

		bool redraw;
		TextWriter oldOut, oldError;

		public void AppendLineThreadSafe(string line) {
			AppendThreadSafe(line + "\n");
		}

		public TextWriter Writer { get { return logger; } }

		void Invalidate() {
			if (!redraw) {
				redraw = true;
				Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)UpdateStringUI);
			}
		}

		public void AppendThreadSafe(string text) {
			lock (curLine) {
				curLine.Append(text);
				Invalidate();
			}
		}

		void UpdateStringUI() {
			string strToAppendToCur = null;
			lock (curLine) {
				if (redraw) {
					redraw = false;
					strToAppendToCur = curLine.ToString();
					curLine.Length = 0;
				}
			}
			if (strToAppendToCur != null) {
				Document.ContentEnd.InsertTextInRun(strToAppendToCur);
				NavigationCommands.LastPage.Execute(null, this);//can we say... nasty hack?
			}
		}

		RestoringReadStream stdOutOverride;
		public bool ClaimStandardOut {
			get {
				return stdOutOverride != null;
			}
			set {
				if (ClaimStandardOut != value) {
					if (value) {
						oldOut = Console.Out;
						Console.SetOut(logger);
						RedirectNativeStream(this, stdOutOverride = StdoutRedirector.RedirectStdout(), "stdout");
					} else {
						stdOutOverride.Dispose();
						stdOutOverride = null;
						Console.SetOut(oldOut);
					}
				}
			}
		}

		RestoringReadStream stdErrOverride;
		public bool ClaimStandardError {
			get {
				return stdErrOverride != null;
			}
			set {
				if (ClaimStandardError != value) {
					if (value) {
						oldError = Console.Error;
						Console.SetError(logger);
						RedirectNativeStream(this, stdErrOverride = StdoutRedirector.RedirectStderr(), "stderr");
					} else {
						stdErrOverride.Dispose();
						stdErrOverride = null;
						Console.SetError(oldError);
					}
				}
			}
		}

		static void RedirectNativeStream(LogControl toControl, RestoringReadStream fromNative, string name) {
			new Thread(() => {
				using (var reader = new StreamReader(fromNative.ReadStream)) {
					char[] buffer = new char[4096];
					while (true) {
						int actuallyRead = reader.Read(buffer, 0, buffer.Length);
						if (actuallyRead <= 0) break;
						toControl.AppendThreadSafe(new string(buffer, 0, actuallyRead));
					}
				}
			}) { IsBackground = true }.Start();
		}
	}
}
