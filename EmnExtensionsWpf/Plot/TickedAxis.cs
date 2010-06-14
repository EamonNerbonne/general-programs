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

namespace EmnExtensions.Wpf.Plot {
	[Flags]
	public enum TickedAxisLocation { None = 0, LeftOfGraph = 1, AboveGraph = 2, RightOfGraph = 4, BelowGraph = 8, Any = 15, Auto = 16, Default = LeftOfGraph | BelowGraph | Auto }

	public class TickedAxis : FrameworkElement {
		//TODO: reorganize code to make public/protected difference more obvious

		const double DefaultAxisLength = 1000.0;//assume we have this many pixels for estimates (i.e. measuring)
		const double MinimumNumberOfTicks = 1.0;//don't bother rendering if we have fewer than this many ticks.
		const int GridLineRanks = 3;
		const double BaseTickWidth = 1.5;

		Typeface m_typeface;
		double m_fontSize;
		Pen m_tickPen;
		Pen[] m_gridRankPen;

		public TickedAxis() {
			m_tickPen = new Pen {
				Brush = Brushes.Black,
				StartLineCap = PenLineCap.Flat,
				EndLineCap = PenLineCap.Round,
				Thickness = BaseTickWidth
			}; //start flat end round
			m_tickPen.Freeze();
			m_gridRankPen = Enumerable.Range(0, GridLineRanks)
				.Select(rank => (GridLineRanks - rank) / (double)(GridLineRanks))
				.Select(relevance => (Pen) new Pen {
					Brush = (Brush)new SolidColorBrush(Color.FromScRgb(1.0f,(float)(1.0-relevance),(float)(1.0-relevance),(float)(1.0-relevance))).GetAsFrozen(),
					Thickness = BaseTickWidth * relevance * Math.Sqrt(relevance),
					EndLineCap = PenLineCap.Flat,
					StartLineCap = PenLineCap.Flat,
					LineJoin = PenLineJoin.Bevel
				}.GetCurrentValueAsFrozen())
				.ToArray();

			DataBound = DimensionBounds.Empty;
			m_typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, new FontFamily("Verdana"));
			m_fontSize = 12.0 * 4.0 / 3.0;//12pt = 12 pixels at 72dpi = 16pixels at 96dpi
			TickLength = 16;
			LabelOffset = 1;
			PixelsPerTick = 150;
			AttemptBorderTicks = true;
		}

		protected override void OnInitialized(EventArgs e) {
			base.OnInitialized(e);
			GuessNeighborsBasedOnAxisPos();
		}

		DimensionBounds m_DataBound;
		DimensionMargins m_DataMargin;
		public DimensionBounds DataBound { get { return m_DataBound; } set { if (m_DataBound != value) { m_DataBound = value; InvalidateMeasure(); InvalidateVisual(); } } }
		public DimensionMargins DataMargin { get { return m_DataMargin; } set { if (m_DataMargin != value) { m_DataMargin = value; InvalidateMeasure(); InvalidateVisual(); } } } //TODO:should invalidate measure/render
		public Brush Background { get; set; } //TODO:affectsrender

		public TickedAxis ClockwisePrevAxis { get; private set; }//TODO:should invalidate measure/render
		public TickedAxis ClockwiseNextAxis { get; private set; }
		public TickedAxis OppositeAxis { get; private set; }

		double m_reqOfNext, m_reqOfPrev;

		double RequiredThicknessOfNext {
			get { return m_reqOfNext; }
			set {
				//value = Math.Ceiling(value);
				if (value != m_reqOfNext && ClockwiseNextAxis != null)
					ClockwiseNextAxis.InvalidateArrange();
				m_reqOfNext = value;
			}
		}
		double RequiredThicknessOfPrev {
			get { return m_reqOfPrev; }
			set {
				//	value = Math.Ceiling(value);
				if (value != m_reqOfPrev && ClockwisePrevAxis != null)
					ClockwisePrevAxis.InvalidateArrange();
				m_reqOfPrev = value;
			}
		}
		double Thickness { get { return Math.Max(0.0, IsHorizontal ? m_bestGuessCurrentSize.Height : m_bestGuessCurrentSize.Width); } }

		double ThicknessOfNext { get { return Math.Max(ClockwiseNextAxis == null ? 0.0 : ClockwiseNextAxis.Thickness, RequiredThicknessOfNext); } }
		double ThicknessOfPrev { get { return Math.Max(ClockwisePrevAxis == null ? 0.0 : ClockwisePrevAxis.Thickness, RequiredThicknessOfPrev); } }
		double RequiredThickness {
			get {
				return Math.Max(
				ClockwiseNextAxis == null ? 0.0 : ClockwiseNextAxis.RequiredThicknessOfPrev,
				ClockwisePrevAxis == null ? 0.0 : ClockwisePrevAxis.RequiredThicknessOfNext);
			}
		}
		public double EffectiveThickness { get { return Math.Max(Thickness, RequiredThickness); } }

