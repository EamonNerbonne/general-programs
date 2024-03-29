// ReSharper disable UnusedMember.Global

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EmnExtensions.Text;
using EmnExtensionsNative;

namespace EmnExtensions.Wpf
{
    public sealed class LogControl : FlowDocumentScrollViewer
    {
        public static Tuple<Window, LogControl> ShowNewLogWindow(string windowTitle = null, double? width = null, double? height = null)
        {
            var logger = new LogControl();
            var win = new Window {
                Title = windowTitle ?? "Log Window",
                Content = logger,
                Background = Brushes.LightGray,
            };
            if (height.HasValue) {
                win.Height = height.Value;
            }

            if (width.HasValue) {
                win.Width = width.Value;
            }

            win.Show();

            return Tuple.Create(win, logger);
        }

        //TODO: this doesn't yet work.
        // ReSharper disable once UnusedMember.Local
        static Tuple<Window, LogControl> ShowNewLogWindow_NewDispatcher(string windowTitle = null, double? width = null, double? height = null)
        {
            var disp = WpfTools.WpfTools.StartNewDispatcher();
            return disp.Invoke(() => ShowNewLogWindow(windowTitle, width, height));
        }

        readonly StringBuilder curLine = new();
        readonly DelegateTextWriter logger;

        public LogControl()
        {
            Reset();
            logger = new(AppendThreadSafe);
            VerticalContentAlignment = VerticalAlignment.Bottom;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Application.Current.MainWindow.Closed += LogControl_Unloaded;
            Application.Current.Exit += LogControl_Unloaded;
            Unloaded += LogControl_Unloaded;
            Loaded += LogControl_Loaded;

            var clearLogMenuItem = new MenuItem { Header = "Clear log" };
            clearLogMenuItem.Click += (s, e) => Reset();

            ContextMenu = new() { Items = { clearLogMenuItem } };
        }

        void Reset()
            => Document = new() {
                TextAlignment = TextAlignment.Left,
                FontFamily = new("Consolas"),
                FontSize = 10.0
            };

        bool wantsStdOut, wantsStdErr;

        void LogControl_Loaded(object sender, RoutedEventArgs e)
        {
            ClaimStandardOut = wantsStdOut || ClaimStandardOut;
            ClaimStandardError = wantsStdErr || ClaimStandardError;
        }

        void LogControl_Unloaded(object sender, EventArgs e)
        {
            wantsStdErr = ClaimStandardError;
            wantsStdOut = ClaimStandardOut;
            ClaimStandardOut = false;
            ClaimStandardError = false;
        }

        bool redraw;
        TextWriter oldOut, oldError;

        public void AppendLineThreadSafe(string line)
            => AppendThreadSafe(line + "\n");

        public TextWriter Writer
            => logger;

        void Invalidate()
        {
            if (!redraw) {
                redraw = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)UpdateStringUI);
            }
        }

        public void AppendThreadSafe(string text)
        {
            lock (curLine) {
                //File.AppendAllText(@"C:\logger.log", text, Encoding.UTF8);
                curLine.Append(text);
                Invalidate();
            }
        }

        void UpdateStringUI()
        {
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
                NavigationCommands.LastPage.Execute(null, this); //can we say... nasty hack?
            }
        }

        public string GetContentsUI()
        {
            lock (curLine) {
                Document.ContentEnd.InsertTextInRun(curLine.ToString());
                curLine.Length = 0;
            }

            return new TextRange(Document.ContentStart, Document.ContentEnd).Text;
        }

        RestoringReadStream stdOutOverride;

        public bool ClaimStandardOut
        {
            get => stdOutOverride != null;
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

        public bool ClaimStandardError
        {
            get => stdErrOverride != null;
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

        static void RedirectNativeStream(LogControl toControl, RestoringReadStream fromNative, string name)
            => new Thread(
                () => {
                    using (var reader = new StreamReader(fromNative.ReadStream)) {
                        var buffer = new char[4096];
                        while (true) {
                            var actuallyRead = reader.Read(buffer, 0, buffer.Length);
                            if (actuallyRead <= 0) {
                                break;
                            }

                            toControl.AppendThreadSafe(new(buffer, 0, actuallyRead));
                        }
                    }
                }
            ) { IsBackground = true }.Start();
    }
}
