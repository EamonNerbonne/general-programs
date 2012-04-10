using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.VizEngines;
using LvqLibCli;

namespace LvqGui {
	class LvqStatPlots {
		public readonly LvqDatasetCli dataset;
		public readonly LvqMultiModel model;
		public readonly IVizEngine<LvqMultiModel.ModelProjectionAndImage>[] prototypeClouds;
		public readonly IVizEngine<LvqMultiModel.ModelProjectionAndImage> classBoundaries, dataClouds;
		public readonly PlotControl scatterPlotControl;
		public readonly IVizEngine<LvqStatPlots>[] statPlots;
		public readonly PlotControl[] plots;
		public int selectedSubModel;
		public bool showTestEmbedding;

		public LvqStatPlots(LvqDatasetCli dataset, LvqMultiModel model) {
			this.dataset = dataset;
			this.model = model;
			if (model.IsProjectionModel) {
				prototypeClouds = MakePerClassScatterGraph(dataset, 0.3f, Math.Min(model.SubModels.First().PrototypeLabels.Length, 3), 1)
					.Select((graph, i) => graph.Map((LvqMultiModel.ModelProjectionAndImage proj) => proj.PrototypesByLabel[i])).ToArray();
				foreach (IPlotMetaDataWriteable metadata in prototypeClouds.Select(viz => viz.MetaData)) {
					metadata.OverrideBounds = Rect.Empty;
				}
				classBoundaries = MakeClassBoundaryGraph();
				dataClouds = MakePointCloudGraph(dataset).Map((LvqMultiModel.ModelProjectionAndImage proj) => proj.RawPoints);
				scatterPlotControl = MakeScatterPlotControl(prototypeClouds.Select(viz => viz).Concat(new[] { classBoundaries, dataClouds }));
			}

			plots = MakeDataPlots(dataset, model);//required
			statPlots = ExtractDataSinksFromPlots(plots);
		}

		public LvqMultiModel.ModelProjectionAndImage CurrentProjection() {
			var widthHeight = LastWidthHeight;
			return model.CurrentProjectionAndImage(dataset, widthHeight == null ? 0 : widthHeight.Item1, widthHeight == null ? 0 : widthHeight.Item2, classBoundaries != null && classBoundaries.MetaData.Hidden, selectedSubModel, showTestEmbedding);
		}


		public void SetScatterBounds(Rect bounds) {
			((IPlotMetaDataWriteable)dataClouds.MetaData).OverrideBounds = bounds;
			//foreach (IPlotMetaDataWriteable metadata in dataClouds.Select(viz => viz.Plot.MetaData)) metadata.OverrideBounds = bounds;
		}

		static IVizEngine<LvqStatPlots>[] ExtractDataSinksFromPlots(IEnumerable<PlotControl> plots) {
			return plots.SelectMany(plot => plot.Graphs).Cast<IVizEngine<LvqStatPlots>>().ToArray();
		}

		static PlotControl[] MakeDataPlots(LvqDatasetCli dataset, LvqMultiModel model) {
			return (
					from statname in model.TrainingStatNames.Select(LvqStatName.Create)
					where statname.StatGroup != null
					group statname by statname.StatGroup into statGroup
					select new PlotControl {
						ShowGridLines = true,
						//Title = statGroup.Key,
						Tag = statGroup.Key,
						GraphsEnumerable = LvqStatPlotFactory.Create(statGroup.Key, statGroup, model.IsMultiModel, dataset.IsFolded() || dataset.HasTestSet(), dataset.ClassColors, model.OriginalSettings.PrototypesPerClass).ToArray(),
						PlotName = statGroup.Key,
						Visibility = statGroup.First().HideByDefault ? Visibility.Collapsed : Visibility.Visible,
					}
				).ToArray();
		}

		static PlotControl MakeScatterPlotControl(IEnumerable<IVizEngine> graphs) {
			return new PlotControl {
				ShowAxes = false,
				AttemptBorderTicks = false,
				//ShowGridLines = true,
				UniformScaling = true,
				//Title = "ScatterPlot: " + model.ModelLabel,
				GraphsEnumerable = graphs,
				PlotName = "embed",
			};
		}

		static IVizEngine<Point[]>[] MakePerClassScatterGraph(LvqDatasetCli dataset, float colorIntensity, int? PointCount = null, int? zIndex = null) {
			return (
					from classColor in dataset.ClassColors//.Select((color,index)=>new{color,index})
					let darkColor = Color.FromScRgb(1.0f, classColor.ScR * colorIntensity, classColor.ScG * colorIntensity, classColor.ScB * colorIntensity)
					select Plot.CreatePixelScatter(
						new PlotMetaData {
							RenderColor = darkColor,
							ZIndex = zIndex ?? 0,
							OverrideMargin = new Thickness(0),
						}).Update(plot => {
							plot.CoverageRatio = 0.95; plot.OverridePointCountEstimate = PointCount ?? dataset.PointCount(0); plot.CoverageGradient = 5.0;
						})
				).ToArray();
		}

		static IVizEngine<LabelledPoint[]> MakePointCloudGraph(LvqDatasetCli dataset, int? zIndex = null) {
			return Plot.CreatePointCloud(new PlotMetaData { ZIndex = zIndex ?? 0 })
				.Update(plot => {
					plot.CoverageRatio = 0.95;
					plot.CoverageGradient = 5.0;
					plot.ClassColors = dataset.ClassColors;
				});
		}

		IVizEngine<LvqMultiModel.ModelProjectionAndImage> MakeClassBoundaryGraph() {
			return Plot.CreateBitmapDelegate<LvqMultiModel.ModelProjectionAndImage>(UpdateClassBoundaries, new PlotMetaData { ZIndex = -1, OverrideBounds = Rect.Empty });
		}

		Tuple<int, int> lastWidthHeight;
		Tuple<int, int> LastWidthHeight { get { return lastWidthHeight; } }

		void UpdateClassBoundaries(WriteableBitmap bmp, Matrix dataToBmp, int width, int height, LvqMultiModel.ModelProjectionAndImage lastProjection) {
			lastWidthHeight = Tuple.Create(width, height);
			bool hideBoundaries = classBoundaries.MetaData.Hidden;

			if (!hideBoundaries) {
				if (width != lastProjection.Width || height != lastProjection.Height || lastProjection.ImageData == null || selectedSubModel != lastProjection.forSubModel) {
					lastProjection = lastProjection.forModels.CurrentProjectionAndImage(lastProjection.forDataset, width, height, false, selectedSubModel, showTestEmbedding);
					SetScatterBounds(lastProjection.Bounds);
				}
				bmp.WritePixels(new Int32Rect(0, 0, width, height), lastProjection.ImageData, width * 4, 0);
			}
		}
	}
}
