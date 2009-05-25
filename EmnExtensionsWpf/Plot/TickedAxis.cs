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
		const double DefaultAxisLength = 1000.0;//assume we have this many pixels for estimates (i.e. measuring)
		const double MinimumNumberOfTicks = 0.5;//don't bother rendering if we have fewer than this many ticks.

		Typeface m_typeface;
		double m_fontSize = 12.0 * 4.0 / 3.0;//12pt = 12 pixels at 72dpi = 16pixels at 96dpi
		Pen m_tickPen; //start flat end round

		public TickedAxis() {
			m_tickPen = new Pen {
				Brush = Brushes.Black,
				StartLineCap = PenLineCap.Flat,
				EndLineCap = PenLineCap.Round,
				Thickness = 1.5
			};
			m_typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, new FontFamily("Verdana"));
			TickLength = 16;
			LabelOffset = 1;
			PixelsPerTick = 100;
		}

		public DimensionBounds DataBound { get; set; }
		public DimensionMargins DataMargin { get; set; }
		public Brush Background { get; set; }

		public TickedAxis ClockwisePrevAxis { get; set; }
		public TickedAxis ClockwiseNextAxis { get; set; }

		double RequiredThicknessOfNext { get; set; }
		double RequiredThicknessOfPrev { get; set; }
		double Thickness { get { return Math.Max(0.0, IsHorizontal ? m_bestGuessCurrentSize.Height : m_bestGuessCurrentSize.Width); } }

		double ThicknessOfNext() { return ClockwiseNextAxis == null || ClockwiseNextAxis.Thickness <= 0.0 ? RequiredThicknessOfNext : ClockwiseNextAxis.Thickness; }
		double ThicknessOfPrev() { return ClockwisePrevAxis == null || ClockwisePrevAxis.Thickness <= 0.0 ? RequiredThicknessOfPrev : ClockwisePrevAxis.Thickness; }
		double RequiredThickness() {
			return Math.Max(
			ClockwiseNextAxis == null ? 0.0 : ClockwiseNextAxis.RequiredThicknessOfPrev,
			ClockwisePrevAxis == null ? 0.0 : ClockwisePrevAxis.RequiredThicknessOfNext);
		}


		public string DataUnits { get; set; }
		public bool AttemptBorderTicks { get; set; }
		public double TickLength { get; set; }
		public double LabelOffset { get; set; }
		public double PixelsPerTick { get; set; }

		public SnapToSide SnapTo { get; set; }
		bool IsHorizontal { get { return SnapTo == SnapToSide.Bottom || SnapTo == SnapToSide.Top; } }

		CultureInfo m_cachedCulture;
		int m_dataOrderOfMagnitude, m_slotOrderOfMagnitude;
		Tick[] m_ticks;
		FormattedText[] m_rank0Labels;
		DrawingGroup m_axisLegend;

		Size m_bestGuessCurrentSize; //TODO refactor to store "local" size - i.e. in rotated form.

		/// <summary>
		/// Attempts to guess the length of the ticked axis based on a previous render length or the DefaultAxisLength, for estimating tick labels for sizing.
		/// </summary>
		double AxisLengthGuess() {
			double axisAlignedLength = IsHorizontal ? m_bestGuessCurrentSize.Width : m_bestGuessCurrentSize.Height;
			if (!axisAlignedLength.IsFinite() || axisAlignedLength <= 0) axisAlignedLength = DefaultAxisLength;
			return axisAlignedLength - ThicknessOfNext() - ThicknessOfPrev() - DataMargin.Sum;
		}

		IEnumerable<double> Rank0Values() {
			return m_ticks == null ? null :
				from tick in m_ticks
				where tick.Rank == 0
				select tick.Value;
		}

		/// <summary>
		/// Recomputes m_ticks: if available space is too small set to null, else when null recomputed based on m_bestGuessCurrentSize.
		/// </summary>
		void RecomputeTicks() {
			double preferredNrOfTicks = AxisLengthGuess() / PixelsPerTick;
			if (preferredNrOfTicks < MinimumNumberOfTicks) {
				m_ticks = null;
				m_rank0Labels = null;
			} else {
				var newTicks = FindAllTicks(DataBound, preferredNrOfTicks, AttemptBorderTicks, out m_slotOrderOfMagnitude);
				if (m_ticks == null || !m_ticks.SequenceEqual(newTicks))
					m_rank0Labels = null;
				m_ticks = newTicks;
			}
		}

		/// <summary>
		/// Recomputes m_dataOrderOfMagnitude.  If value has changed, sets m_axisLegend to null.
		/// </summary>
		void RecomputeDataOrderOfMagnitude() {
			double oldDOOM = m_dataOrderOfMagnitude;
			m_dataOrderOfMagnitude = (int)(0.5 + Math.Floor(Math.Log10(Math.Max(Math.Abs(DataBound.Min), Math.Abs(DataBound.Max)))));
			if (Math.Abs(m_dataOrderOfMagnitude) < 4) m_dataOrderOfMagnitude = 0;//don't use scientific notation for small powers of 10

			if (m_dataOrderOfMagnitude != oldDOOM)
				m_axisLegend = null; //if order of magnitude has changed, we'll need to recreate the axis legend.
		}

		Size TickLabelSizeGuess() {
			if (m_rank0Labels != null)
				return new Size(
					m_rank0Labels.Select(label => label.Width).DefaultIfEmpty(0.0).Max(),
					m_rank0Labels.Select(label => label.Height).DefaultIfEmpty(0.0).Max()
					);
			else {
				var canBeNegative = Rank0Values().Any(value => value < 0.0);
				var excessMagnitude = m_dataOrderOfMagnitude == 0 ? (int)(0.5 + Math.Floor(Math.Log10(Math.Max(Math.Abs(DataBound.Min), Math.Abs(DataBound.Max))))) : 0;
				var textSample = MakeText(8.88888888888888888 * Math.Pow(10.0,excessMagnitude)* (canBeNegative ? -1 : 1));
				return new Size(textSample.Width, textSample.Height);
			}
		}

		static Size Transpose(Size size) { return new Size(size.Height, size.Width); }

		Size CondTranspose(Size size) { return IsHorizontal ? size : Transpose(size); }//height==thickness, width == along span of axis

		bool IsCollapsedOrEmpty() { return Visibility == Visibility.Collapsed || DataBound.Length <= 0 || !DataBound.Length.IsFinite(); }



		protected override Size MeasureOverride(Size constraint) {
			if (IsCollapsedOrEmpty()) {
				RequiredThicknessOfNext = RequiredThicknessOfPrev = 0.0;
				return (m_bestGuessCurrentSize = Size.Empty); //won't render - not visible or no data to display.
			}

			if (m_cachedCulture == null)
				m_cachedCulture = CultureInfo.CurrentCulture;

			RecomputeDataOrderOfMagnitude();

			if (m_axisLegend == null)
				m_axisLegend = MakeAxisLegendText(m_dataOrderOfMagnitude, DataUnits, m_cachedCulture, m_fontSize, m_typeface);

			if (m_bestGuessCurrentSize.IsEmpty)
				m_bestGuessCurrentSize = constraint;

			RecomputeTicks();

			if (m_ticks == null) {
				RequiredThicknessOfNext = RequiredThicknessOfPrev = 0.0;
				return (m_bestGuessCurrentSize = Size.Empty); //can't render; too little space...
			}

			double constraintAxisAlignedWidth = CondTranspose(constraint).Width;
			m_bestGuessCurrentSize = CondTranspose(ComputeSize(constraintAxisAlignedWidth));

			//At the end of Measure, m_bestGuessCurrentSize is either empty if we don't think we can render, or it's a reasonable estimate of a final layout size, producing reasonable thickness.
			return m_bestGuessCurrentSize;
		}

		Size ComputeSize(double constraintAxisAlignedWidth) {
			Size tickLabelSize = CondTranspose(TickLabelSizeGuess());//height==thickness, width == along span of axis
			Size minimumSize = m_axisLegend.Bounds.Size;

			if (constraintAxisAlignedWidth.IsFinite())
				minimumSize.Width = Math.Max(minimumSize.Width, constraintAxisAlignedWidth);

			if (AttemptBorderTicks) {
				minimumSize.Width = Math.Max(minimumSize.Width, tickLabelSize.Width * 2);
			} 
			RequiredThicknessOfNext = RequiredThicknessOfPrev = tickLabelSize.Width / 2.0;

			minimumSize.Height += TickLength + LabelOffset + tickLabelSize.Height;
			minimumSize.Height = Math.Max(minimumSize.Height, RequiredThickness());

			return minimumSize;
		}

		void RecomputeTickLabels() {
			if (m_rank0Labels == null)
				m_rank0Labels = (from value in Rank0Values()
								 select MakeText(value)
								).ToArray();
		}

		protected override Size ArrangeOverride(Size finalSize) {
			m_bestGuessCurrentSize = finalSize;
			if (IsCollapsedOrEmpty())
				return m_bestGuessCurrentSize;
			RecomputeTicks(); //now with accurate info of actual size and an estimate of neighbors thicknesses.

			if (m_ticks == null) return Size.Empty; //can't render; too little space...

			double constraintAxisAlignedWidth = CondTranspose(finalSize).Width;
			m_bestGuessCurrentSize = CondTranspose(ComputeSize(constraintAxisAlignedWidth));
			if (m_bestGuessCurrentSize.Width > finalSize.Width || m_bestGuessCurrentSize.Height > finalSize.Height
				|| (finalSize.Width - m_bestGuessCurrentSize.Width) + (finalSize.Height - m_bestGuessCurrentSize.Height) > 1.0
				) {
				//OK, we misestimated.  We either need more space than available, or too much less than available
				//this affects our neighbors, e.g. as follows:
				//less thickness for us-> longer axis for them -> potentially more ticks -> they become thicker...
				//so they'll need a new measurement pass.

				if (ClockwisePrevAxis != null) ClockwisePrevAxis.InvalidateMeasure();
				if (ClockwiseNextAxis != null) ClockwiseNextAxis.InvalidateMeasure();
			}

			//ticks are likely good now, so we compute labels...
			RecomputeTickLabels();			//we now have ticks and labels, yay!

			//At the end of arrange, the ticks+labels have been set, and m_bestGuessCurrentSize of this axis and it's neighbours 
			//is valid.  This means that we can caluculate the precise position of the axis as being within m_bestGuessCurrentSize, with margins on either side
			//corresponding to the neighbors' thicknesses (when shown) or the minimum required thickness (when not shown)
			//This layout will therefore be used for rendering (although if ArrangeOverride isn't happy, it will have triggered a remeasure.
			return m_bestGuessCurrentSize;
		}

		void ComputeDisplayRange(out double displayStart, out double displayEnd) {
			bool lowAtNext = SnapTo == SnapToSide.Top || SnapTo == SnapToSide.Left; //if we're on bottom or right, data low values are towards the clockwise end.
			displayStart = (lowAtNext ? ThicknessOfNext() : ThicknessOfPrev()) + DataMargin.AtStart;
			displayEnd = (IsHorizontal ? m_bestGuessCurrentSize.Width : m_bestGuessCurrentSize.Height) -
				 ((lowAtNext ? ThicknessOfPrev() : ThicknessOfNext()) + DataMargin.AtEnd);

			if (!IsHorizontal) { //we need to "flip" the vertical ordering!
				displayStart = m_bestGuessCurrentSize.Height - displayStart;
				displayEnd = m_bestGuessCurrentSize.Height - displayEnd;
			}
		}

		static Matrix DataToDisplay(double displayStart, double displayEnd, DimensionBounds dataBounds) {//TODO: take into account AttemptBorderTicks
			Matrix transform = Matrix.Identity;
			double dataStart = dataBounds.Min, dataEnd = dataBounds.Max;

			double scaleFactor = (displayEnd - displayStart) / (dataEnd - dataStart);

			transform.Scale(scaleFactor, 1.0);
			dataStart *= scaleFactor; dataEnd *= scaleFactor;

			double offset = displayStart - dataStart;
			transform.Translate(offset, 0.0);

			//dataStart += offset; dataEnd += offset;
			//now datastart~=displayStart && displayEnd ~=dataEnd

			return transform;
		}

		static Matrix AlignmentTransform(SnapToSide snapTo, Size axisAlignedRenderSize) {
			Matrix transform = Matrix.Identity; //top-left is 0,0, so if you're on the bottom you're happy
			if (snapTo == SnapToSide.Right || snapTo == SnapToSide.Bottom)
				transform.ScaleAt(1.0, -1.0, 0.0, axisAlignedRenderSize.Height / 2.0);
			if (snapTo == SnapToSide.Right || snapTo == SnapToSide.Left) {
				transform.Rotate(-90.0);
				transform.Scale(1.0, -1.0);
			}
			return transform;
		}

		static Matrix AxisLegendToCenter(SnapToSide snapTo,Rect axisLegendBounds, Point centerAt) {
			Matrix transform = Matrix.Identity;
			Vector center = 0.5*((Vector)axisLegendBounds.TopLeft + (Vector)axisLegendBounds.BottomRight);
			transform.Translate(-center.X, -center.Y);
			if (snapTo == SnapToSide.Left || snapTo == SnapToSide.Right)
				transform.Rotate(90.0);
			transform.Translate(centerAt.X, centerAt.Y);
			return transform;
		}

		protected override void OnRender(DrawingContext drawingContext) {

			//TODO:looks like vertical alignment overstates label height?
			//TODO: weird snapping error on the 1000.

			drawingContext.DrawRectangle(Background, null, new Rect(m_bestGuessCurrentSize));
			if (IsCollapsedOrEmpty())
				return;

			//We have a layout estimate.  We need to compute a transform from the data values to the axis position.
			//We'll render everything as if horizontal and top-aligned, then transform to where we need to be.
			//This means we need to make an "overall" bottom->where we really are transform, and two text transforms:
			//one for the data units (rotated as needed)
			//another for the tick labels (to keep them upright).

			//first, we compute transforms data->display as if snapToTop->display
			double dispStart,dispEnd;
			ComputeDisplayRange(out dispStart,out dispEnd);
			Matrix dataToDispX = DataToDisplay(dispStart, dispEnd, DataBound.UnionWith(m_ticks.First().Value,m_ticks.Last().Value) );
			Matrix dispXToDisp = AlignmentTransform(SnapTo, CondTranspose(m_bestGuessCurrentSize));
			Matrix dataToDisp = Matrix.Multiply(dataToDispX, dispXToDisp);

			//next, we draw a streamgeometry of all the ticks using data->disp transform.
			StreamGeometry tickGeometry = DrawTicksAlongX(m_ticks, TickLength);// in data-space.
			tickGeometry.Transform = new MatrixTransform(dataToDisp);
			drawingContext.DrawGeometry(null, m_tickPen, tickGeometry);

			//then we draw all labels, computing the label center point accounting for horizontal/vertical alignment, and using data->disp to position that center point.
			foreach (var labelledValue in F.ZipWith(Rank0Values(), m_rank0Labels, (val, label) => new { Value=val, Label=label })) {
				double labelAltitude = TickLength + LabelOffset + (IsHorizontal ? labelledValue.Label.Height : labelledValue.Label.Width) / 2.0;
				Point centerPoint = dataToDisp.Transform(new Point(labelledValue.Value, labelAltitude));
				Point originPoint = centerPoint - new Vector(labelledValue.Label.Width / 2.0, labelledValue.Label.Height / 2.0);

				drawingContext.DrawText(labelledValue.Label, originPoint);
			}

			//finally, we draw the axisLegend:
			double axisLegendAltitude = TickLength + LabelOffset + CondTranspose(TickLabelSizeGuess()).Height + m_axisLegend.Bounds.Height / 2.0;
			Point axisLegendCenterPoint = dispXToDisp.Transform(new Point(CondTranspose(m_bestGuessCurrentSize).Width / 2.0, axisLegendAltitude));
			drawingContext.PushTransform(new MatrixTransform(AxisLegendToCenter(SnapTo, m_axisLegend.Bounds, axisLegendCenterPoint)));
			drawingContext.DrawDrawing(m_axisLegend);
			drawingContext.Pop();

		}

		FormattedText MakeText(double val) {
			string numericValueString = (val).ToString("f" + Math.Max(0, m_dataOrderOfMagnitude - m_slotOrderOfMagnitude));
			return new FormattedText(numericValueString, m_cachedCulture, FlowDirection.LeftToRight, m_typeface, m_fontSize, Brushes.Black);
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

		static Tick[] FindAllTicks(DimensionBounds range, double preferredNum, bool attemptBorderTicks, out int slotOrderOfMagnitude) {
			double firstTickAt, totalSlotSize;
			int[] subDivTicks;
			int slotCount;
			CalcTickPositions(range, preferredNum, ref attemptBorderTicks, out firstTickAt, out totalSlotSize, out slotOrderOfMagnitude, out slotCount, out subDivTicks);

			//convert subDivTicks into "cumulative" multiples, i.e. 2,2,5 into 20,10,5
			int[] subMultiple = new int[subDivTicks.Length + 1];
			subMultiple[subDivTicks.Length] = 1;
			for (int i = subDivTicks.Length - 1; i >= 0; i--)
				subMultiple[i] = subDivTicks[i] * subMultiple[i + 1];

			double subSlotSize = totalSlotSize / subMultiple[0];
			int subSlotCount = slotCount * subMultiple[0];

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
