using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using EmnExtensions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using LvqLibCli;
using Vector = System.Windows.Vector;

namespace LvqGui
{
    static class LvqStatPlotFactory
    {
        static readonly ConcurrentDictionary<int, Color[]> colorLookup = new ConcurrentDictionary<int, Color[]>();

        static Color[] GetColors(int length) => colorLookup.GetOrAdd(length, len =>
            WpfTools.MakeDistributedColors(len, new MersenneTwister(42))
        );


        public static IEnumerable<IVizEngine<LvqStatPlots>> Create(string statisticGroup, IEnumerable<LvqStatName> stats, bool isMultiModel, bool hasTestSet, Color[] classColors, int protosPerClass)
        {
            var relevantStatistics = (hasTestSet ? stats : stats.Where(stat => !stat.TrainingStatLabel.StartsWith("Test", StringComparison.Ordinal))).ToArray();
            var colors = statisticGroup.StartsWith("Per-prototype:", StringComparison.Ordinal) && relevantStatistics.Length == classColors.Length * protosPerClass
                ? classColors.SelectMany(color => Enumerable.Repeat(color, protosPerClass)).ToArray()
                : GetColors(relevantStatistics.Length);
            var isSpecialGroup = statisticGroup == "Error Rates" || statisticGroup == "Cost Function";
            if (isSpecialGroup) {
                Array.Reverse(relevantStatistics);
            }

            var i = 0;
            foreach (var stat in relevantStatistics) {
                var color =
                    isSpecialGroup && stat.TrainingStatLabel.StartsWith("Training", StringComparison.Ordinal) ? Color.FromRgb(190, 140, 0)
                    : isSpecialGroup && stat.TrainingStatLabel.StartsWith("Test", StringComparison.Ordinal) ? Color.FromRgb(190, 0, 255)
                    : isSpecialGroup && stat.TrainingStatLabel.StartsWith("NN", StringComparison.Ordinal) ? Color.FromRgb(0, 140, 255)
                    : colors[i];
                i++;
                foreach (var plot in MakePlotsForStatIdx(stat.TrainingStatLabel, stat.UnitLabel, color, stat.Index, isMultiModel)) {
                    yield return plot;
                }
            }
        }


        static IEnumerable<IVizEngine<LvqStatPlots>> MakePlotsForStatIdx(string dataLabel, string yunitLabel, Color color, int statIdx, bool doVariants)
        {
            var isTest = dataLabel.StartsWith("Test", StringComparison.Ordinal);
            var isNn = dataLabel.StartsWith("NN Error", StringComparison.Ordinal);
            if (doVariants) {
                yield return MakeStderrRangePlot(yunitLabel, color, statIdx, isTest, isNn);
                yield return MakeCurrentModelPlot(yunitLabel, color, statIdx, isTest, isNn);
            }

            yield return MakeMeanPlot(dataLabel, yunitLabel, color, statIdx, isTest, isNn);
        }

        static readonly object IsCurrPlotTag = new object();
        static readonly object IsTestPlotTag = new object();
        static readonly object IsNnPlotTag = new object();
        static readonly object IsCurrTestPlotTag = new object();
        static readonly object IsCurrNnPlotTag = new object();


        public static bool IsTestPlot(IPlotMetaData plot) => plot.Tag == IsTestPlotTag || plot.Tag == IsCurrTestPlotTag;
        public static bool IsCurrPlot(IPlotMetaData plot) => plot.Tag == IsCurrPlotTag || plot.Tag == IsCurrTestPlotTag || plot.Tag == IsCurrNnPlotTag;
        public static bool IsNnPlot(IPlotMetaData plot) => plot.Tag == IsNnPlotTag || plot.Tag == IsCurrNnPlotTag;
        public static bool IsTestPlot(IVizEngine plot) => IsTestPlot(plot.MetaData);
        public static bool IsCurrPlot(IVizEngine plot) => IsCurrPlot(plot.MetaData);
        public static bool IsNnPlot(IVizEngine plot) => IsNnPlot(plot.MetaData);


        public static readonly DashStyle CurrPlotDashStyle = new DashStyle(new[] { 0.0, 3.0 }, 0.0);
        static IVizEngine<LvqStatPlots> MakeCurrentModelPlot(string yunitLabel, Color color, int statIdx, bool isTest, bool isNn) => MakePlotHelper(null, color, yunitLabel, isTest ? IsCurrTestPlotTag : isNn ? IsCurrNnPlotTag : IsCurrPlotTag, SelectedModelToPointsMapper(statIdx), CurrPlotDashStyle);

