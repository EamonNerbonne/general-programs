// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
    [Flags]
    public enum TickedAxisLocation { None = 0, LeftOfGraph = 1, AboveGraph = 2, RightOfGraph = 4, BelowGraph = 8, Any = 15, Auto = 16, Default = LeftOfGraph | BelowGraph | Auto }

    public sealed class TickedAxis : FrameworkElement
    {
        const double DefaultAxisLength = 1000.0; //assume we have this many pixels for estimates (i.e. measuring)
        const double MinimumNumberOfTicks = 1.0; //don't bother rendering if we have fewer than this many ticks.
        const int GridLineRanks = 3;
        const double BaseTickWidth = 1.25;
        const double BaseGridlineWidth = 0.9;
        const double FontSize = 12.0 * 4.0 / 3.0; //12pt = 12 pixels at 72dpi = 16pixels at 96dpi

        static readonly Typeface m_typeface;
        static readonly Pen m_tickPen;
        static readonly Pen[] m_gridRankPen;

        static TickedAxis()
        {
            m_gridRankPen = Enumerable.Range(0, GridLineRanks)
                .Select(rank => (GridLineRanks - rank) / (double)GridLineRanks)
                .Select(relevance => (Pen)new Pen {
                        Brush = (Brush)new SolidColorBrush(Color.FromScRgb(1.0f, (float)(1.0 - relevance), (float)(1.0 - relevance), (float)(1.0 - relevance))).GetAsFrozen(),
                        Thickness = BaseGridlineWidth * relevance * Math.Sqrt(relevance),
                        EndLineCap = PenLineCap.Flat,
                        StartLineCap = PenLineCap.Flat,
                        LineJoin = PenLineJoin.Bevel
                    }.GetCurrentValueAsFrozen()
                )
                .ToArray();
            m_tickPen = new Pen {
                Brush = Brushes.Black,
                StartLineCap = PenLineCap.Flat,
                EndLineCap = PenLineCap.Round,
                Thickness = BaseTickWidth
            }; //start flat end round
            m_tickPen.Freeze();
            m_typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, new FontFamily("Arial"));
        }

        public TickedAxis()
        {
            DataBound = DimensionBounds.Empty;
            TickLength = 16;
            LabelOffset = 1;
            PixelsPerTick = 150;
            AttemptBorderTicks = true;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            GuessNeighborsBasedOnAxisPos();
        }

        DimensionBounds m_DataBound;
        DimensionMargins m_DataMargin;

        public DimensionBounds DataBound
        {
            get => m_DataBound;
            set {
                if (m_DataBound != value) {
                    m_DataBound = value;
                    InvalidateMeasure();
                    InvalidateVisual();
                    InvalidateRender();
                }
            }
        }

        public DimensionMargins DataMargin
        {
            get => m_DataMargin;
            set {
                if (m_DataMargin != value) {
                    m_DataMargin = value;
                    InvalidateMeasure();
                    InvalidateVisual();
                    InvalidateRender();
                }
            }
        }

        Brush _background;

        public Brush Background
        {
            get => _background;
            set {
                _background = value;
                InvalidateVisual();
            }
        }

        TickedAxis ClockwisePrevAxis, ClockwiseNextAxis, OppositeAxis; //set in OnInitialized

        double m_reqOfNext, m_reqOfPrev;

        double RequiredThicknessOfNext
        {
            get => m_reqOfNext;
            set {
                //value = Math.Ceiling(value);
                if (value != m_reqOfNext && ClockwiseNextAxis != null) {
                    ClockwiseNextAxis.InvalidateArrange();
                }

                m_reqOfNext = value;
            }
        }

        double RequiredThicknessOfPrev
        {
            get => m_reqOfPrev;
            set {
                //    value = Math.Ceiling(value);
                if (value != m_reqOfPrev && ClockwisePrevAxis != null) {
                    ClockwisePrevAxis.InvalidateArrange();
                }

                m_reqOfPrev = value;
            }
        }

        double Thickness => Math.Max(0.0, IsHorizontal ? m_bestGuessCurrentSize.Height : m_bestGuessCurrentSize.Width);

        double ThicknessOfNext => Math.Max(ClockwiseNextAxis == null ? 0.0 : ClockwiseNextAxis.Thickness, RequiredThicknessOfNext);
        double ThicknessOfPrev => Math.Max(ClockwisePrevAxis == null ? 0.0 : ClockwisePrevAxis.Thickness, RequiredThicknessOfPrev);

        double RequiredThickness => Math.Max(
            ClockwiseNextAxis == null ? 0.0 : ClockwiseNextAxis.RequiredThicknessOfPrev,
            ClockwisePrevAxis == null ? 0.0 : ClockwisePrevAxis.RequiredThicknessOfNext
        );

        public double EffectiveThickness => Math.Max(Thickness, RequiredThickness);

        string _dataUnits;

        public string DataUnits
        {
            get => _dataUnits;
            set {
                _dataUnits = value;
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        bool m_AttemptBorderTicks, m_matchOppositeTicks, m_UniformScale;

        public bool AttemptBorderTicks
        {
            get => m_AttemptBorderTicks || MatchOppositeTicks && OppositeAxis != null && !OppositeAxis.IsCollapsedOrEmpty;
            set {
                m_AttemptBorderTicks = value;
                InvalidateMeasure();
            }
        }

        public bool MatchOppositeTicks
        {
            get => m_matchOppositeTicks;
            set {
                m_matchOppositeTicks = value;
                if (value) {
                    UniformScale = false;
                }

                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        public bool UniformScale
        {
            get => m_UniformScale && !AttemptBorderTicks;
            set {
                m_UniformScale = value;
                if (value) {
                    AttemptBorderTicks = false;
                    MatchOppositeTicks = false;
                }

                InvalidateMeasure();
                InvalidateVisual();
            }
        }


        double _tickLength, _labelOffset, _pixelsPerTick;

        public double TickLength
        {
            get => _tickLength;
            set {
                _tickLength = value;
                InvalidateVisual();
                InvalidateMeasure();
            }
        }

        public double LabelOffset
        {
            get => _labelOffset;
            set {
                _labelOffset = value;
                InvalidateVisual();
                InvalidateMeasure();
            }
        }

        public double PixelsPerTick
        {
            get => _pixelsPerTick;
            set {
                _pixelsPerTick = value;
                InvalidateMeasure();
            }
        }


        TickedAxisLocation m_axisPos = 0;

        public TickedAxisLocation AxisPos
        {
            get => m_axisPos;
            set {
                if (value != TickedAxisLocation.AboveGraph && value != TickedAxisLocation.BelowGraph && value != TickedAxisLocation.LeftOfGraph && value != TickedAxisLocation.RightOfGraph) {
                    throw new ArgumentException("A Ticked Axis must be along precisely one side");
                }

                m_axisPos = value;
                VerticalAlignment = m_axisPos == TickedAxisLocation.BelowGraph ? VerticalAlignment.Bottom : VerticalAlignment.Top;
                HorizontalAlignment = m_axisPos == TickedAxisLocation.RightOfGraph ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                InvalidateMeasure();
                InvalidateVisual(); //Possible: GuessNeighborsBasedOnAxisPos here and not in Initialized?
            }
        }

        public bool IsHorizontal => AxisPos == TickedAxisLocation.AboveGraph || AxisPos == TickedAxisLocation.BelowGraph;

        static TickedAxisLocation NextLoc(TickedAxisLocation current) => (TickedAxisLocation)Math.Max((int)TickedAxisLocation.Any & (int)current * 2, 1);
        static TickedAxisLocation PrevLoc(TickedAxisLocation current) => (TickedAxisLocation)((int)current * 17 / 2 & (int)TickedAxisLocation.Any);

        IEnumerable<TickedAxis> Siblings => LogicalTreeHelper.GetChildren(Parent).OfType<TickedAxis>();

        void GuessNeighborsBasedOnAxisPos()
        {
            if ((AxisPos & TickedAxisLocation.Any) == 0 || Parent == null) {
                ClockwiseNextAxis = ClockwisePrevAxis = null;
            } else {
                var next = NextLoc(AxisPos);
                var prev = PrevLoc(AxisPos);
                var opp = NextLoc(NextLoc(AxisPos));
                foreach (var siblingAxis in Siblings) {
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

            InvalidateMeasure();
            InvalidateVisual();
        }

        CultureInfo m_cachedCulture;
        int m_dataOrderOfMagnitude, m_slotOrderOfMagnitude;
        Tick[] m_ticks;
        int m_minReqTickCount, m_tickCount;
        Tuple<Tick, FormattedText>[] m_tickLabels;
        DrawingGroup m_axisLegend;
        bool m_redrawGridLines;
        Size m_bestGuessCurrentSize;


        /// <summary>
        /// Attempts to guess the length of the ticked axis based on a previous render length or the DefaultAxisLength, for estimating tick labels for sizing.
        /// </summary>
        double AxisLengthGuess()
        {
            var axisAlignedLength = IsHorizontal ? m_bestGuessCurrentSize.Width : m_bestGuessCurrentSize.Height;
            if (!axisAlignedLength.IsFinite() || axisAlignedLength <= 0) {
                axisAlignedLength = DefaultAxisLength;
            }

            return axisAlignedLength - ThicknessOfNext - ThicknessOfPrev - DataMargin.Sum;
        }

        IEnumerable<double> Rank1Values => m_ticks == null
            ? Enumerable.Empty<double>()
            : from tick in m_ticks
            where tick.Rank <= 1
            select tick.Value;

        IEnumerable<Tick> TicksNeedingLabels
        {
            get {
                if (m_ticks == null) {
                    yield break;
                }

                for (var i = 0; i < m_ticks.Length; i++) {
                    if (i == 0 || i == m_ticks.Length - 1 || m_ticks[i].Rank <= 1) {
                        yield return m_ticks[i];
                    }
                }
            }
        }

        static bool TickArrEqual(Tick[] a, Tick[] b)
        {
            if (a.Length != b.Length) {
                return false;
            }

            for (var i = 0; i < a.Length; i++) {
                if (a[i].Rank != b[i].Rank || a[i].Value != b[i].Value) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Recomputes m_ticks: if available space is too small set to null, else when null recomputed based on m_bestGuessCurrentSize.
        /// </summary>
        void RecomputeTicks(bool mayIncrease)
        {
            var preferredNrOfTicks = AxisLengthGuess() / PreferredPixelsPerTick;
            if (preferredNrOfTicks < MinimumNumberOfTicks) {
                if (m_ticks != null) {
                    InvalidateVisual();
                }

                m_ticks = null;
                m_tickLabels = null;
                m_redrawGridLines = true;
            } else {
                var newTicks = FindAllTicks(DataBound, m_minReqTickCount, preferredNrOfTicks, AttemptBorderTicks, out var newSlotOrderOfMagnitude, out var newTickCount);
                if (m_ticks == null && mayIncrease
                    || m_ticks != null && !TickArrEqual(m_ticks, newTicks) && (mayIncrease || m_ticks.Count(tick => tick.Rank <= 1) > newTicks.Count(tick => tick.Rank <= 1))) {
                    m_slotOrderOfMagnitude = newSlotOrderOfMagnitude;
                    m_tickLabels = null;
                    m_redrawGridLines = true;
                    m_ticks = newTicks;
                    if (MatchOppositeTicks && OppositeAxis != null && !OppositeAxis.IsCollapsedOrEmpty
                        && m_tickCount == OppositeAxis.m_minReqTickCount && m_tickCount != newTickCount
                        && m_tickCount >= OppositeAxis.Rank1Values.Count()) {
                        OppositeAxis.InvalidateMeasure();
                    }

                    m_tickCount = newTickCount;
                    InvalidateVisual();
                }
            }
        }

        void RecomputeTickLabels()
        {
            if (m_tickLabels == null) {
                m_tickLabels =
                (
                    from tick in TicksNeedingLabels
                    select Tuple.Create(tick, MakeText(tick.Value))
                ).ToArray();
                InvalidateRender();
            }
        }

        /// <summary>
        /// Recomputes m_dataOrderOfMagnitude.  If value has changed, sets m_axisLegend to null.
        /// </summary>
        void RecomputeDataOrderOfMagnitude()
        {
            double oldDOOM = m_dataOrderOfMagnitude;
            m_dataOrderOfMagnitude = ComputedDataOrderOfMagnitude();
            if (Math.Abs(m_dataOrderOfMagnitude) < 4) {
                m_dataOrderOfMagnitude = 0; //don't use scientific notation for small powers of 10
            }

            if (m_dataOrderOfMagnitude != oldDOOM) {
                m_axisLegend = null; //if order of magnitude has changed, we'll need to recreate the axis legend.
            }
        }

        int ComputedDataOrderOfMagnitude() => (int)(0.5 + Math.Floor(Math.Log10(Math.Max(Math.Abs(DataBound.Start), Math.Abs(DataBound.End)))));

        Size TickLabelSizeGuess
        {
            get {
                if (m_tickLabels != null) {
                    return new Size(
                        m_tickLabels.Select(label => label.Item2.Width).DefaultIfEmpty(0.0).Max(),
                        m_tickLabels.Select(label => label.Item2.Height).DefaultIfEmpty(0.0).Max()
                    );
                }

                var canBeNegative = TicksNeedingLabels.Any(tick => tick.Value < 0.0);
                var excessMagnitude = m_dataOrderOfMagnitude == 0 ? ComputedDataOrderOfMagnitude() : 0;
                var textSample = MakeText(8.88888888888888888 * Math.Pow(10.0, excessMagnitude) * (canBeNegative ? -1 : 1));
                return new Size(textSample.Width, textSample.Height);
            }
        }

        double PreferredPixelsPerTick => m_ticks == null ? PixelsPerTick : Math.Max((PixelsPerTick + CondTranspose(TickLabelSizeGuess).Width) / 2.0, CondTranspose(TickLabelSizeGuess).Width * Math.Sqrt(10) / 2.0 + 4); //factor sqrt(10)/2 since actual tick distance may be off by that much from the preferred quantity.
        static Size Transpose(Size size) => new Size(size.Height, size.Width);

        Size CondTranspose(Size size) => IsHorizontal ? size : Transpose(size); //height==thickness, width == along span of axis


        public bool HideAxis
        {
            get => (bool)GetValue(HideAxisProperty);
            set => SetValue(HideAxisProperty, value);
        }

        public static readonly DependencyProperty HideAxisProperty =
            DependencyProperty.Register("HideAxis", typeof(bool), typeof(TickedAxis), new UIPropertyMetadata(false, HideAxisChanged));

        static void HideAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TickedAxis)d).InvalidateVisual();


        bool IsCollapsedOrEmpty => HideAxis || Visibility == Visibility.Collapsed || DataBound.Length <= 0 || !DataBound.Length.IsFinite();

        static double ZeroIfInf(double val) => double.IsInfinity(val) ? 0 : val;

        Size DontShow(Size constraint)
        {
            m_bestGuessCurrentSize = CondTranspose(new Size(ZeroIfInf(CondTranspose(constraint).Width), 0));
            RequiredThicknessOfNext = RequiredThicknessOfPrev = 0.0;
            return m_bestGuessCurrentSize;
        }

        void RedrawNeighborsAsNeeded(double oldThickness)
        {
            var newThickness = Thickness;
            if (newThickness == oldThickness) {
                return; //no change.
            }

            //change...
            var maxThickness = Math.Max(newThickness, oldThickness);

            var toUpdate = new List<TickedAxis>();
            if (ClockwiseNextAxis != null && ClockwiseNextAxis.RequiredThicknessOfPrev < maxThickness) {
                toUpdate.Add(ClockwiseNextAxis);
            }

            if (ClockwisePrevAxis != null && ClockwisePrevAxis.RequiredThicknessOfNext < maxThickness) {
                toUpdate.Add(ClockwisePrevAxis);
            }

            foreach (var axis in toUpdate) {
                if (newThickness > oldThickness) //less space for them, need to remeasure.
                {
                    axis.InvalidateMeasure();
                }

                axis.InvalidateVisual();
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var origThickness = Thickness;
            try {
                m_minReqTickCount = OppositeAxis == null || OppositeAxis.IsCollapsedOrEmpty || !MatchOppositeTicks ? 0 : OppositeAxis.m_tickCount;
                if (IsCollapsedOrEmpty) {
                    return DontShow(constraint);
                }
#if TRACE
                Console.WriteLine("MeasureOverride: " + AxisPos);
#endif
                if (m_cachedCulture == null) {
                    m_cachedCulture = CultureInfo.CurrentCulture;
                }

                var isReasonableConstraint = constraint.Height >= 0 && constraint.Width >= 0 && constraint.Width.IsFinite() && constraint.Height.IsFinite();

                if (isReasonableConstraint) {
                    m_bestGuessCurrentSize = constraint;
                }

                RecomputeDataOrderOfMagnitude();

                if (m_axisLegend == null) {
                    m_axisLegend = MakeAxisLegendText(m_dataOrderOfMagnitude, DataUnits, m_cachedCulture, FontSize, m_typeface, pixelsPerDip);
                }

                RecomputeTicks(true);

                if (m_ticks == null) {
                    return DontShow(constraint);
                }

                //ticks are likely good now, so we compute labels...
                RecomputeTickLabels(); //we now have ticks and labels, yay!

                var constraintAxisAlignedWidth = CondTranspose(constraint).Width;
                m_bestGuessCurrentSize = CondTranspose(ComputeSize(constraintAxisAlignedWidth));

                if (isReasonableConstraint && (m_bestGuessCurrentSize.Width > constraint.Width || m_bestGuessCurrentSize.Height > constraint.Height)) {
                    return DontShow(constraint);
                }

                //At the end of Measure, m_bestGuessCurrentSize is either 0x0 if we don't think we can render,
                //or it's a reasonable estimate of a final layout size, producing reasonable thickness.
                return m_bestGuessCurrentSize;
            } finally {
                RedrawNeighborsAsNeeded(origThickness);
            }
        }

        Size ComputeSize(double constraintAxisAlignedWidth)
        {
            var tickLabelSize = CondTranspose(TickLabelSizeGuess); //height==thickness, width == along span of axis
            var minBounds = m_axisLegend.Bounds;
            minBounds.Intersect(new Rect(new Point(0.0, 0.0), new Size(double.PositiveInfinity, double.PositiveInfinity)));
            minBounds.Union(new Point(0, 0));
            minBounds.Height += TickLength + LabelOffset + tickLabelSize.Height;

            //Size minimumSize = m_axisLegend.Bounds.Size;
            //minBounds.Union(new Point(constraintAxisAlignedWidth, 0));

            if (constraintAxisAlignedWidth.IsFinite()) {
                minBounds.Union(new Point(constraintAxisAlignedWidth, 0));
            }

            RequiredThicknessOfNext = RequiredThicknessOfPrev = tickLabelSize.Width / 2.0;
            var minAxisLength = MinimumNumberOfTicks * PreferredPixelsPerTick;
            var overallWidth = minAxisLength + ThicknessOfNext + ThicknessOfPrev + DataMargin.Sum;

            minBounds.Union(new Point(overallWidth, RequiredThickness));
            minBounds.Height = Math.Ceiling(minBounds.Height);

            return minBounds.Size;
        }

        protected override Size ArrangeOverride(Size constraint)
        {
            var origThickness = Thickness;
            try {
                m_bestGuessCurrentSize = constraint;
                if (IsCollapsedOrEmpty) {
                    return DontShow(constraint);
                }
#if TRACE
                Console.WriteLine("Arrange: " + AxisPos);
#endif

                RecomputeTicks(false); //now with accurate info of actual size and an estimate of neighbors thicknesses.

                if (m_ticks == null) {
                    return DontShow(constraint);
                }

                //ticks are likely good now, so we compute labels...
                RecomputeTickLabels(); //we now have ticks and labels, yay!

                var constraintAxisAlignedWidth = CondTranspose(constraint).Width;
                m_bestGuessCurrentSize = CondTranspose(ComputeSize(constraintAxisAlignedWidth));

                //At the end of arrange, the ticks+labels have been set, and m_bestGuessCurrentSize of this axis and it's neighbours
                //is valid.  This means that we can caluculate the precise position of the axis as being within m_bestGuessCurrentSize, with margins on either side
                //corresponding to the neighbors' thicknesses (when shown) or the minimum required thickness (when not shown)
                //This layout will therefore be used for rendering (although if ArrangeOverride isn't happy, it will have triggered a remeasure.
                return m_bestGuessCurrentSize;
            } finally {
                RedrawNeighborsAsNeeded(origThickness);
                if (m_bestGuessCurrentSize.Width > constraint.Width || m_bestGuessCurrentSize.Height > constraint.Height) {
                    InvalidateMeasure();
                }
            }
        }

        public DimensionBounds DisplayBounds
        { //depends on AxisPos, DataMargin, ThicknessOf*, m_bestGuessCurrentSize
            get {
                if (DataBound == DimensionBounds.Empty) {
                    return DimensionBounds.Empty;
                }

                //if we're on bottom or right, data low values are towards the clockwise end.
                var lowAtNext = AxisPos == TickedAxisLocation.BelowGraph || AxisPos == TickedAxisLocation.RightOfGraph; //flipped-vertical-relevant

                var displayStart = (lowAtNext ? ThicknessOfNext : ThicknessOfPrev) + DataMargin.AtStart;
                var displayEnd = (IsHorizontal ? m_bestGuessCurrentSize.Width : m_bestGuessCurrentSize.Height) -
                    ((lowAtNext ? ThicknessOfPrev : ThicknessOfNext) + DataMargin.AtEnd);

                if (!IsHorizontal) { //we need to "flip" the vertical ordering!
                    displayStart = m_bestGuessCurrentSize.Height - displayStart;
                    displayEnd = m_bestGuessCurrentSize.Height - displayEnd;
                }

                return new DimensionBounds { Start = displayStart, End = displayEnd };
            }
        }

        public DimensionBounds DisplayClippingBounds
        {
            get {
                if (DataBound == DimensionBounds.Empty) {
                    return DimensionBounds.Empty;
                }

                //if we're on bottom or right, data low values are towards the clockwise end.
                var lowAtNext = AxisPos == TickedAxisLocation.BelowGraph || AxisPos == TickedAxisLocation.RightOfGraph; //flipped-vertical-relevant

                var displayStart = lowAtNext ? ThicknessOfNext : ThicknessOfPrev;
                var displayEnd = (IsHorizontal ? m_bestGuessCurrentSize.Width : m_bestGuessCurrentSize.Height) - (lowAtNext ? ThicknessOfPrev : ThicknessOfNext);

                if (!IsHorizontal) { //we need to "flip" the vertical ordering!
                    displayStart = m_bestGuessCurrentSize.Height - displayStart;
                    displayEnd = m_bestGuessCurrentSize.Height - displayEnd;
                }

                return new DimensionBounds { Start = displayStart, End = displayEnd };
            }
        }

        static Matrix MapBounds(DimensionBounds srcBounds, DimensionBounds dstBounds)
        {
            if (dstBounds.IsEmpty || srcBounds.IsEmpty) {
                return Matrix.Identity;
            }

            var transform = Matrix.Identity;

            var scaleFactor = (dstBounds.End - dstBounds.Start) / (srcBounds.End - srcBounds.Start);

            transform.Scale(scaleFactor, 1.0);

            var offset = dstBounds.Start - srcBounds.Start * scaleFactor;
            transform.Translate(offset, 0.0);

            return transform;
        }

        static Matrix AlignmentTransform(TickedAxisLocation side, Size axisAlignedRenderSize)
        {
            var transform = Matrix.Identity; //top-left is 0,0, so if you're on the bottom you're happy
            if (side == TickedAxisLocation.LeftOfGraph || side == TickedAxisLocation.AboveGraph) {
                transform.ScaleAt(1.0, -1.0, 0.0, axisAlignedRenderSize.Height / 2.0);
            }

            if (side == TickedAxisLocation.LeftOfGraph || side == TickedAxisLocation.RightOfGraph) {
                transform.Rotate(-90.0);
                transform.Scale(1.0, -1.0);
            }

            return transform;
        }

        static Matrix AxisLegendToCenter(TickedAxisLocation side, Rect axisLegendBounds, Point centerAt)
        {
            var transform = Matrix.Identity;
            var center = 0.5 * ((Vector)axisLegendBounds.TopLeft + (Vector)axisLegendBounds.BottomRight);
            transform.Translate(-center.X, -center.Y);
            if (side == TickedAxisLocation.RightOfGraph || side == TickedAxisLocation.LeftOfGraph) {
                transform.Rotate(90.0);
            }

            transform.Translate(centerAt.X, centerAt.Y);
            return transform;
        }

        Matrix DataToDisplayAlongXTransform => MapBounds(DisplayedDataBounds, DisplayBounds);

        public Matrix DataToDisplayTransform
        {
            get {
                var dataToDispX = DataToDisplayAlongXTransform;
                if (!IsHorizontal && !dataToDispX.IsIdentity) {
                    dataToDispX.M22 = dataToDispX.M11;
                    dataToDispX.M11 = 1.0;
                    dataToDispX.OffsetY = dataToDispX.OffsetX;
                    dataToDispX.OffsetX = 0.0;
                }

                return dataToDispX;
            }
        }

        DimensionBounds DisplayedDataBoundsRaw => m_ticks == null ? DataBound : DataBound.UnionWith(m_ticks.First().Value, m_ticks.Last().Value);
        double UnitsPerPixelRaw => DisplayedDataBoundsRaw.IsEmpty ? 0 : DisplayedDataBoundsRaw.Length / DisplayBounds.Length;

        public DimensionBounds DisplayedDataBounds
        {
            get {
                var databounds = DisplayedDataBoundsRaw;
                if (UniformScale && !databounds.IsEmpty) {
                    var density = Siblings.Select(axis => axis.UnitsPerPixelRaw).Max();
                    databounds.ScaleFromCenter(density / UnitsPerPixelRaw);
                }

                return databounds;
            }
        }


        public ulong RepaintWorkaround
        {
            get => (ulong)GetValue(RepaintWorkaroundProperty);
            set => SetValue(RepaintWorkaroundProperty, value);
        }

        // Using a DependencyProperty as the backing store for RepaintWorkaround.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RepaintWorkaroundProperty =
            DependencyProperty.Register("RepaintWorkaround", typeof(ulong), typeof(TickedAxis), new FrameworkPropertyMetadata(0UL, FrameworkPropertyMetadataOptions.NotDataBindable | FrameworkPropertyMetadataOptions.AffectsRender));

        void InvalidateRender() => RepaintWorkaround++;


        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Background, null, new Rect(m_bestGuessCurrentSize));
            if (IsCollapsedOrEmpty || m_ticks == null) {
                return;
            }

#if TRACE
            Console.WriteLine("Rendering ticks: " + AxisPos);
#endif
            //We have a layout estimate.  We need to compute a transform from the data values to the axis position.
            //We'll render everything as if horizontal and top-aligned, then transform to where we need to be.
            //This means we need to make an "overall" bottom->where we really are transform, and two text transforms:
            //one for the data units (rotated as needed)
            //another for the tick labels (to keep them upright).

            //first, we compute transforms data->display as if BelowGraph->display with actual alignment
            var dataToDispX = DataToDisplayAlongXTransform;
            var alignToDisp = AlignmentTransform(AxisPos, CondTranspose(m_bestGuessCurrentSize));
            var dataToDisp = Matrix.Multiply(dataToDispX, alignToDisp);

            //next, we draw a streamgeometry of all the ticks using data->disp transform.
            var tickGeometry = DrawTicksAlongX(m_ticks.Select((tick, idx) => new Tick { Value = dataToDispX.Transform(new Point(tick.Value, 0)).X, Rank = tick.Rank }), TickLength); // in disp-space due to accuracy.
            tickGeometry.Transform = new MatrixTransform(alignToDisp);
            tickGeometry.Freeze();
            drawingContext.DrawGeometry(null, m_tickPen, tickGeometry);

            var dispBounds = DisplayBounds;

            drawingContext.DrawLine(m_tickPen, alignToDisp.Transform(new Point(dispBounds.End, m_tickPen.Thickness / 4.0)), alignToDisp.Transform(new Point(dispBounds.Start, m_tickPen.Thickness / 4.0)));

            //then we draw all labels, computing the label center point accounting for horizontal/vertical alignment, and using data->disp to position that center point.
            var centerPoints = m_tickLabels.Select(labelledValue => {
                    var labelAltitude = TickLength + LabelOffset + (IsHorizontal ? labelledValue.Item2.Height : labelledValue.Item2.Width) / 2.0;
                    return dataToDisp.Transform(new Point(labelledValue.Item1.Value, labelAltitude));
                }
            ).ToArray();


            for (var i = 0; i < m_tickLabels.Length; i++) {
                var labelledValue = m_tickLabels[i];
                var centerPoint = centerPoints[i];
                var originPoint = centerPoint - new Vector(labelledValue.Item2.Width / 2.0, labelledValue.Item2.Height / 2.0);

                var tooClose =
                    labelledValue.Item1.Rank > 1 &&
                    new[] { i - 1, i + 1 }.Where(j => j >= 0 && j < m_tickLabels.Length).Any(j => {
                            var widthsum = IsHorizontal ? m_tickLabels[i].Item2.Width + m_tickLabels[j].Item2.Width : m_tickLabels[i].Item2.Height + m_tickLabels[j].Item2.Height;
                            var offset = IsHorizontal ? centerPoints[i].X - centerPoints[j].X : centerPoints[i].Y - centerPoints[j].Y;
                            return Math.Abs(offset) < 0.6 * widthsum;
                        }
                    );

                if (!tooClose) {
                    drawingContext.DrawText(labelledValue.Item2, originPoint);
                }
            }

            //finally, we draw the axisLegend:
            var axisLegendAltitude = TickLength + LabelOffset + CondTranspose(TickLabelSizeGuess).Height + m_axisLegend.Bounds.Height / 2.0;
            var axisLegendCenterPoint = alignToDisp.Transform(new Point(0.5 * (dispBounds.Start + dispBounds.End), axisLegendAltitude));
            drawingContext.PushTransform(new MatrixTransform(AxisLegendToCenter(AxisPos, m_axisLegend.Bounds, axisLegendCenterPoint)));
            drawingContext.DrawDrawing(m_axisLegend);
            drawingContext.Pop();
        }

        void RenderGridLines()
        {
            //            Console.Write(".");
            var ticksByIdx = m_ticks.Select((tick, idx) => new Tick { Value = idx, Rank = tick.Rank });
            using (var context = m_gridLines.Open()) {
                foreach (var rank in Enumerable.Range(0, m_gridRankPen.Length)) {
                    // ReSharper disable AccessToModifiedClosure
                    var rankGridLineGeom = DrawGridLines(ticksByIdx.Where(tick => tick.Rank == rank));
                    // ReSharper restore AccessToModifiedClosure
                    rankGridLineGeom.Transform = m_gridLineAlignTransform;
                    context.DrawGeometry(null, m_gridRankPen[rank], rankGridLineGeom);
                }
            }

            m_redrawGridLines = false;
        }

        static StreamGeometry DrawGridLines(IEnumerable<Tick> ticks)
        {
            var geom = new StreamGeometry();
            using (var context = geom.Open()) {
                foreach (var tick in ticks) {
                    DrawGridLinesHelper(context, tick.Value, 1);
                }
            }

            return geom;
        }

        static void DrawGridLinesHelper(StreamGeometryContext context, double value, double tickLength)
        {
            //we choose to generate the the streamgeometry "by index" of tick rather than value, due to accuracy: it seems the geometry is low-resolution, so
            //once stored, the resolution is no better than a float: using the "real" value is often inaccurate.
            context.BeginFigure(new Point(value, 0), false, false);
            context.LineTo(new Point(value, tickLength), true, false);
        }

        static StreamGeometry DrawTicksAlongX(IEnumerable<Tick> ticks, double tickLength)
        {
            var geom = new StreamGeometry();
            using (var context = geom.Open()) {
                foreach (var tick in ticks) {
                    DrawGridLinesHelper(context, tick.Value, (3.8 - Math.Max(tick.Rank - 1, 0)) / 3.8 * tickLength);
                }
            }

            return geom;
        }

        static Matrix TransposeMatrix => new Matrix { M11 = 0, M12 = 1, M21 = 1, M22 = 0, OffsetX = 0, OffsetY = 0 };

        readonly MatrixTransform m_gridLineAlignTransform = new MatrixTransform();
        readonly DrawingGroup m_gridLines = new DrawingGroup();
        double pixelsPerDip = 1.0;
        public Drawing GridLines => m_gridLines;

        public void SetGridLineExtent(Size outerBounds)
        {
            if (m_ticks == null) {
                return;
            }

            if (m_redrawGridLines) {
                RenderGridLines();
            }

            var oppFirst = AxisPos == TickedAxisLocation.BelowGraph || AxisPos == TickedAxisLocation.RightOfGraph;
            var oppAxis = ClockwiseNextAxis.ClockwiseNextAxis ?? ClockwisePrevAxis.ClockwisePrevAxis;
            var oppositeThickness = oppAxis != null ? oppAxis.EffectiveThickness : 0.0;

            var overallLength = IsHorizontal ? outerBounds.Height : outerBounds.Width;
            var startAt = oppFirst ? oppositeThickness : Thickness;
            var endAt = overallLength - (oppFirst ? Thickness : oppositeThickness);

            var tickIdxToDisp = MapBounds(new DimensionBounds { Start = 0, End = m_ticks.Length - 1 },
                    new DimensionBounds { Start = m_ticks[0].Value, End = m_ticks[^1].Value }
                )
                * DataToDisplayAlongXTransform;


            var transform = Matrix.Identity;
            transform.Scale(1.0, endAt - startAt);
            transform.Translate(0.0, startAt); //grid lines long enough;
            transform.Append(tickIdxToDisp);
            if (!IsHorizontal) {
                transform.Append(TransposeMatrix);
            }

            m_gridLineAlignTransform.Matrix = transform;
        }

        FormattedText MakeText(double val)
        {
            var numericValueString = (val * Math.Pow(10.0, -m_dataOrderOfMagnitude)).ToString("f" + Math.Max(0, m_dataOrderOfMagnitude - m_slotOrderOfMagnitude));
            return new FormattedText(numericValueString, m_cachedCulture, FlowDirection.LeftToRight, m_typeface, FontSize, Brushes.Black, pixelsPerDip);
        }

        static DrawingGroup MakeAxisLegendText(int dataOrderOfMagnitude, string dataUnits, CultureInfo culture, double fontSize, Typeface typeface, double pixelsPerDip)
        {
            var retval = new DrawingGroup();
            using (var context = retval.Open()) {
                var baseStr = dataOrderOfMagnitude == 0 ? "" : "×10";
                var powStr = dataOrderOfMagnitude == 0 ? "" : dataOrderOfMagnitude.ToString();
                var labelStr = dataUnits + (dataOrderOfMagnitude == 0 ? "" : " — ");
                var baseLabel = new FormattedText(baseStr, culture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black, pixelsPerDip);
                var powerLabel = new FormattedText(powStr, culture, FlowDirection.LeftToRight, typeface, fontSize * 0.8, Brushes.Black,pixelsPerDip);
                var dataUnitsLabel = new FormattedText(labelStr, culture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black,pixelsPerDip);

                var textOrigin = new Point(0, 0.1 * baseLabel.Height);
                context.DrawText(dataUnitsLabel, textOrigin);
                textOrigin.Offset(dataUnitsLabel.Width, 0);
                context.DrawText(baseLabel, textOrigin);
                textOrigin.Offset(baseLabel.Width, -0.1 * baseLabel.Height);
                context.DrawText(powerLabel, textOrigin);
            }

            return retval;
        }

        struct Tick
        {
            public double Value;
            public int Rank;
        }

        static Tick[] FindAllTicks(DimensionBounds range, int minReqTickCount, double preferredNum, bool attemptBorderTicks, out int slotOrderOfMagnitude, out int tickCount)
        {
            CalcTickPositions(range, minReqTickCount, preferredNum, out var totalSlotSize, out slotOrderOfMagnitude, out var firstTickMult, out var lastTickMult, out var subDivTicks, out tickCount, out var fixedSlot);
            subDivTicks = subDivTicks.Take(1).ToArray();
            //convert subDivTicks into "cumulative" multiples, i.e. 2,2,5 into 20,10,5,1
            var subMultiple = new int[subDivTicks.Length + 1];
            subMultiple[subDivTicks.Length] = 1;
            for (var i = subDivTicks.Length - 1; i >= 0; i--) {
                subMultiple[i] = subDivTicks[i] * subMultiple[i + 1];
            }

            var subSlotSize = totalSlotSize / subMultiple[0];
            var subSlotCount = (int)(lastTickMult - firstTickMult) * subMultiple[0];

            var allTicks = new List<Tick>();
            for (var i = 0; i <= subSlotCount; i++) {
                var value = (firstTickMult * subMultiple[0] + i) * subSlotSize; //by working in integral math here, we ensure that 0 falls on 0.0 exactly.
                var rank = 1;
                if (value == 0.0) {
                    rank = 0;
                } else {
                    while (i % subMultiple[rank - 1] != 0) {
                        rank++;
                    }
                }

                allTicks.Add(new Tick { Rank = rank, Value = value });
            }
            //we have all ticks, now trim ticks down to relevant ones

            var startSkip = 0;
            if (minReqTickCount == 0 && (!attemptBorderTicks || range.Min - allTicks[0].Value > 0.2 * range.Length)) {
                for (var i = 0; i < allTicks.Count && !range.EncompassesValue(allTicks[i].Value); i++) {
                    if (allTicks[i].Rank <= 2) {
                        startSkip = i; //found SubPrime before range!
                    }
                }
            }

            var upto = allTicks.Count;
            if (minReqTickCount == 0 && (!attemptBorderTicks || allTicks[^1].Value - range.Max > 0.2 * range.Length)) {
                for (var i = allTicks.Count - 1; i >= 0 && !range.EncompassesValue(allTicks[i].Value); i--) {
                    if (allTicks[i].Rank <= 2) {
                        upto = i + 1; //found SubPrime after range!
                    }
                }
            }

            if ((startSkip != 0 || upto != allTicks.Count) && fixedSlot == 1) { //subdividing into a new significant digit!
                //Console.WriteLine("Subdividing for range " + allTicks[startSkip].Value.ToString("g3") + " - " + allTicks[upto - 1].Value.ToString("g3"));
                slotOrderOfMagnitude--;
            }


            return allTicks.Skip(startSkip).Take(upto - startSkip).ToArray();
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
        /// <param name="range"> the range of values to be ticked</param>
        /// <param name="preferredNum">the preferred number of labelled ticks.  This method will deviate by at most a factor 0.5*sqrt(10) from that</param>
        /// <param name="minReqTickCount">the minimal required tick count.  This method will always generate this many ticks, if necessary by padding the result.</param>
        /// <param name="slotSize">output: the distance between consecutive ticks</param>
        /// <param name="firstTickAtSlotMultiple">output: the first tick is at this multiple of slotSize.</param>
        /// <param name="lastTickAtSlotMultiple">output: the last tick is at this multiple of slotSize.</param>
        /// <param name="ticks">
        /// output: the additional order of subdivisions each slot can be divided into.
        /// This value aims to have around 10 subdivisions total, slightly more when the actual number of slots is fewer than requested
        /// and slightly less when the actual number of slots greater than requested.
        /// </param>
        /// <param name="tickCount">output: the number of major ticks the range has been subdived over.</param>
        /// <param name="slotOrderOfMagnitude">output: The order of magnitude of the difference between consecutive major ticks, in base 10 - useful for deciding how many digits of a label to print.</param>
        /// <param name="fixedSlot">?</param>
        static void CalcTickPositions(DimensionBounds range, int minReqTickCount, double preferredNum, out double slotSize, out int slotOrderOfMagnitude, out long firstTickAtSlotMultiple, out long lastTickAtSlotMultiple, out int[] ticks, out int tickCount, out int fixedSlot)
        {
            if (preferredNum > 10.0) {
                preferredNum = Math.Sqrt(10 * preferredNum);
            }

            var idealSlotSize = range.Length / preferredNum;
            slotOrderOfMagnitude = (int)Math.Floor(Math.Log10(idealSlotSize)); //i.e  for 143 or 971 this is 2
            var baseSize = Math.Pow(10, slotOrderOfMagnitude);
            var relSlotSize = idealSlotSize / baseSize; //some number between 1 and 10

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
            tickCount = (int)(lastTickAtSlotMultiple - firstTickAtSlotMultiple + 1);
            long extraNeeded = Math.Max(0, minReqTickCount - tickCount);
            firstTickAtSlotMultiple -= extraNeeded / 2;
            lastTickAtSlotMultiple += (extraNeeded + 1) / 2;
        }
    }
}