		public string DataUnits { get; set; } //TODO:should invalidate measure/render
		bool m_AttemptBorderTicks;
		public bool AttemptBorderTicks { get { return m_AttemptBorderTicks || (MatchOppositeTicks && OppositeAxis != null && !OppositeAxis.IsCollapsedOrEmpty); } set { m_AttemptBorderTicks = value; InvalidateMeasure(); } } //TODO:should invalidate measure/render
		public double TickLength { get; set; } //TODO:should invalidate measure/render
		public double LabelOffset { get; set; } //TODO:should invalidate measure/render
		public double PixelsPerTick { get; set; } //TODO:should invalidate measure/render

		TickedAxisLocation m_axisPos = 0;
		public TickedAxisLocation AxisPos { //TODO: make DependancyProperty
			get { return m_axisPos; }
			set {
				if (value != TickedAxisLocation.AboveGraph && value != TickedAxisLocation.BelowGraph && value != TickedAxisLocation.LeftOfGraph && value != TickedAxisLocation.RightOfGraph)
					throw new ArgumentException("A Ticked Axis must be along precisely one side");
				m_axisPos = value;
				VerticalAlignment = m_axisPos == TickedAxisLocation.BelowGraph ? VerticalAlignment.Bottom : VerticalAlignment.Top;
				HorizontalAlignment = m_axisPos == TickedAxisLocation.RightOfGraph ? HorizontalAlignment.Right : HorizontalAlignment.Left;
				InvalidateMeasure(); InvalidateVisual();
			} //TODO:GuessNeighborsBasedOnAxisPos here and not in Initialized?
		}

		public bool IsHorizontal { get { return AxisPos == TickedAxisLocation.AboveGraph || AxisPos == TickedAxisLocation.BelowGraph; } }

		static TickedAxisLocation NextLoc(TickedAxisLocation current) {
			return (TickedAxisLocation)Math.Max((int)TickedAxisLocation.Any & (int)current * 2, 1);
		}
		static TickedAxisLocation PrevLoc(TickedAxisLocation current) {
			return (TickedAxisLocation)((int)current * 17 / 2 & (int)TickedAxisLocation.Any);
		}

		IEnumerable<TickedAxis> Siblings { get { return LogicalTreeHelper.GetChildren(Parent).OfType<TickedAxis>(); } }

		private void GuessNeighborsBasedOnAxisPos() {
			if ((AxisPos & TickedAxisLocation.Any) == 0 || Parent == null) //check for parent==null to permit usage in designer.
			{
				ClockwiseNextAxis = ClockwisePrevAxis = null;
			} else {
				TickedAxisLocation next = NextLoc(AxisPos);
				TickedAxisLocation prev = PrevLoc(AxisPos);
				TickedAxisLocation opp = NextLoc(NextLoc(AxisPos));
				foreach (TickedAxis siblingAxis in Siblings) {
					if (siblingAxis.AxisPos == next) {
						ClockwiseNextAxis = siblingAxis;
						siblingAxis.ClockwisePrevAxis = this;
					} else if (siblingAxis.AxisPos == prev) {
						ClockwisePrevAxis = siblingAxis;
						siblingAxis.ClockwiseNextAxis = this;
					} else if (siblingAxis.AxisPos == opp) {
						OppositeAxis = siblingAxis;
						siblingAxis.OppositeAxis = this;
					}
				}
			}
		}

		CultureInfo m_cachedCulture;
		int m_dataOrderOfMagnitude, m_slotOrderOfMagnitude;
		Tick[] m_ticks;
		int m_minReqTickCount, m_wantsTickCount;
		FormattedText[] m_rank1Labels;
		DrawingGroup m_axisLegend;
		bool m_redrawGridLines, m_matchOppositeTicks;
		Size m_bestGuessCurrentSize;

		public bool MatchOppositeTicks { get { return m_matchOppositeTicks; } set { m_matchOppositeTicks = value; InvalidateMeasure(); InvalidateVisual(); } }

