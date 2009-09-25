﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HwrDataModel;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;

namespace HwrSplitter.Gui
{
    public class HwrImageAnnotater
    {
        ImageAnnotViewbox imageView;
        MainManager man;
        public HwrImageAnnotater(MainManager man, ImageAnnotViewbox imageView) {
            this.imageView = imageView;
            this.man = man;
            imageView.MouseLeftButtonDown += new MouseButtonEventHandler(imgView_MouseLeftButtonDown);
        }

        void imgView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            man.SelectPoint(e.MouseDevice.GetPosition(imageView.ImageCanvas));
        }

        private static Brush stat2brush(HwrEndpointStatus stat) {
            switch (stat) {
				case HwrEndpointStatus.Uninitialized: return Brushes.Pink;
				case HwrEndpointStatus.Initialized: return Brushes.Blue;
				case HwrEndpointStatus.Calculated: return Brushes.Purple;
				case HwrEndpointStatus.Manual: return Brushes.Red;
                default: throw new NotImplementedException("This track status does not exist!");
            }
        }
        public void ProcessWord(HwrTextWord word) {
            ProcessWords(Enumerable.Repeat(word,1));
        }
        public void ProcessLine(HwrTextLine line) {
            ProcessWords(line.words);
        }
        public void ProcessLines(IEnumerable<HwrTextLine> lines) {//TODO:give better name
            ProcessWords(lines.SelectMany(tl => tl.words));
        }
        void ProcessWords(IEnumerable<HwrTextWord> words) {
            imageView.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action<IEnumerable<HwrTextWord>>(this.ProcessLinesUI),
                words);
        }

        void ProcessLinesUI(IEnumerable<HwrTextWord> words) {
            foreach (HwrTextWord word in words)
                DrawWordLinesUI(word);
        }

        private static void setLine(Line line, double x1, double y1, double x2, double y2, Brush brush) {
            line.Stroke = brush;
            line.X1 = x1;
            line.Y1 = y1;
            line.X2 = x2;
            line.Y2 = y2;
            line.StrokeStartLineCap = PenLineCap.Round;
            line.StrokeEndLineCap = PenLineCap.Round;
            line.StrokeThickness = 2;
        }
        
        class LinePair
        {
            public Line left, right;
        }


        public void DrawWordLinesUI(HwrTextWord word) {
            LinePair lines;
            if (word.guiTag as LinePair != null)
                lines = (LinePair)word.guiTag;
            else {
                word.guiTag = lines = new LinePair();
                imageView.ImageCanvas.Children.Add(lines.left = new Line());
                imageView.ImageCanvas.Children.Add(lines.right = new Line());
            }
            var xcorr = word.BottomXOffset;
            //yield return mkLine(word.left, word.top, word.right, word.top, brush);
            setLine(lines.right,word.right, word.top, word.right + xcorr, word.bottom, stat2brush(word.rightStat));
            //yield return mkLine(word.right + xcorr, word.bottom, word.left + xcorr, word.bottom, brush);
            setLine(lines.left,word.left + xcorr, word.bottom, word.left, word.top, stat2brush(word.leftStat));
        }


    }
}
