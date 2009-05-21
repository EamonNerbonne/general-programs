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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace EmnExtensions.Wpf.Plot
{
	public class TickedLegendControl : FrameworkElement
	{

		Typeface labelType;
		double fontSize = 12.0 * 4.0 / 3.0;//12pt = 12 pixels at 72dpi = 16pixels at 96dpi
		Pen tickPen;
		public Brush TickColor {
			set {
				tickPen = new Pen(value, 1.5);
				tickPen.StartLineCap = PenLineCap.Round;
				tickPen.EndLineCap = PenLineCap.Round;
				tickPen.LineJoin = PenLineJoin.Round;
				tickPen.Freeze();
				InvalidateVisual();
			}
			get {
				return tickPen.Brush;
			}
		}



		public TickedLegendControl() {
			TickColor = Brushes.Black;
			labelType = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, new FontFamily("Verdana"));
		}
		GraphControl watchedGraph;
		public GraphControl Watch {
			get { return watchedGraph; }
			set {
				if (watchedGraph != null) {
					try {
						watchedGraph.GraphBoundsUpdated -= OnGraphBoundsUpdated;
					} catch { } //who cares if it doesn't work
				}
				watchedGraph = value;
				if (watchedGraph != null) {
					watchedGraph.GraphBoundsUpdated += OnGraphBoundsUpdated;
					TickColor = watchedGraph.GraphLineColor;
					LegendLabel = (IsHorizontal ? watchedGraph.XLabel : watchedGraph.YLabel) ?? watchedGraph.Name;
					OnGraphBoundsUpdated(watchedGraph, watchedGraph.GraphBounds);
				} else {
					this.Visibility = Visibility.Collapsed;
				}
			}
		}

		void OnGraphBoundsUpdated(GraphControl graph, Rect newBounds) {
			if (graph != watchedGraph) return;//shouldn't happen, but heck;
			if (newBounds.IsEmpty) {
				StartVal = double.NaN;
				EndVal = double.NaN;
				Visibility = Visibility.Collapsed;
			} else {
				if (IsHorizontal) {
					StartVal = newBounds.Left;
					EndVal = newBounds.Right;
				} else {
					StartVal = newBounds.Top; //these are inverted since WPF has 0 at the top, and we work with a flipped coordinate system!
					EndVal = newBounds.Bottom;
				}
				Visibility = Visibility.Visible;
			}
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

		string legendLabel = "<unset>";
		public string LegendLabel { get { return legendLabel; } set { legendLabel = value; InvalidateVisual(); } }

		public enum Side { Top, Right, Bottom, Left }
		Side snapTo = Side.Top;
		public Side SnapTo { get { return snapTo; } set { snapTo = value; InvalidateVisual(); } }

		CultureInfo cachedCulture;
		int orderOfMagnitude, orderOfMagnitudeDiff;//TODO:really, these should be calculated in measure.  The measure pass should have everything measured!
		bool IsHorizontal { get { return snapTo == Side.Bottom || snapTo == Side.Top; } }

		protected override Size MeasureOverride(Size constraint) {
			if (Visibility == Visibility.Collapsed || startVal == endVal)
				return new Size(0, 0);
			cachedCulture = CultureInfo.CurrentCulture;

			orderOfMagnitudeDiff = (int)Math.Floor(Math.Log10(Math.Abs(startVal - endVal))) - 2;
			orderOfMagnitude = (int)Math.Floor(Math.Log10(Math.Max(Math.Abs(startVal), Math.Abs(endVal))));
			if (Math.Abs(orderOfMagnitude) < 4) orderOfMagnitude = 0;
			FormattedText text = MakeText(8.88888888888888888);
			FormattedText baseL, powL, textL;
			double labelWidth;
			MakeLegendText(out baseL, out powL, out textL, out labelWidth);

			if (IsHorizontal)
				return new Size(
					Math.Max(constraint.Width.IsFinite() ? constraint.Width : 0, labelWidth),
					Math.Max(constraint.Height.IsFinite() ? constraint.Height : 0, 17 + text.Height + textL.Height)
					);
			else
				return new Size(
					Math.Max(17 + text.Width + textL.Height + baseL.Height * 0.1, constraint.Width.IsFinite() ? constraint.Width : 0),
					Math.Max(constraint.Height.IsFinite() ? constraint.Height : 0, labelWidth)
					);
		}


		protected override void OnRender(DrawingContext drawingContext) {
			if (Visibility != Visibility.Visible)
				return;

			double ticksPixelsDim = IsHorizontal ? ActualWidth : ActualHeight;

			if (ticksPixelsDim <= 0 ||
				!ticksPixelsDim.IsFinite() ||
				(endVal == startVal) ||
				!endVal.IsFinite() ||
				!startVal.IsFinite())
				return;// no point!


			cachedCulture = CultureInfo.CurrentCulture;
			orderOfMagnitudeDiff = (int)Math.Floor(Math.Log10(Math.Abs(startVal - endVal))) - 2;
			orderOfMagnitude = (int)Math.Floor(Math.Log10(Math.Max(Math.Abs(startVal), Math.Abs(endVal))));
			if (Math.Abs(orderOfMagnitude) < 4) orderOfMagnitude = 0;

			Matrix mirrTrans = Matrix.Identity; //top-left is 0,0, so if you're on the bottom you're happy
			if (snapTo == Side.Right || snapTo == Side.Bottom)
				mirrTrans.ScaleAt(1.0, -1.0, 0.0, (ActualHeight + ActualWidth - ticksPixelsDim) / 2.0);
			if (snapTo == Side.Right || snapTo == Side.Left) {
				mirrTrans.Rotate(-90.0);
				mirrTrans.Translate(0.0, ticksPixelsDim);
			}
			double textHMax = 0.0;
			//now mirrTrans projects from an "ideal" SnapTo-Top world-view to what we need.
			StreamGeometry geom = DrawTicks(ticksPixelsDim, (val, pixPos) => {
				FormattedText text = MakeText(val / Math.Pow(10, orderOfMagnitude));
				double textH = (IsHorizontal ? text.Height : text.Width);
				double altitudeCenter = 17 + textH / 2.0;
				if (textH > textHMax) textHMax = textH;
				Point textPos = mirrTrans.Transform(new Point(pixPos, altitudeCenter));
				//but we need to shift from center to top-left...
				textPos.Offset(text.Width / -2.0, text.Height / -2.0);
				drawingContext.DrawText(text, textPos);
			});
			geom.Transform = new MatrixTransform(mirrTrans);

			drawingContext.DrawGeometry(null, tickPen, geom);

			FormattedText baseL, powL, textL;
			double totalLabelWidth;
			MakeLegendText(out baseL, out powL, out textL, out totalLabelWidth);
			double centerPix = ticksPixelsDim / 2;

			if (!IsHorizontal) textHMax += baseL.Height * 0.1;
			double centerAlt = 17 + textHMax + textL.Height / 2.0;

			Point labelPos = mirrTrans.Transform(new Point(centerPix, centerAlt));

			if (snapTo == Side.Right)
				drawingContext.PushTransform(new RotateTransform(90.0, labelPos.X, labelPos.Y));
			else if (snapTo == Side.Left)
				drawingContext.PushTransform(new RotateTransform(-90.0, labelPos.X, labelPos.Y));

			labelPos.Offset(-totalLabelWidth / 2.0, -textL.Height / 2.0);
			drawingContext.DrawText(textL, labelPos);
			labelPos.Offset(textL.Width, 0);
			drawingContext.DrawText(baseL, labelPos);
			labelPos.Offset(baseL.Width, -0.1 * baseL.Height);
			drawingContext.DrawText(powL, labelPos);
			//labelPos.Offset(powL.Width, 0.1 * baseL.Height);
			//drawingContext.DrawText(textL, labelPos);

			if (snapTo == Side.Right || snapTo == Side.Left)
				drawingContext.Pop();
		}



		FormattedText MakeText(double val) {
			string numericValueString = (val).ToString("f" + Math.Max(0, orderOfMagnitude - orderOfMagnitudeDiff));
			return new FormattedText(numericValueString, cachedCulture, FlowDirection.LeftToRight, labelType, fontSize, Brushes.Black);
		}
		void MakeLegendText(out FormattedText baseL, out FormattedText powL, out FormattedText textL, out double totalLabelWidth) {
			string baseStr = orderOfMagnitude == 0 ? "" : "×10";
			string powStr = orderOfMagnitude == 0 ? "" : orderOfMagnitude.ToString();
			string labelStr = legendLabel + (orderOfMagnitude == 0 ? "" : " — ");
			baseL = new FormattedText(baseStr,
cachedCulture, FlowDirection.LeftToRight, labelType, fontSize, Brushes.Black);

			powL = new FormattedText(
				powStr,
				cachedCulture, FlowDirection.LeftToRight, labelType, fontSize * 0.8, Brushes.Black);

			textL = new FormattedText(
				labelStr,
				cachedCulture, FlowDirection.LeftToRight, labelType, fontSize, Brushes.Black);

			totalLabelWidth = baseL.Width + powL.Width + textL.Width;

		}

		StreamGeometry DrawTicks(double pixelsWide, Action<double, double> rank0ValAtPixel) {
			double scale = pixelsWide / (endVal - startVal);
			StreamGeometry geom = new StreamGeometry();
			using (var context = geom.Open()) {
				FindAllTicks(Math.Min(startVal, endVal), Math.Max(startVal, endVal), pixelsWide / 100, out orderOfMagnitudeDiff, (val, rank) => {
					double pixelPos = (val - startVal) * scale;

					context.BeginFigure(new Point(pixelPos, 0), false, false);
					context.LineTo(new Point(pixelPos, (4 - rank) * 4), true, false);

					if (rank == 0) rank0ValAtPixel(val, pixelPos);
				});
			}
			return geom;
		}

		struct Tick { public double Value; public int Rank; }

		static List<Tick> FindAllTicks(double minVal, double maxVal, double preferredNum, ref bool attemptBorderTicks, out int orderOfMagnitudeSlot) {
			double firstTickAt, totalSlotSize;
			int[] subDivTicks;
			int slotCount;
			CalcTickPositions(minVal, maxVal, preferredNum, ref attemptBorderTicks, out firstTickAt, out totalSlotSize, out orderOfMagnitudeSlot, out slotCount, out subDivTicks);

			//convert subDivTicks into "cumulative" multiples, i.e. 2,2,5 into 20,10,5
			int[] subMultiple = new int[subDivTicks.Length + 1];
			subMultiple[subDivTicks.Length] = 1;
			for (int i = subDivTicks.Length - 1; i >= 0; i--)
				subMultiple[i] = subDivTicks[i] * subMultiple[i + 1];

			double subSlotSize = totalSlotSize / subDivTicks[0];
			int subSlotCount = slotCount * subDivTicks[0];

			List<Tick> allTicks = new List<Tick>();
			for (int i = 0; i <= subSlotCount; i++) {
				double value = firstTickAt + subSlotSize * i;
				if (!attemptBorderTicks && (value < minVal || value > maxVal)) continue;

				int rank = 0;
				while (i % subMultiple[rank] != 0) rank++;
				allTicks.Add(new Tick { Rank = rank, Value = value });
			}
			return allTicks;
		}

		//static IEnumerable<int> SlotFactors {
		//    get {
		//        int slotFactor = 1;
		//        bool next2 = true;
		//        while (true) {
		//            yield return slotFactor;
		//            if (next2) slotFactor *= 2;
		//            else slotFactor *= 5;
		//        }
		//    }
		//}

		/// <summary>
		/// Calculates optimal parameters for the placements of the legend
		/// </summary>
		/// <param name="minVal">the start of the range to be ticked</param>
		/// <param name="maxVal">the end of the range to be ticked</param>
		/// <param name="preferredNum">the preferred number of labelled ticks.  This method will deviate by at most a factor 1.5 from that</param>
		/// <param name="attemptBorderTicks">Whether to try and extend the minVal-maxVal data range to include the next logical ticks.  Set to false by the method if inadvisable (when the preferredNum is too small, for instance).</param>
		/// <param name="firstTickAt">output: the position of the major tick before the data range.</param>
		/// <param name="slotSize">output: the distance between consecutive ticks</param>
		/// <param name="slotCount">The number of slotSize segments to tick.  i.e. the position of the major tick after the data range is at firstTickAt + slotCount*slotSize</param>
		/// <param name="ticks">output: the additional order of subdivisions each slot can be divided into.
		/// This value aims to have around 10 subdivisions total, slightly more when the actual number of slots is fewer than requested
		/// and slightly less when the actual number of slots greater than requested.</param>
		public static void CalcTickPositions(double minVal, double maxVal, double preferredNum, ref bool attemptBorderTicks, out double firstTickAt, out double slotSize, out int slotOrderOfMagnitude, out int slotCount, out int[] ticks) {
			if (preferredNum < 2) attemptBorderTicks = false;
			if (attemptBorderTicks) preferredNum--;

			double range = maxVal - minVal;
			double idealSlotSize = range / preferredNum;
			slotOrderOfMagnitude = (int)Math.Floor(Math.Log10(idealSlotSize)); //i.e  for 143 or 971 this is 2
			double baseSize = Math.Pow(10, slotOrderOfMagnitude);
			double relSlotSize = idealSlotSize / baseSize; //some number between 1 and 10
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
				fixedSlot = 1;
				slotOrderOfMagnitude++;
				baseSize *= 10;
				ticks = new[] { 2, 5, 2 };
			}

			slotSize = fixedSlot * baseSize;
			firstTickAt = Math.Floor(minVal / slotSize) * slotSize;
			slotCount = (int)(Math.Ceiling((maxVal - firstTickAt) / slotSize) + 0.5);
		}

	}
}
