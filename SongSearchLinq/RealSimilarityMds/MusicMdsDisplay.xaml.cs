﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using EmnExtensions.Wpf;

namespace RealSimilarityMds
{
    public partial class MusicMdsDisplay : Window
    {
        ProgressManager progress;
        Program program;
        public AutoHistogram HistogramControl { get { return this.distanceHistoview; } }
        public MusicMdsDisplay() {
            InitializeComponent();
            progress = new ProgressManager(progressBar, labelETA);
            program = new Program(progress,this);
            program.RunInBackground();

        }
    }
}