        static IVizEngine<LvqStatPlots> MakeMeanPlot(string dataLabel, string yunitLabel, Color color, int statIdx, bool isTest, bool isNn) => MakePlotHelper(dataLabel, color, yunitLabel, isTest ? IsTestPlotTag : isNn ? IsNnPlotTag : null, ModelToPointsMapper(statIdx));

        static IVizEngine<LvqStatPlots> MakePlotHelper(string dataLabel, Color color, string yunitLabel, object tag, Func<LvqStatPlots, Point[]> mapper, DashStyle dashStyle = null)
        {
            var lineplot = PlotHelpers.CreateLine(new PlotMetaData {
                    DataLabel = string.IsNullOrEmpty(dataLabel) || dataLabel.StartsWith("#", StringComparison.Ordinal) ? null : dataLabel,
                    RenderColor = color,
                    XUnitLabel = "Training iterations",
                    YUnitLabel = yunitLabel,
                    AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
                    ZIndex = 1,
                    Tag = tag,
                }
            );
            lineplot.CoverageRatioY = yunitLabel.StartsWith("max", StringComparison.Ordinal) ? 1.0 : 0.90;
            lineplot.CoverageRatioGrad = 20.0;
            lineplot.DashStyle = dashStyle ?? DashStyles.Solid;
            return lineplot.Map(mapper);
        }


        static IVizEngine<LvqStatPlots> MakeStderrRangePlot(string yunitLabel, Color color, int statIdx, bool isTest, bool isNn)
        {
            //Blend(color, Colors.White)
            color.ScA = 0.25f;
            var rangeplot = PlotHelpers.CreateDataRange(new PlotMetaData {
                    RenderColor = color,
                    XUnitLabel = "Training iterations",
                    YUnitLabel = yunitLabel,
                    AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
                    Tag = isTest ? IsTestPlotTag : isNn ? IsNnPlotTag : null,
                    ZIndex = 0
                }
            );
            rangeplot.CoverageRatioY = yunitLabel.StartsWith("max", StringComparison.Ordinal) ? 1.0 : 0.90;
            rangeplot.CoverageRatioGrad = 20.0;
            return rangeplot.Map(ModelToRangeMapper(statIdx));
        }

        static Func<LvqStatPlots, Point[]> ModelToPointsMapper(int statIdx) => subplots => LimitGraphDetail(subplots.model.TrainingStats.Where(info => info.Value[statIdx].IsFinite()).Select(info => new Point(info.Value[LvqTrainingStatCli.TrainingIterationI], info.Value[statIdx])).ToArray());
        static Func<LvqStatPlots, Point[]> SelectedModelToPointsMapper(int statIdx) => subplots => LimitGraphDetail(subplots.model.SelectedStats(subplots.selectedSubModel).Where(stat => stat.values[statIdx].IsFinite()).Select(stat => new Point(stat.values[LvqTrainingStatCli.TrainingIterationI], stat.values[statIdx])).ToArray());

        static Func<LvqStatPlots, Tuple<Point[], Point[]>> ModelToRangeMapper(int statIdx) => subplots => {
            var okstats = subplots.model.TrainingStats.Where(info => (info.Value[statIdx] + info.StandardError[statIdx]).IsFinite());
            return
                Tuple.Create(
                    LimitGraphDetail(
                        okstats.Select(info =>
                            new Point(info.Value[LvqTrainingStatCli.TrainingIterationI], info.Value[statIdx] + info.StandardError[statIdx])
                        ).ToArray()
                    ),
                    LimitGraphDetail(
                        okstats.Select(info =>
                            new Point(info.Value[LvqTrainingStatCli.TrainingIterationI], info.Value[statIdx] - info.StandardError[statIdx])
                        ).ToArray()
                    )
                );
        };

        static Point[] LimitGraphDetail(Point[] retval)
        {
            var scaleFac = retval.Length / 1024; //not necessary anymore?
            if (scaleFac <= 1) {
                return retval;
            }

            var newret = new Point[retval.Length / scaleFac];
            for (var i = 0; i < newret.Length; ++i) {
                for (var j = i * scaleFac; j < i * scaleFac + scaleFac; ++j) {
                    newret[i] += new Vector(retval[j].X / scaleFac, retval[j].Y / scaleFac);
                }
            }

            return newret;
        }
    }
}
