using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace RealSimilarityMds
{
    public class ProgressManager
    {
        ProgressBar progressBar;
        Label label;
        bool redrawPending = false;
        double progressVal;
        DateTime actionStart;
        DateTime nextEtaUpdate;
        string taskName=null;
        double taskLength;
        public ProgressManager(ProgressBar progressBar, Label label) {
            this.progressBar = progressBar;
            progressBar.Minimum = 0.0;
            progressBar.Maximum = 1.0;
            this.label = label;
        }
        object syncroot = new object();
        public void NewTask(string taskName, double taskLength) {
            lock (syncroot) {
                progressVal = 0.0;
                this.taskName = taskName;
                this.taskLength = taskLength;
                this.actionStart = DateTime.Now;
                nextEtaUpdate = DateTime.Now;
                RegUpdate();
            }
        }

        public void SetProgress(double newVal) {
            lock (syncroot) {
                progressVal = newVal;
                RegUpdate();
            }
        }

        private void RegUpdate() {
            if (redrawPending) return;
            redrawPending = true;
            progressBar.Dispatcher.BeginInvoke((Action)Redraw);
        }

        private void Redraw() {
            double pos;
            string etaString=null;
            lock (syncroot) {
                redrawPending = false;
                pos = progressVal / taskLength;
                if (DateTime.Now >= nextEtaUpdate && pos > 0) {
                    TimeSpan eta = TimeSpan.FromSeconds((DateTime.Now - actionStart).TotalSeconds * (1.0 - pos) / pos);// .ToLongTimeString();
                    etaString = eta.ToString() + " (" + taskName + ")";
                    nextEtaUpdate = DateTime.Now + TimeSpan.FromSeconds(1.0);
                }
            }
            progressBar.Value = pos;
            if (etaString != null) 
            label.Content = etaString;
        }
    }
}
