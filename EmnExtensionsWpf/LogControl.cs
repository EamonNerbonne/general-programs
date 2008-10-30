using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using EmnExtensions.Text;
using System.IO;
using System.Threading;
using EmnExtensionsNative;

namespace EmnExtensions.WPF
{
    public class LogControl : TextBox
    {
        public LogControl() {
            logger = new DelegateTextWriter(AppendThreadSafe);
        }

        StringBuilder sb = new StringBuilder();
        bool redraw = false;
        DelegateTextWriter logger;
        TextWriter oldOut,oldError;


        public void AppendLineThreadSafe(string line) {
            lock (sb) {
                sb.AppendLine(line);
                Invalidate();
            }
        }

        private void Invalidate() {
            if (!redraw) {
                redraw = true;
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(UpdateStringUI));
            }
        }

        public void AppendThreadSafe(string text) {
            lock (sb) {
                sb.Append(text);
                Invalidate();
            }
        }

        private void UpdateStringUI() {
            lock (sb) {
                if (redraw) {
                    redraw = false;
                    Text += sb.ToString();
                    sb.Length = 0;
                    ScrollToEnd();
                }
            }
        }

        public bool ClaimStandardOut {
            get {
                return logger == Console.Out;
            }
            set {
                if (ClaimStandardOut != value) {
                    if (value) {
                        oldOut = Console.Out;
                        Console.SetOut(logger);
                        RedirectNativeStream(this, StdoutRedirector.RedirectStdout());

                    } else {
                        Console.SetOut(oldOut);
                    }
                }
            }
        }

        public bool ClaimStandardError {
            get {
                return logger == Console.Error;
            }
            set {
                if (ClaimStandardOut != value) {
                    if (value) {
                        oldError = Console.Error;
                        Console.SetError(logger);
                        RedirectNativeStream(this, StdoutRedirector.RedirectStderr());
                    } else {
                        Console.SetError(oldError);
                    }
                }
            }
        }

        private static void RedirectNativeStream(LogControl toControl, Stream fromNative) {
            Thread bgReader = new Thread(() => {
                using (fromNative)
                using (StreamReader reader = new StreamReader(fromNative)) {
                    char[] buffer = new char[512];
                    while (true) {
                        int actuallyRead = reader.Read(buffer, 0, buffer.Length);
                        if (actuallyRead <= 0) break;
                        toControl.AppendThreadSafe(new string(buffer, 0, actuallyRead));
                    }
                }
            });
            bgReader.IsBackground = true;
            bgReader.Start();
        }

    }
}