		/// <summary>
		/// Attempts to guess the length of the ticked axis based on a previous render length or the DefaultAxisLength, for estimating tick labels for sizing.
		/// </summary>
		double AxisLengthGuess() {
			double axisAlignedLength = IsHorizontal ? m_bestGuessCurrentSize.Width : m_bestGuessCurrentSize.Height;
			if (!axisAlignedLength.IsFinite() || axisAlignedLength <= 0) axisAlignedLength = DefaultAxisLength;
			return axisAlignedLength - ThicknessOfNext - ThicknessOfPrev - DataMargin.Sum;
		}

		IEnumerable<double> Rank1Values {
			get {
				return m_ticks == null ? Enumerable.Empty<double>() :
					from tick in m_ticks
					where tick.Rank <= 1
					select tick.Value;
			}
		}

		/// <summary>
		/// Recomputes m_ticks: if available space is too small set to null, else when null recomputed based on m_bestGuessCurrentSize.
		/// </summary>
		void RecomputeTicks(bool mayIncrease) {
			double preferredNrOfTicks = AxisLengthGuess() / PreferredPixelsPerTick;
			if (preferredNrOfTicks < MinimumNumberOfTicks) {
				if (m_ticks != null)
					InvalidateVisual();
				m_ticks = null;
				m_rank1Labels = null;
			} else {
				int newSlotOrderOfMagnitude;
				int newWantsTickCount;
				var newTicks = FindAllTicks(DataBound, m_minReqTickCount, preferredNrOfTicks, AttemptBorderTicks, out newSlotOrderOfMagnitude, out newWantsTickCount);
				if (m_ticks == null && mayIncrease
					|| m_ticks != null && !m_ticks.SequenceEqual(newTicks) && (mayIncrease || m_ticks.Count(tick => tick.Rank <= 1) > newTicks.Count(tick => tick.Rank <= 1))) {

					m_slotOrderOfMagnitude = newSlotOrderOfMagnitude;
					m_rank1Labels = null;
					m_redrawGridLines = true;
					m_ticks = newTicks;
					if (MatchOppositeTicks&&OppositeAxis!=null&&!OppositeAxis.IsCollapsedOrEmpty
						&& m_wantsTickCount == OppositeAxis.m_minReqTickCount && m_wantsTickCount != newWantsTickCount
						&& m_wantsTickCount >= OppositeAxis.Rank1Values.Count()) {
						OppositeAxis.InvalidateMeasure();
					}
					m_wantsTickCount = newWantsTickCount;
					InvalidateVisual();
				}
			}
		}

		/// <summary>
		/// Recomputes m_dataOrderOfMagnitude.  If value has changed, sets m_axisLegend to null.
		/// </summary>
		void RecomputeDataOrderOfMagnitude() {
			double oldDOOM = m_dataOrderOfMagnitude;
			m_dataOrderOfMagnitude = (int)(0.5 + Math.Floor(Math.Log10(Math.Max(Math.Abs(DataBound.Start), Math.Abs(DataBound.End)))));
			if (Math.Abs(m_dataOrderOfMagnitude) < 4) m_dataOrderOfMagnitude = 0;//don't use scientific notation for small powers of 10

			if (m_dataOrderOfMagnitude != oldDOOM)
				m_axisLegend = null; //if order of magnitude has changed, we'll need to recreate the axis legend.
		}

		Size TickLabelSizeGuess {
			get {
				if (m_rank1Labels != null)
					return new Size(
						m_rank1Labels.Select(label => label.Width).DefaultIfEmpty(0.0).Max(),
						m_rank1Labels.Select(label => label.Height).DefaultIfEmpty(0.0).Max()
						);
				else {
					var canBeNegative = Rank1Values.Any(value => value < 0.0);
					var excessMagnitude = m_dataOrderOfMagnitude == 0 ? (int)(0.5 + Math.Floor(Math.Log10(Math.Max(Math.Abs(DataBound.Start), Math.Abs(DataBound.End))))) : 0;
					var textSample = MakeText(8.88888888888888888 * Math.Pow(10.0, excessMagnitude) * (canBeNegative ? -1 : 1));
					return new Size(textSample.Width, textSample.Height);
				}
			}
		}

		double PreferredPixelsPerTick {
			get {
				return m_ticks == null ? PixelsPerTick : Math.Max((PixelsPerTick + CondTranspose(TickLabelSizeGuess).Width) / 2.0, CondTranspose(TickLabelSizeGuess).Width * Math.Sqrt(10) / 2.0 + 4); //factor sqrt(10)/2 since actual tick distance may be off by that much from the preferred quantity.
			}
		}
		static Size Transpose(Size size) { return new Size(size.Height, size.Width); }

		Size CondTranspose(Size size) { return IsHorizontal ? size : Transpose(size); }//height==thickness, width == along span of axis



		public bool HideAxis {
			get { return (bool)GetValue(HideAxisProperty); }
			set { SetValue(HideAxisProperty, value); }
		}

