using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace EmnImageTestDisplay {
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window {
        StringBuilder sb = new StringBuilder();
        bool redraw = false;

        public ProgressWindow() {
            InitializeComponent();
        }
        public void AppendLine(string line) {
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
        public void Append(string text) {
            lock (sb) {
                sb.Append(text);
                Invalidate();
            }
        }
        private void UpdateStringUI() {
            lock (sb) {
                if (redraw) {
                    redraw = false;
                    ProgressTextBox.Text += sb.ToString();
                    sb.Length = 0;
                    ProgressTextBox.ScrollToEnd();

                    if (!this.IsVisible)
                        Show();

                }
            }
        }

       
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(this.Hide));
            //the beginInvoke is necessary since you can't Hide a Closing window... phooey!
            base.OnClosing(e);
        }

    }
}
