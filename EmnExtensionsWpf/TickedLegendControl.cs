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
using System.Globalization;

namespace EmnExtensions.Wpf
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:EmnExtensions.Wpf"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:EmnExtensions.Wpf;assembly=EmnExtensions.Wpf"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:TickedLegendControl/>
    ///
    /// </summary>
    public class TickedLegendControl : Control
    {
        Typeface labelType;
        double fontSize = 10.0*4.0/3.0;
        static Pen tickPen;
        static TickedLegendControl() {
            tickPen = new Pen(Brushes.Black, 1.0);
            tickPen.StartLineCap = PenLineCap.Round;
            tickPen.EndLineCap = PenLineCap.Round;
            tickPen.LineJoin = PenLineJoin.Round;
            tickPen.Freeze();

            DefaultStyleKeyProperty.OverrideMetadata(typeof(TickedLegendControl), new FrameworkPropertyMetadata(typeof(TickedLegendControl)));
        }

        public TickedLegendControl() {
            labelType = new Typeface(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, new FontFamily("Verdana"));
            StartVal = 0;
            EndVal = 1;
        }

        double startVal, endVal;
        /// <summary>
        /// Left or bottom
        /// </summary>
        public double StartVal { get { return startVal; } set { startVal = value; InvalidateVisual(); } }
        /// <summary>
        /// Right or top
        /// </summary>
        public double EndVal { get { return endVal; } set { endVal = value; InvalidateVisual(); } }


        public enum Side { Top, Right, Bottom, Left }
        Side snapTo = Side.Top;
        public Side SnapTo { get { return snapTo; } set { snapTo = value; InvalidateVisual(); } }

        CultureInfo cachedCulture;
        int orderOfMagnitude;



        protected override void OnRender(DrawingContext drawingContext) {
            //base.OnRender(drawingContext);//not used.

            bool ticksAlongWidth = snapTo==Side.Top ||snapTo==Side.Bottom;
            double pixelsWide = ticksAlongWidth?ActualWidth:ActualHeight;

            if (pixelsWide <= 0 || 
                !pixelsWide.IsFinite() || 
                (endVal == startVal) || 
                !endVal.IsFinite() ||
                !startVal.IsFinite())
                return;// no point!


            cachedCulture = CultureInfo.CurrentCulture;
            orderOfMagnitude = (int)Math.Floor(Math.Log10(Math.Max(Math.Abs(startVal), Math.Abs(endVal))));

            Matrix mirrTrans = Matrix.Identity; //top-left is 0,0, so if you're on the bottom you're happy
            if(snapTo == Side.Right || snapTo == Side.Bottom)
                mirrTrans.ScaleAt(1.0, -1.0, 0.0, (ActualHeight+ActualWidth-pixelsWide) / 2.0);
            if(snapTo == Side.Right || snapTo == Side.Left) {
                mirrTrans.Rotate(-90.0);
                mirrTrans.Translate(0.0, pixelsWide);
            }
            //now mirrTrans projects from an "ideal" SnapTo-Top world-view to what we need.
            StreamGeometry geom = DrawTicks(pixelsWide, (val, pixPos) => {
                FormattedText text = MakeText(val);
                double altitudeCenter = 17 + (ticksAlongWidth ? text.Height : text.Width) / 2.0;
                Point textPos= mirrTrans.Transform(new Point(pixPos, altitudeCenter));
                //but we need to shift from center to top-left...
                textPos.Offset(text.Width / -2.0, text.Height / -2.0);
                drawingContext.DrawText(text, textPos);
            });
            geom.Transform = new MatrixTransform(mirrTrans);

            drawingContext.DrawGeometry(null, tickPen, geom);
        }


        
        FormattedText MakeText(double val) {
            string numericValueString = (val / Math.Pow(10, orderOfMagnitude)).ToString("f1");
            return new FormattedText(numericValueString, cachedCulture, FlowDirection.LeftToRight, labelType, fontSize, Brushes.Black);
        }

        StreamGeometry DrawTicks(double pixelsWide, Action<double,double> rank0ValAtPixel) {
            double scale=pixelsWide/(endVal- startVal);
            StreamGeometry geom = new StreamGeometry();
            using (var context = geom.Open())        {
                FindAllTicks(pixelsWide / 100, Math.Min(startVal, endVal), Math.Max(startVal, endVal), (val, rank) => {
                    double pixelPos = (val - startVal)*scale;

                    context.BeginFigure(new Point(pixelPos, 0), false, false);
                    context.LineTo(new Point(pixelPos, (4 - rank) * 4), true, false);

                    if (rank == 0) rank0ValAtPixel(val, pixelPos);
                });
            }
            return geom;
        }


        static void FindAllTicks(double preferredNum, double minVal,double maxVal, Action<double,int> foundTickWithRank) {
            double firstTickAt, totalSlotSize;
            int[] subDivTicks;
            int orderOfMagnitude;
            CalcTickPositions(minVal, maxVal, preferredNum, out firstTickAt, out totalSlotSize, out subDivTicks, out orderOfMagnitude);
            //we want the first tick to start before the range
            firstTickAt-=totalSlotSize;
            //convert subDivTicks into multiples:
            for (int i = subDivTicks.Length - 2; i >= 0; i--)
                subDivTicks[i] = subDivTicks[i] * subDivTicks[i + 1];
            double subSlotSize = totalSlotSize / subDivTicks[0];

            int firstSubTickMult = (int)Math.Ceiling((minVal-firstTickAt) /subSlotSize);//some positive number;

            for (int i = firstSubTickMult; firstTickAt + subSlotSize * i < maxVal; i++) {
                int rank = 0;
                while(rank <subDivTicks.Length && i % subDivTicks[rank]!=0) rank++;
                foundTickWithRank(firstTickAt + subSlotSize * i, rank);
            }
        }

        /// <summary>
        /// Calculates optimal parameters for the placements of the legend
        /// </summary>
        /// <param name="minVal">the start of the range to be ticked</param>
        /// <param name="maxVal">the end of the range to be ticked</param>
        /// <param name="preferredNum">the preferred number of labelled ticks.  This method will deviate by at most a factor 1.5 from that</param>
        /// <param name="firstTickAt">output: where the first tick should be placed</param>
        /// <param name="slotSize">output: the distance between consequetive ticks</param>
        /// <param name="ticks">output: the additional order of subdivisions each slot can be divided into.
        /// This value aims to have around 10 subdivisions total, slightly more when the actual number of slots is fewer than requested
        /// and slightly less when the actual number of slots greater than requested.</param>
        public static void CalcTickPositions(double minVal, double maxVal, double preferredNum, out double firstTickAt, out double slotSize, out int[] ticks, out int orderOfMagnitude) {
            double range = maxVal - minVal;
            double idealSlotSize = range / (preferredNum + 1);
            orderOfMagnitude = (int)Math.Floor(Math.Log10(idealSlotSize)); //i.e  for 143 or 971 this is 2
            double relSlotSize = idealSlotSize / Math.Pow(10, orderOfMagnitude); //some number between 1 and 10
            int fixedSlot;

            if (relSlotSize < 1.42) {
                fixedSlot = 1;
                ticks = new[] { 2, 5 };
            } else if (relSlotSize < 3.16) {
                fixedSlot = 2;
                ticks = relSlotSize < 2 ? new[] { 2, 2, 5 } : new[] { 2, 2 };
            } else if (relSlotSize < 7.07) {
                fixedSlot = 5;
                ticks = new[] { 5, 2 };
            } else {
                fixedSlot = 10;
                ticks = new[] { 2, 5, 2 };
            }
            slotSize = fixedSlot * Math.Pow(10, orderOfMagnitude);


            //now to find the first tick!

            firstTickAt = Math.Ceiling(minVal / slotSize) * slotSize;

        }



    }
}