		// Using a DependencyProperty as the backing store for HideAxis.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HideAxisProperty =
			DependencyProperty.Register("HideAxis", typeof(bool), typeof(TickedAxis), new UIPropertyMetadata(false, HideAxisChanged));
		static void HideAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { ((TickedAxis)d).InvalidateVisual(); }


		bool IsCollapsedOrEmpty { get { return HideAxis || Visibility == Visibility.Collapsed || DataBound.Length <= 0 || !DataBound.Length.IsFinite(); } }

		Size DontShow(Size constraint) {
			m_bestGuessCurrentSize = CondTranspose(new Size(CondTranspose(constraint).Width, 0));
			RequiredThicknessOfNext = RequiredThicknessOfPrev = 0.0;
			return m_bestGuessCurrentSize;
		}

		void RedrawNeighborsAsNeeded(double oldThickness) {
			double newThickness = Thickness;
			if (newThickness == oldThickness)
				return;//no change.
			//change...
			double maxThickness = Math.Max(newThickness, oldThickness);

			List<TickedAxis> toUpdate = new List<TickedAxis>();
			if (ClockwiseNextAxis != null && ClockwiseNextAxis.RequiredThicknessOfPrev < maxThickness)
				toUpdate.Add(ClockwiseNextAxis);
			if (ClockwisePrevAxis != null && ClockwisePrevAxis.RequiredThicknessOfNext < maxThickness)
				toUpdate.Add(ClockwisePrevAxis);

			foreach (var axis in toUpdate) {
				if (newThickness > oldThickness) //less space for them, need to remeasure.
					axis.InvalidateMeasure();
				axis.InvalidateVisual();
			}
		}

		protected override Size MeasureOverride(Size constraint) {
			double origThickness = Thickness;
			double origAxisLen = AxisLengthGuess();
			try {
				m_minReqTickCount = OppositeAxis==null|| OppositeAxis.IsCollapsedOrEmpty ? 0 : OppositeAxis.m_wantsTickCount;
				if (IsCollapsedOrEmpty)
					return DontShow(constraint);
#if TRACE
				Console.WriteLine("MeasureOverride: " + this.AxisPos);
#endif
				if (m_cachedCulture == null)
					m_cachedCulture = CultureInfo.CurrentCulture;

				bool isReasonableConstraint = constraint.Height >= 0 && constraint.Width >= 0 && constraint.Width.IsFinite() && constraint.Height.IsFinite();

				if (isReasonableConstraint)
					m_bestGuessCurrentSize = constraint;

				RecomputeDataOrderOfMagnitude();

				if (m_axisLegend == null)
					m_axisLegend = MakeAxisLegendText(m_dataOrderOfMagnitude, DataUnits, m_cachedCulture, m_fontSize, m_typeface);

				RecomputeTicks(true);

				if (m_ticks == null)
					return DontShow(constraint);

				//ticks are likely good now, so we compute labels...
				RecomputeTickLabels();			//we now have ticks and labels, yay!

				double constraintAxisAlignedWidth = CondTranspose(constraint).Width;
				m_bestGuessCurrentSize = CondTranspose(ComputeSize(constraintAxisAlignedWidth));

				if (isReasonableConstraint && (m_bestGuessCurrentSize.Width > constraint.Width || m_bestGuessCurrentSize.Height > constraint.Height))
					return DontShow(constraint);

				//At the end of Measure, m_bestGuessCurrentSize is either 0x0 if we don't think we can render, 
				//or it's a reasonable estimate of a final layout size, producing reasonable thickness.
				return m_bestGuessCurrentSize;
			} finally {
				RedrawNeighborsAsNeeded(origThickness);
			}
		}

		Size ComputeSize(double constraintAxisAlignedWidth) {
			Size tickLabelSize = CondTranspose(TickLabelSizeGuess);//height==thickness, width == along span of axis
			Rect minBounds = m_axisLegend.Bounds;
			minBounds.Union(new Point(0, 0));
			minBounds.Height += TickLength + LabelOffset + tickLabelSize.Height;

			//Size minimumSize = m_axisLegend.Bounds.Size;
			//minBounds.Union(new Point(constraintAxisAlignedWidth, 0));

			if (constraintAxisAlignedWidth.IsFinite())
				minBounds.Union(new Point(constraintAxisAlignedWidth, 0));

			RequiredThicknessOfNext = RequiredThicknessOfPrev = tickLabelSize.Width / 2.0;
			double minAxisLength = MinimumNumberOfTicks * PreferredPixelsPerTick;
			double overallWidth = minAxisLength + ThicknessOfNext + ThicknessOfPrev + DataMargin.Sum;

			minBounds.Union(new Point(overallWidth, RequiredThickness));
			minBounds.Height = Math.Ceiling(minBounds.Height);

			return minBounds.Size;
		}

