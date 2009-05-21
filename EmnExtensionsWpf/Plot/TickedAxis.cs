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
	public enum SnapToSide { Top, Right, Bottom, Left }

	public class TickedAxis : FrameworkElement
	{

		Typeface typeface;
		double fontSize = 12.0 * 4.0 / 3.0;//12pt = 12 pixels at 72dpi = 16pixels at 96dpi
		Pen tickPen; //start flat end round

		public TickedAxis() {
			tickPen = new Pen {
				Brush = Brushes.Black,
				StartLineCap = PenLineCap.Flat,
				EndLineCap = PenLineCap.Round,
				Thickness = 1.5
			};
			typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, new FontFamily("Verdana"));
			TickLength = 16;
			LabelOffset = 1;
		}

		public DimensionBounds DataBound { get; set; }
		public DimensionMargins DataMargin { get; set; }
		public DimensionMargins ActualMargin { get; set; }
		public string DataUnits { get; set; }
		public bool AttemptBorderTicks { get; set; }
		double TickLength { get; set; }
		double LabelOffset { get; set; }
		double PixelsPerTick { get; set; }

		bool isAxisLabelValid = false;

		public SnapToSide SnapTo { get; set; }
		bool IsHorizontal { get { return SnapTo == SnapToSide.Bottom || SnapTo == SnapToSide.Top; } }

		CultureInfo cachedCulture;
		int m_dataOrderOfMagnitude, m_slotOrderOfMagnitude;//TODO:really, these should be calculated in measure.  The measure pass should have everything measured!
		Tick[] m_ticks;
		FormattedText[] rank0Labels;
		double[] rank0Values;

		DrawingGroup axisLegend;

		protected override Size MeasureOverride(Size constraint) {
			if (Visibility == Visibility.Collapsed || DataBound.Length<=0 || !DataBound.Length.IsFinite())
				return new Size(0, 0);
			
			cachedCulture = CultureInfo.CurrentCulture;

			m_dataOrderOfMagnitude = (int)Math.Floor(Math.Log10(Math.Max(Math.Abs(DataBound.Min), Math.Abs(DataBound.Max))));
			if (Math.Abs(m_dataOrderOfMagnitude) < 4) m_dataOrderOfMagnitude = 0;//don't use scientific notation for small powers of 10
		
			if(axisLegend==null)
				axisLegend = MakeAxisLegendText(m_dataOrderOfMagnitude,DataUnits,cachedCulture,fontSize,typeface);
			bool attemptBT = AttemptBorderTicks;
			if(m_ticks==null)
				m_ticks = FindAllTicks(DataBound, 1000/PixelsPerTick, ref attemptBT, out m_slotOrderOfMagnitude);
			
			FormattedText textMin = MakeText(DataBound.Min);
			FormattedText textMax = MakeText(DataBound.Max);
			double textWidth = Math.Max(textMin.Width, textMax.Width);
			double textHeight = Math.Min(textMin.Height, textMax.Height);

			if (IsHorizontal)
				return new Size(
					Math.Max(constraint.Width.IsFinite() ? constraint.Width : 0, axisLegend.Bounds.Width),
					Math.Max(constraint.Height.IsFinite() ? constraint.Height : 0, TickLength+LabelOffset + textHeight + axisLegend.Bounds.Height)
					);
			else
				return new Size(
					Math.Max(constraint.Width.IsFinite() ? constraint.Width : 0, TickLength + LabelOffset + textWidth + axisLegend.Bounds.Height),
					Math.Max(constraint.Height.IsFinite() ? constraint.Height : 0, axisLegend.Bounds.Width)
					);
		}

		protected override Size ArrangeOverride(Size finalSize) {
			double ticksPixelsDim = IsHorizontal ? finalSize.Width : finalSize.Height;
			double preferredNrOfTicks = ticksPixelsDim / PixelsPerTick;
			bool attemptBT = AttemptBorderTicks;
			Tick[] newTicks = FindAllTicks(DataBound, preferredNrOfTicks, ref  attemptBT, out m_slotOrderOfMagnitude);
			if (m_ticks == null || !m_ticks.SequenceEqual(newTicks)) {
				m_ticks = newTicks;
				rank0Values =( from tick in m_ticks
							   where tick.Rank==0
							   select tick.Value
							   ).ToArray();
				rank0Labels = ( from value in rank0Values
								select MakeText(value)
								).ToArray();
			}
			if(IsHorizontal)


			return base.ArrangeOverride(finalSize);
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
			m_slotOrderOfMagnitude = (int)Math.Floor(Math.Log10(Math.Abs(startVal - endVal))) - 2;
			m_dataOrderOfMagnitude = (int)Math.Floor(Math.Log10(Math.Max(Math.Abs(startVal), Math.Abs(endVal))));
			if (Math.Abs(m_dataOrderOfMagnitude) < 4) m_dataOrderOfMagnitude = 0;

			Matrix mirrTrans = Matrix.Identity; //top-left is 0,0, so if you're on the bottom you're happy
			if (SnapTo == SnapToSide.Right || SnapTo == SnapToSide.Bottom)
				mirrTrans.ScaleAt(1.0, -1.0, 0.0, (ActualHeight + ActualWidth - ticksPixelsDim) / 2.0);
			if (SnapTo == SnapToSide.Right || SnapTo == SnapToSide.Left) {
				mirrTrans.Rotate(-90.0);
				mirrTrans.Translate(0.0, ticksPixelsDim);
			}
			double textHMax = 0.0;
			//now mirrTrans projects from an "ideal" SnapTo-Top world-view to what we need.
			StreamGeometry geom = DrawTicks(ticksPixelsDim, (val, pixPos) => {
				FormattedText text = MakeText(val / Math.Pow(10, m_dataOrderOfMagnitude));
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
			MakeAxisLegendText(out baseL, out powL, out textL, out totalLabelWidth);
			double centerPix = ticksPixelsDim / 2;

			if (!IsHorizontal) textHMax += baseL.Height * 0.1;
			double centerAlt = 17 + textHMax + textL.Height / 2.0;

			Point labelPos = mirrTrans.Transform(new Point(centerPix, centerAlt));

			if (SnapTo == SnapToSide.Right)
				drawingContext.PushTransform(new RotateTransform(90.0, labelPos.X, labelPos.Y));
			else if (SnapTo == SnapToSide.Left)
				drawingContext.PushTransform(new RotateTransform(-90.0, labelPos.X, labelPos.Y));

			labelPos.Offset(-totalLabelWidth / 2.0, -textL.Height / 2.0);
			drawingContext.DrawText(textL, labelPos);
			labelPos.Offset(textL.Width, 0);
			drawingContext.DrawText(baseL, labelPos);
			labelPos.Offset(baseL.Width, -0.1 * baseL.Height);
			drawingContext.DrawText(powL, labelPos);
			//labelPos.Offset(powL.Width, 0.1 * baseL.Height);
			//drawingContext.DrawText(textL, labelPos);

			if (SnapTo == SnapToSide.Right || SnapTo == SnapToSide.Left)
				drawingContext.Pop();
		}



		FormattedText MakeText(double val) {
			string numericValueString = (val).ToString("f" + Math.Max(0, m_dataOrderOfMagnitude - m_slotOrderOfMagnitude));
			return new FormattedText(numericValueString, cachedCulture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
		}

		static DrawingGroup MakeAxisLegendText(int dataOrderOfMagnitude, string dataUnits, CultureInfo culture, double fontSize, Typeface typeface) {
			DrawingGroup retval = new DrawingGroup();
			using (DrawingContext context = retval.Open()) {
				string baseStr = dataOrderOfMagnitude == 0 ? "" : "×10";
				string powStr = dataOrderOfMagnitude == 0 ? "" : dataOrderOfMagnitude.ToString();
				string labelStr = dataUnits + (dataOrderOfMagnitude == 0 ? "" : " — ");
				var baseLabel = new FormattedText(baseStr, culture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				var powerLabel = new FormattedText(powStr, culture, FlowDirection.LeftToRight, typeface, fontSize * 0.8, Brushes.Black);
				var dataUnitsLabel = new FormattedText(labelStr, culture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);

				Point textOrigin = new Point(0, 0.1 * baseLabel.Height);
				context.DrawText(dataUnitsLabel, textOrigin);
				textOrigin.Offset(dataUnitsLabel.Width, 0);
				context.DrawText(baseLabel, textOrigin);
				textOrigin.Offset(baseLabel.Width, -0.1 * baseLabel.Height);
				context.DrawText(powerLabel, textOrigin);
			}
			return retval;
		}

		static StreamGeometry DrawTicksAlongX(IEnumerable<Tick> ticks, double tickLength) {
			StreamGeometry geom = new StreamGeometry();
			using (var context = geom.Open()) {
				foreach (Tick tick in ticks) {
					context.BeginFigure(new Point(tick.Value, 0), false, false);
					context.LineTo(new Point(tick.Value, (4 - tick.Rank) / 4.0 * tickLength), true, false);
				}
			}
			return geom;
		}

		struct Tick { public double Value; public int Rank; }

		static Tick[] FindAllTicks(DimensionBounds range, double preferredNum, ref bool attemptBorderTicks, out int slotOrderOfMagnitude) {
			double firstTickAt, totalSlotSize;
			int[] subDivTicks;
			int slotCount;
			CalcTickPositions(range, preferredNum, ref attemptBorderTicks, out firstTickAt, out totalSlotSize, out slotOrderOfMagnitude, out slotCount, out subDivTicks);

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
				if (!attemptBorderTicks && !range.EncompassesValue(value)) continue;

				int rank = 0;
				while (i % subMultiple[rank] != 0) rank++;
				allTicks.Add(new Tick { Rank = rank, Value = value });
			}
			return allTicks.ToArray();
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
		public static void CalcTickPositions(DimensionBounds range, double preferredNum, ref bool attemptBorderTicks, out double firstTickAt, out double slotSize, out int slotOrderOfMagnitude, out int slotCount, out int[] ticks) {
			if (preferredNum < 2) attemptBorderTicks = false;
			if (attemptBorderTicks) preferredNum--;

			double idealSlotSize = range.Length / preferredNum;
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
			firstTickAt = Math.Floor(range.Min / slotSize) * slotSize;
			slotCount = (int)(Math.Ceiling((range.Max
				- firstTickAt) / slotSize) + 0.5);
		}

	}
}
