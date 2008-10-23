using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using EmnExtensions.Text;

namespace EmnExtensions.WPF
{
    public class LogControl : TextBox
    {

        StringBuilder sb = new StringBuilder();
        bool redraw = false;

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
        public void ClaimStandardOut() {
            Console.SetOut(new DelegateTextWriter(AppendThreadSafe));
        }
    }
}
