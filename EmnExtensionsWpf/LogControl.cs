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

        List<string> lines = new List<string>();
        int nextLine = 0;
        int nextChar = 0;
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
            string[] newlines = text.Split(splitChars,StringSplitOptions.None);

            lock (curLine) {

                curLine.Append(newlines[0]);
                if (newlines.Length > 1) {
                    lines.Add(curLine.ToString());
                    lines.AddRange(newlines.Take(newlines.Length - 1).Skip(1));
                    curLine.Length = 0;
                    curLine.Append(newlines[newlines.Length - 1]);
                }

                Invalidate();
            }
        }

        private void UpdateStringUI() {
            string strToAppendToCur = null;
            string[] newlines = null;
            string newCur = null;
            lock (curLine) {
                if (redraw) {
                    redraw = false;
                    if( nextLine <lines.Count){ //entire line added
                        strToAppendToCur = lines[nextLine].Substring(nextChar);
                        newlines = lines.Skip(nextLine+1).ToArray();//take full new lines
                        nextLine = lines.Count;
                        newCur = curLine.ToString();
                    }else {//no entire line added
                        strToAppendToCur = curLine.ToString(nextChar,curLine.Length);
                    }
                    nextChar=curLine.Length;
                }
            }
            if (strToAppendToCur != null) {
                insertionPoint.InsertTextInRun(strToAppendToCur);
                if (newlines != null) {
                    foreach (string line in newlines) {
                        // insertionPoint.InsertLineBreak();
                        insertionPoint.InsertTextInRun(line);
                    }
                    //   insertionPoint.InsertLineBreak();
                    insertionPoint.InsertTextInRun(newCur);
                }
            }
            NavigationCommands.LastPage.Execute(null, this);//can we say... nasty hack?

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