		void RecomputeTickLabels() {
			if (m_rank1Labels == null)
				m_rank1Labels = (from value in Rank1Values
								 select MakeText(value)
								).ToArray();
		}

		protected override Size ArrangeOverride(Size constraint) {
			double origThickness = Thickness;
			try {
				m_bestGuessCurrentSize = constraint;
				if (IsCollapsedOrEmpty)
					return DontShow(constraint);
#if TRACE
				Console.WriteLine("Arrange: " + this.AxisPos);
#endif

				RecomputeTicks(false); //now with accurate info of actual size and an estimate of neighbors thicknesses.

				if (m_ticks == null)
					return DontShow(constraint);

				//ticks are likely good now, so we compute labels...
				RecomputeTickLabels();			//we now have ticks and labels, yay!

				double constraintAxisAlignedWidth = CondTranspose(constraint).Width;
				m_bestGuessCurrentSize = CondTranspose(ComputeSize(constraintAxisAlignedWidth));

				//At the end of arrange, the ticks+labels have been set, and m_bestGuessCurrentSize of this axis and it's neighbours 
				//is valid.  This means that we can caluculate the precise position of the axis as being within m_bestGuessCurrentSize, with margins on either side
				//corresponding to the neighbors' thicknesses (when shown) or the minimum required thickness (when not shown)
				//This layout will therefore be used for rendering (although if ArrangeOverride isn't happy, it will have triggered a remeasure.
				return m_bestGuessCurrentSize;
			} finally {
				RedrawNeighborsAsNeeded(origThickness);
				if (m_bestGuessCurrentSize.Width > constraint.Width || m_bestGuessCurrentSize.Height > constraint.Height)
					InvalidateMeasure();
			}
		}

		public DimensionBounds DisplayBounds { //depends on AxisPos, DataMargin, ThicknessOf*, m_bestGuessCurrentSize
			get {
				if (DataBound == DimensionBounds.Empty)
					return DimensionBounds.Empty;
				//if we're on bottom or right, data low values are towards the clockwise end.
				bool lowAtNext = AxisPos == TickedAxisLocation.BelowGraph || AxisPos == TickedAxisLocation.RightOfGraph; //flipped-vertical-relevant

				double displayStart = (lowAtNext ? ThicknessOfNext : ThicknessOfPrev) + DataMargin.AtStart;
				double displayEnd = (IsHorizontal ? m_bestGuessCurrentSize.Width : m_bestGuessCurrentSize.Height) -
					 ((lowAtNext ? ThicknessOfPrev : ThicknessOfNext) + DataMargin.AtEnd);

				if (!IsHorizontal) { //we need to "flip" the vertical ordering!
					displayStart = m_bestGuessCurrentSize.Height - displayStart;
					displayEnd = m_bestGuessCurrentSize.Height - displayEnd;
				}

				return new DimensionBounds { Start = displayStart, End = displayEnd };
			}
		}

		public DimensionBounds DisplayClippingBounds {
			get {
				if (DataBound == DimensionBounds.Empty)
					return DimensionBounds.Empty;
				//if we're on bottom or right, data low values are towards the clockwise end.
				bool lowAtNext = AxisPos == TickedAxisLocation.BelowGraph || AxisPos == TickedAxisLocation.RightOfGraph; //flipped-vertical-relevant

				double displayStart = (lowAtNext ? ThicknessOfNext : ThicknessOfPrev);
				double displayEnd = (IsHorizontal ? m_bestGuessCurrentSize.Width : m_bestGuessCurrentSize.Height) - (lowAtNext ? ThicknessOfPrev : ThicknessOfNext);

				if (!IsHorizontal) { //we need to "flip" the vertical ordering!
					displayStart = m_bestGuessCurrentSize.Height - displayStart;
					displayEnd = m_bestGuessCurrentSize.Height - displayEnd;
				}

				return new DimensionBounds { Start = displayStart, End = displayEnd };
			}
		}

		static Matrix MapBounds(DimensionBounds srcBounds, DimensionBounds dstBounds) {
			if (dstBounds.IsEmpty || srcBounds.IsEmpty)
				return Matrix.Identity;
			Matrix transform = Matrix.Identity;

			double scaleFactor = (dstBounds.End - dstBounds.Start) / (srcBounds.End - srcBounds.Start);

			transform.Scale(scaleFactor, 1.0);

			double offset = dstBounds.Start - srcBounds.Start * scaleFactor;
			transform.Translate(offset, 0.0);

			return transform;
		}

