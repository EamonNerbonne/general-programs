using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Wpf.Plot
{

	public interface IPlotData
	{
		PlotMetaData MetaData { get; set; }
		event Action<IPlotData, GraphChange> Changed;
		object RawData { get; set; }
	}

	internal interface IPlotDataInternal : IPlotData
	{
		void OnChange(GraphChange changeType);
	}

	public class PlotData<T> : IPlotDataInternal
	{
		public event Action<IPlotData, GraphChange> Changed;
		internal void OnChange(GraphChange changeType) { if (Changed != null) Changed(this, changeType); }
		void IPlotDataInternal.OnChange(GraphChange changeType) { OnChange(changeType); }

		PlotMetaData m_MetaData = PlotMetaData.Default;
		public PlotMetaData MetaData
		{
			get { return m_MetaData; }
			set
			{
				if (value.owner != null) throw new ArgumentException("Cannot share metadata between plots");
				value.owner = this;
				m_MetaData = value;
				OnChange(GraphChange.Projection);
				OnChange(GraphChange.Labels);
			}
		}

		public T Data { get; set; }
		object IPlotData.RawData { get { return Data; } set { Data = (T)value; } }
	}
}
