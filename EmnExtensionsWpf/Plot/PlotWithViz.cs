using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Wpf.Plot {
	public class PlotWithViz<T> : IPlot {
		private IPlotContainer container;

		public IPlotContainer Container {
			get { return container; }
			set { container = value; }
		}

		private IPlotMetaData metaData;

		public IPlotMetaData MetaData {
			get { return metaData; }
			set { metaData = value; metaData.Container = this; }
		}

		private IVizEngine<T> visualisation;

		public IVizEngine<T> Visualisation {
			get { return visualisation; }
			set { visualisation = value; visualisation.Plot = this; }
		}

		IVizEngine IPlot.Visualisation { get { return Visualisation; } }
		public void GraphChanged(GraphChange changeType) {
			if (changeType == GraphChange.RenderOptions && Visualisation != null) Visualisation.OnRenderOptionsChanged();
			if (Container != null) Container.GraphChanged(this, changeType);
		}
	}

	public static class Plot {
		public static PlotWithViz<T> Create<T>(IPlotMetaData metadata, IVizEngine<T> viz) { return new PlotWithViz<T> { MetaData = metadata, Visualisation = viz }; }
	}
}
