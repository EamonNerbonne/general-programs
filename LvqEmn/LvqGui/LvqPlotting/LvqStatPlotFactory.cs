using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using EmnExtensions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.VizEngines;
using LvqLibCli;

namespace LvqGui {

	static class LvqStatPlotFactory {
		public static IEnumerable<IVizEngine<LvqStatPlots>> Create(string statisticGroup, IEnumerable<LvqStatName> stats, bool isMultiModel, bool hasTestSet, Color[]classColors,int protosPerClass) {
			var relevantStatistics = (hasTestSet ? stats : stats.Where(stat => !stat.TrainingStatLabel.StartsWith("Test"))).ToArray();

			return
				relevantStatistics.Zip(GetPlotColorsForStatGroup(statisticGroup, relevantStatistics.Length, classColors, protosPerClass),
					(stat, color) => MakePlotsForStatIdx(stat.TrainingStatLabel, stat.UnitLabel, color, stat.Index, isMultiModel)
				).SelectMany(s => s);
		}

		static readonly Color[] errorColors = new[] { Colors.Red, Color.FromRgb(0x8b, 0x8b, 0), };
		static readonly Color[] costColors = new[] { Colors.Blue, Colors.DarkCyan, };
		static IEnumerable<Color> GetPlotColorsForStatGroup(string windowTitle, int length, Color[] classColors, int protosPerClass) {
			return
				windowTitle == "Error Rates" ? errorColors :
				windowTitle == "Cost Function" ? costColors :
				windowTitle.StartsWith("Border Matrix: ") && length == classColors.Length * protosPerClass ? classColors.SelectMany(color=>Enumerable.Repeat(color,protosPerClass)) :
				WpfTools.MakeDistributedColors(length, new MersenneTwister(42));
		}

		static IEnumerable<IVizEngine<LvqStatPlots>> MakePlotsForStatIdx(string dataLabel, string yunitLabel, Color color, int statIdx, bool doVariants) {
			bool isTest=dataLabel.StartsWith("Test");
			if (doVariants) {
				yield return MakeStderrRangePlot(yunitLabel, color, statIdx, isTest);
				yield return MakeCurrentModelPlot(yunitLabel, color, statIdx, isTest);
			}
			yield return MakeMeanPlot(dataLabel, yunitLabel, color, statIdx, isTest);
		}

		static readonly object IsCurrPlotTag = new object();
		static readonly object IsTestPlotTag = new object();
		static readonly object IsCurrTestPlotTag = new object();

		public static bool IsTestPlot(IPlotMetaData plot) { return plot.Tag == IsTestPlotTag || plot.Tag == IsCurrTestPlotTag; }
		public static bool IsCurrPlot(IPlotMetaData plot) { return plot.Tag == IsCurrPlotTag || plot.Tag == IsCurrTestPlotTag; }
		public static bool IsTestPlot(IVizEngine plot) { return IsTestPlot(plot.MetaData);}
		public static bool IsCurrPlot(IVizEngine plot) { return IsCurrPlot(plot.MetaData); }


		public static readonly DashStyle CurrPlotDashStyle = new DashStyle(new[] { 0.0, 3.0 }, 0.0);
		static IVizEngine<LvqStatPlots> MakeCurrentModelPlot(string yunitLabel, Color color, int statIdx, bool isTest) {
			return MakePlotHelper(null, color, yunitLabel, isTest?IsCurrTestPlotTag: IsCurrPlotTag, SelectedModelToPointsMapper(statIdx), CurrPlotDashStyle);
		}

		static IVizEngine<LvqStatPlots> MakeMeanPlot(string dataLabel, string yunitLabel, Color color, int statIdx, bool isTest) {
			return MakePlotHelper(dataLabel, color, yunitLabel,isTest?IsTestPlotTag: null, ModelToPointsMapper(statIdx));
		}

		static IVizEngine<LvqStatPlots> MakePlotHelper(string dataLabel, Color color, string yunitLabel, object tag, Func<LvqStatPlots, Point[]> mapper, DashStyle dashStyle = null) {
			var lineplot= Plot.CreateLine(new PlotMetaData {
																					DataLabel = dataLabel == "" ? null : dataLabel,
			                                                                       	RenderColor = color,
			                                                                       	XUnitLabel = "Training iterations",
			                                                                       	YUnitLabel = yunitLabel,
			                                                                       	AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
			                                                                       	ZIndex = 1,
			                                                                       	Tag = tag,
			                                                                       });
			lineplot.CoverageRatioY = yunitLabel.StartsWith("max") ? 1.0 : 0.90;
			lineplot.CoverageRatioGrad = 20.0;
			lineplot.DashStyle = dashStyle ?? DashStyles.Solid;
			return lineplot.Map(mapper);
		}


		static IVizEngine<LvqStatPlots> MakeStderrRangePlot(string yunitLabel, Color color, int statIdx, bool isTest) {
			//Blend(color, Colors.White)
			color.ScA = 0.25f;
			var rangeplot = Plot.CreateDataRange(new PlotMetaData {
			                                                                                         	RenderColor = color,
			                                                                                         	XUnitLabel = "Training iterations",
			                                                                                         	YUnitLabel = yunitLabel,
			                                                                                         	AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
			                                                                                         	Tag = isTest?IsTestPlotTag:null,
			                                                                                         	ZIndex = 0
			                                                                                         });
			rangeplot.CoverageRatioY = yunitLabel.StartsWith("max") ? 1.0 : 0.90;
			rangeplot.CoverageRatioGrad = 20.0;
			return rangeplot.Map(ModelToRangeMapper(statIdx));
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