		static Matrix AlignmentTransform(TickedAxisLocation side, Size axisAlignedRenderSize) {
			Matrix transform = Matrix.Identity; //top-left is 0,0, so if you're on the bottom you're happy
			if (side == TickedAxisLocation.LeftOfGraph || side == TickedAxisLocation.AboveGraph)
				transform.ScaleAt(1.0, -1.0, 0.0, axisAlignedRenderSize.Height / 2.0);
			if (side == TickedAxisLocation.LeftOfGraph || side == TickedAxisLocation.RightOfGraph) {
				transform.Rotate(-90.0);
				transform.Scale(1.0, -1.0);
			}
			return transform;
		}

		static Matrix AxisLegendToCenter(TickedAxisLocation side, Rect axisLegendBounds, Point centerAt) {
			Matrix transform = Matrix.Identity;
			Vector center = 0.5 * ((Vector)axisLegendBounds.TopLeft + (Vector)axisLegendBounds.BottomRight);
			transform.Translate(-center.X, -center.Y);
			if (side == TickedAxisLocation.RightOfGraph || side == TickedAxisLocation.LeftOfGraph)
				transform.Rotate(90.0);
			transform.Translate(centerAt.X, centerAt.Y);
			return transform;
		}

		Matrix DataToDisplayAlongXTransform { get { return MapBounds(DisplayedDataBounds, DisplayBounds); } }

		public Matrix DataToDisplayTransform {
			get {
				Matrix dataToDispX = DataToDisplayAlongXTransform;
				if (!IsHorizontal && !dataToDispX.IsIdentity) {
					dataToDispX.M22 = dataToDispX.M11;
					dataToDispX.M11 = 1.0;
					dataToDispX.OffsetY = dataToDispX.OffsetX;
					dataToDispX.OffsetX = 0.0;
				}
				return dataToDispX;
			}
		}

		public DimensionBounds DisplayedDataBounds { get { return m_ticks == null ? DataBound : DataBound.UnionWith(m_ticks.First().Value, m_ticks.Last().Value); } }

		protected override void OnRender(DrawingContext drawingContext) {
			drawingContext.DrawRectangle(Background, null, new Rect(m_bestGuessCurrentSize));
			if (IsCollapsedOrEmpty || m_ticks == null)
				return;

#if TRACE
			Console.WriteLine("Rendering ticks: " + AxisPos);
#endif
			//We have a layout estimate.  We need to compute a transform from the data values to the axis position.
			//We'll render everything as if horizontal and top-aligned, then transform to where we need to be.
			//This means we need to make an "overall" bottom->where we really are transform, and two text transforms:
			//one for the data units (rotated as needed)
			//another for the tick labels (to keep them upright).

			//first, we compute transforms data->display as if BelowGraph->display with actual alignment
			Matrix dataToDispX = DataToDisplayAlongXTransform;
			Matrix alignToDisp = AlignmentTransform(AxisPos, CondTranspose(m_bestGuessCurrentSize));
			Matrix dataToDisp = Matrix.Multiply(dataToDispX, alignToDisp);

			//next, we draw a streamgeometry of all the ticks using data->disp transform.
			StreamGeometry tickGeometry = DrawTicksAlongX(m_ticks.Select((tick, idx) => new Tick { Value = dataToDispX.Transform(new Point(tick.Value, 0)).X, Rank = tick.Rank }), TickLength);// in disp-space due to accuracy.
			tickGeometry.Transform = new MatrixTransform(alignToDisp);
			tickGeometry.Freeze();
			drawingContext.DrawGeometry(null, m_tickPen, tickGeometry);

			var dispBounds = DisplayBounds;

			drawingContext.DrawLine(m_tickPen, alignToDisp.Transform(new Point(dispBounds.End, m_tickPen.Thickness / 4.0)), alignToDisp.Transform(new Point(dispBounds.Start, m_tickPen.Thickness / 4.0)));

			//then we draw all labels, computing the label center point accounting for horizontal/vertical alignment, and using data->disp to position that center point.
			foreach (var labelledValue in F.ZipWith(Rank1Values, m_rank1Labels, (val, label) => new { Value = val, Label = label })) {
				double labelAltitude = TickLength + LabelOffset + (IsHorizontal ? labelledValue.Label.Height : labelledValue.Label.Width) / 2.0;
				Point centerPoint = dataToDisp.Transform(new Point(labelledValue.Value, labelAltitude));
				Point originPoint = centerPoint - new Vector(labelledValue.Label.Width / 2.0, labelledValue.Label.Height / 2.0);

				drawingContext.DrawText(labelledValue.Label, originPoint);
			}

			//finally, we draw the axisLegend:
			double axisLegendAltitude = TickLength + LabelOffset + CondTranspose(TickLabelSizeGuess).Height + m_axisLegend.Bounds.Height / 2.0;
			Point axisLegendCenterPoint = alignToDisp.Transform(new Point(0.5 * (dispBounds.Start + dispBounds.End), axisLegendAltitude));
			drawingContext.PushTransform(new MatrixTransform(AxisLegendToCenter(AxisPos, m_axisLegend.Bounds, axisLegendCenterPoint)));
			drawingContext.DrawDrawing(m_axisLegend);
			drawingContext.Pop();
		}

