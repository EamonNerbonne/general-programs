using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using EmnExtensions;

namespace SimilarityMdsLib
{
    public interface IProgressManager
    {
        void NewTask(string taskName);
        void Done();
        void SetProgress(double newVal);
        void SetProgress(double newVal, string msg);
    }

    public class NullProgressManager:IProgressManager
    {
        

        public void NewTask(string taskName) {
        }

        public void Done() {
        }

        public void SetProgress(double newVal) {
        }

        public void SetProgress(double newVal, string msg) {
        }
    }

    public class ProgressManager :IProgressManager
    {
        ProgressBar progressBar;
        Label label;
        bool redrawPending = false;
        double progressVal;
        DateTime actionStart;
        DateTime nextEtaUpdate;
        string taskName=null;
        public readonly NiceTimer timer;
        public ProgressManager(ProgressBar progressBar, Label label, NiceTimer timer) {
            this.progressBar = progressBar;
            progressBar.Minimum = 0.0;
            progressBar.Maximum = 1.0;
            this.label = label;
            this.timer = timer;
        }
        object syncroot = new object();
        public void NewTask(string taskName) {
            timer.TimeMark(taskName);
            lock (syncroot) {
                progressVal = 0.0;
                this.taskName = taskName;
                this.actionStart = DateTime.Now;
                nextEtaUpdate = DateTime.Now;
                RegUpdate();
            }
        }
        public void Done() {
            timer.Done();
            lock (syncroot) {
                progressVal = 1.0;
                taskName = "Done:"+taskName;
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

        public void SetProgress(double newVal,string msg) {
            timer.TimeMark(msg);
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
                pos = progressVal;
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
