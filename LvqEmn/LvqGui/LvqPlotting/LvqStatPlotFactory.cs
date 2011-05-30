using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using EmnExtensions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.Wpf.Plot.VizEngines;
using LvqLibCli;

namespace LvqGui {

	static class LvqStatPlotFactory {
		public static IEnumerable<PlotWithViz<LvqStatPlots>> Create(string statisticGroup, IEnumerable<LvqStatName> stats, bool isMultiModel, bool hasTestSet) {
			var relevantStatistics = (hasTestSet ? stats : stats.Where(stat => !stat.TrainingStatLabel.StartsWith("Test"))).ToArray();

			return
				relevantStatistics.Zip(GetPlotColorsForStatGroup(statisticGroup, relevantStatistics.Length),
					(stat, color) => MakePlotsForStatIdx(stat.TrainingStatLabel, stat.UnitLabel, color, stat.Index, isMultiModel)
				).SelectMany(s => s);
		}

		static readonly Color[] errorColors = new[] { Colors.Red, Color.FromRgb(0x8b, 0x8b, 0), };
		static readonly Color[] costColors = new[] { Colors.Blue, Colors.DarkCyan, };
		static IEnumerable<Color> GetPlotColorsForStatGroup(string windowTitle, int length) {
			return
				windowTitle == "Error Rates" ? errorColors :
				windowTitle == "Cost Function" ? costColors :
				WpfTools.MakeDistributedColors(length, new MersenneTwister(42));
		}

		static IEnumerable<PlotWithViz<LvqStatPlots>> MakePlotsForStatIdx(string dataLabel, string yunitLabel, Color color, int statIdx, bool doVariants) {
			if (doVariants) {
				yield return MakeStderrRangePlot(yunitLabel, color, statIdx);
				yield return MakeCurrentModelPlot(yunitLabel, color, statIdx);
			}
			yield return MakeMeanPlot(dataLabel, yunitLabel, color, statIdx);
		}

		public static readonly object IsCurrPlotTag = new object();
		static PlotWithViz<LvqStatPlots> MakeCurrentModelPlot( string yunitLabel, Color color, int statIdx) {
			return MakePlotHelper(null, color, yunitLabel, IsCurrPlotTag, SelectedModelToPointsMapper(statIdx), DashStyles.Dot);
		}

		static PlotWithViz<LvqStatPlots> MakeMeanPlot(string dataLabel, string yunitLabel, Color color, int statIdx) {
			return MakePlotHelper(dataLabel, color, yunitLabel, null, ModelToPointsMapper(statIdx));
		}

		static PlotWithViz<LvqStatPlots> MakePlotHelper(string dataLabel, Color color, string yunitLabel, object tag, Func<LvqStatPlots, Point[]> mapper, DashStyle dashStyle = null) {
			return Plot.Create(
				new PlotMetaData {
					DataLabel = dataLabel,
					RenderColor = color,
					XUnitLabel = "Training iterations",
					YUnitLabel = yunitLabel,
					AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
					ZIndex = 1,
					Tag = tag,
				},
				new VizLineSegments {
					CoverageRatioY = yunitLabel.StartsWith("max") ? 1.0 : 0.90,
					CoverageRatioGrad = 20.0,
					DashStyle = dashStyle ?? DashStyles.Solid,
				}.Map(mapper));
		}


		static PlotWithViz<LvqStatPlots> MakeStderrRangePlot( string yunitLabel, Color color, int statIdx) {
			//Blend(color, Colors.White)
			color.ScA = 0.3f;
			return Plot.Create(
				new PlotMetaData {
					RenderColor = color,
					XUnitLabel = "Training iterations",
					YUnitLabel = yunitLabel,
					AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
					ZIndex = 0
				},
				new VizDataRange {
					CoverageRatioY = yunitLabel.StartsWith("max") ? 1.0 : 0.90,
					CoverageRatioGrad = 20.0,
				}.Map(ModelToRangeMapper(statIdx))
				);
		}

		static Func<LvqStatPlots, Point[]> ModelToPointsMapper(int statIdx) {
			return subplots => LimitGraphDetail(subplots.model.TrainingStats.Where(info => info.Value[statIdx].IsFinite()).Select(info => new Point(info.Value[LvqTrainingStatCli.TrainingIterationI], info.Value[statIdx])).ToArray());
		}
		static Func<LvqStatPlots, Point[]> SelectedModelToPointsMapper(int statIdx) {
			return subplots => LimitGraphDetail(subplots.model.SelectedStats(subplots.selectedSubModel).Where(stat => stat.values[statIdx].IsFinite()).Select(stat => new Point(stat.values[LvqTrainingStatCli.TrainingIterationI], stat.values[statIdx])).ToArray());
		}
		static Func<LvqStatPlots, Tuple<Point[], Point[]>> ModelToRangeMapper(int statIdx) {
			return subplots => {
				var okstats = subplots.model.TrainingStats.Where(info => (info.Value[statIdx] + info.StandardError[statIdx]).IsFinite());
				return
				Tuple.Create(
					LimitGraphDetail(
						okstats.Select(info =>
							new Point(info.Value[LvqTrainingStatCli.TrainingIterationI], info.Value[statIdx] + info.StandardError[statIdx])
						).ToArray()),
					LimitGraphDetail(
						okstats.Select(info =>
							new Point(info.Value[LvqTrainingStatCli.TrainingIterationI], info.Value[statIdx] - info.StandardError[statIdx])
						).ToArray()
					)
				);
			};
		}
		static Point[] LimitGraphDetail(Point[] retval) {
			int scaleFac = retval.Length / 1024; //not necessary anymore?
			if (scaleFac <= 1)
				return retval;
			Point[] newret = new Point[retval.Length / scaleFac];
			for (int i = 0; i < newret.Length; ++i) {
				for (int j = i * scaleFac; j < i * scaleFac + scaleFac; ++j) {
					newret[i] += new Vector(retval[j].X / scaleFac, retval[j].Y / scaleFac);
				}
			}
			return newret;
		}

	}
}