		private void RenderGridLines() {
			//			Console.Write(".");
			var ticksByIdx = m_ticks.Select((tick, idx) => new Tick { Value = idx, Rank = tick.Rank });
			using (var context = m_gridLines.Open()) {
				foreach (var rank in Enumerable.Range(0, m_gridRankPen.Length)) {
					StreamGeometry rankGridLineGeom = DrawGridLines(ticksByIdx.Where(tick => tick.Rank == rank));
					rankGridLineGeom.Transform = m_gridLineAlignTransform;
					context.DrawGeometry(null, m_gridRankPen[rank], rankGridLineGeom);
				}
			}
			m_redrawGridLines = false;
		}

		private static StreamGeometry DrawGridLines(IEnumerable<Tick> ticks) {
			StreamGeometry geom = new StreamGeometry();
			using (var context = geom.Open())
				foreach (Tick tick in ticks)
					DrawGridLinesHelper(context, tick.Value, 1);
			return geom;
		}

		private static void DrawGridLinesHelper(StreamGeometryContext context, double value, double tickLength) {
			//we choose to generate the the streamgeometry "by index" of tick rather than value, due to accuracy: it seems the geometry is low-resolution, so
			//once stored, the resolution is no better than a float: using the "real" value is often inaccurate.
			context.BeginFigure(new Point(value, 0), false, false);
			context.LineTo(new Point(value, tickLength), true, false);
		}

		static StreamGeometry DrawTicksAlongX(IEnumerable<Tick> ticks, double tickLength) {
			StreamGeometry geom = new StreamGeometry();
			using (var context = geom.Open())
				foreach (Tick tick in ticks)
					DrawGridLinesHelper(context, tick.Value, (3.8 - Math.Max(tick.Rank - 1, 0)) / 3.8 * tickLength);
			return geom;
		}
		static Matrix TransposeMatrix { get { return new Matrix { M11 = 0, M12 = 1, M21 = 1, M22 = 0, OffsetX = 0, OffsetY = 0 }; } }

		MatrixTransform m_gridLineAlignTransform = new MatrixTransform();
		DrawingGroup m_gridLines = new DrawingGroup();

		public Drawing GridLines { get { return m_gridLines; } }

		public void SetGridLineExtent(Size outerBounds) {
			if (m_ticks == null) return; //TODO; why is this test actually necessary?
			if (m_redrawGridLines)
				RenderGridLines();

			bool oppFirst = AxisPos == TickedAxisLocation.BelowGraph || AxisPos == TickedAxisLocation.RightOfGraph;
			var oppAxis = ClockwiseNextAxis.ClockwiseNextAxis ?? ClockwisePrevAxis.ClockwisePrevAxis;
			double oppositeThickness = oppAxis != null ? oppAxis.EffectiveThickness : 0.0;

			double overallLength = IsHorizontal ? outerBounds.Height : outerBounds.Width;
			double startAt = oppFirst ? oppositeThickness : Thickness;
			double endAt = overallLength - (oppFirst ? Thickness : oppositeThickness);

			Matrix tickIdxToDisp = MapBounds(new DimensionBounds { Start = 0, End = m_ticks.Length - 1 },
					new DimensionBounds { Start = m_ticks[0].Value, End = m_ticks[m_ticks.Length - 1].Value })
					* DataToDisplayAlongXTransform;


			Matrix transform = Matrix.Identity;
			transform.Scale(1.0, endAt - startAt);
			transform.Translate(0.0, startAt);//grid lines long enough;
			transform.Append(tickIdxToDisp);
			if (!IsHorizontal)
				transform.Append(TransposeMatrix);

			m_gridLineAlignTransform.Matrix = transform;
		}

