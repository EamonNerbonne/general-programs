using System;
using System.Collections.Generic;
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

namespace EmnExtensions.Wpf
{
    public class LogControl : FlowDocumentScrollViewer
    {
        Paragraph p;
        FlowDocument doc;
        TextPointer insertionPoint;
        public LogControl() {
            p = new Paragraph();
            doc = new FlowDocument(p);
            var style = new Style();
            doc.FontFamily = new FontFamily("Consolas");
            doc.FontSize = 10.0;
            insertionPoint = p.ContentEnd;
            logger = new DelegateTextWriter(AppendThreadSafe);
            this.Document = doc;
        }
        StringBuilder curLine = new StringBuilder();
        bool redraw = false;
        DelegateTextWriter logger;
        TextWriter oldOut,oldError;


        public void AppendLineThreadSafe(string line) {
            AppendThreadSafe(line + "\n");
        }

        private void Invalidate() {
            if (!redraw) {
                redraw = true;
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(UpdateStringUI));
            }
        }

        static char[] splitChars = new char[] { '\n' };
        public void AppendThreadSafe(string text) {
            lock (curLine) {
                curLine.Append(text);
                Invalidate();
            }
        }

        private void UpdateStringUI() {
            string strToAppendToCur = null;
            lock (curLine) {
                if (redraw) {
                    redraw = false;
                    strToAppendToCur = curLine.ToString();
                    curLine.Length = 0;
                }
            }
            if (strToAppendToCur != null) {
                insertionPoint.InsertTextInRun(strToAppendToCur);
                NavigationCommands.LastPage.Execute(null, this);//can we say... nasty hack?
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
                    //    return;
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
                       // return;
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