		FormattedText MakeText(double val) {
			string numericValueString = (val * Math.Pow(10.0, -m_dataOrderOfMagnitude)).ToString("f" + Math.Max(0, m_dataOrderOfMagnitude - m_slotOrderOfMagnitude));
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

		struct Tick { public double Value; public int Rank; }

		static Tick[] FindAllTicks(DimensionBounds range, int minReqTickCount, double preferredNum, bool attemptBorderTicks, out int slotOrderOfMagnitude, out int wantsTickCount) {
			double totalSlotSize;
			int[] subDivTicks;
			long firstTickMult, lastTickMult;
			CalcTickPositions(range, minReqTickCount, preferredNum, ref attemptBorderTicks, out totalSlotSize, out slotOrderOfMagnitude, out firstTickMult, out lastTickMult, out subDivTicks, out wantsTickCount);

			//convert subDivTicks into "cumulative" multiples, i.e. 2,2,5 into 20,10,5
			int[] subMultiple = new int[subDivTicks.Length + 1];
			subMultiple[subDivTicks.Length] = 1;
			for (int i = subDivTicks.Length - 1; i >= 0; i--)
				subMultiple[i] = subDivTicks[i] * subMultiple[i + 1];

			double subSlotSize = totalSlotSize / subMultiple[0];
			int subSlotCount = (int)(lastTickMult - firstTickMult) * subMultiple[0];

			List<Tick> allTicks = new List<Tick>();
			for (int i = 0; i <= subSlotCount; i++) {
				double value = (firstTickMult * subMultiple[0] + i) * subSlotSize; //by working in integral math here, we ensure that 0 falls on 0.0 exactly.
				if (!attemptBorderTicks && !range.EncompassesValue(value)) continue;

				int rank = 1;
				if (value == 0.0)
					rank = 0;
				else
					while (i % subMultiple[rank - 1] != 0) rank++;
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

		const double permittedErrorRatio = 0.0009; //i.e. upto 0.1 percent overshoot of the outer tick is permitted when attempting border ticks.
		//0.0009 value chosen to avoid unnecessary overshoot in cases approaching maximum resolution of a double.
		/// <summary>
		/// Calculates optimal parameters for the placements of the legend
		/// </summary>
		/// <param name="minVal">the start of the range to be ticked</param>
		/// <param name="maxVal">the end of the range to be ticked</param>
		/// <param name="preferredNum">the preferred number of labelled ticks.  This method will deviate by at most a factor 0.5*sqrt(10) from that</param>
		/// <param name="minReqTickCount">the minimal required tick count.  This method will always generate this many ticks, if necessary by padding the result.</param>
		/// <param name="attemptBorderTicks">Whether to try and extend the minVal-maxVal data range to include the next logical ticks.  Set to false by the method if inadvisable (when the preferredNum is too small, for instance).</param>
		/// <param name="firstTickAt">output: the position of the major tick before the data range.</param>
		/// <param name="slotSize">output: the distance between consecutive ticks</param>
		/// <param name="slotCount">The number of slotSize segments to tick.  i.e. the position of the major tick after the data range is at firstTickAt + slotCount*slotSize</param>
		/// <param name="ticks">output: the additional order of subdivisions each slot can be divided into.
		/// This value aims to have around 10 subdivisions total, slightly more when the actual number of slots is fewer than requested
		/// and slightly less when the actual number of slots greater than requested.</param>
		public static void CalcTickPositions(DimensionBounds range, int minReqTickCount, double preferredNum, ref bool attemptBorderTicks, out double slotSize, out int slotOrderOfMagnitude, out long firstTickAtSlotMultiple, out long lastTickAtSlotMultiple, out int[] ticks, out int wantsTickCount) {
			if (preferredNum > 10.0) preferredNum = Math.Sqrt(10 * preferredNum);

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
			firstTickAtSlotMultiple = (long)Math.Floor(range.Start / slotSize + permittedErrorRatio);
			lastTickAtSlotMultiple = (long)Math.Ceiling(range.End / slotSize - permittedErrorRatio);
			long tickCount = lastTickAtSlotMultiple - firstTickAtSlotMultiple + 1;
			wantsTickCount = (int)tickCount;
			long extraNeeded = Math.Max(0, minReqTickCount - tickCount);
			firstTickAtSlotMultiple -= extraNeeded / 2;
			lastTickAtSlotMultiple += (extraNeeded + 1) / 2;

			double effectiveStart = firstTickAtSlotMultiple * slotSize;
			double effectiveEnd = lastTickAtSlotMultiple * slotSize;
			if (minReqTickCount > 0) attemptBorderTicks = true;
			else if (effectiveEnd - effectiveStart > 1.35 * range.Length) attemptBorderTicks = false;
		}
	}
}