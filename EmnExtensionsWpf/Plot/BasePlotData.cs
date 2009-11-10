using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Wpf.Plot
{

	public interface IBasePlotData
	{
		PlotMetaData MetaData { get; set; }
		public event Action<IBasePlotData, GraphChange> Changed;
		public object RawData { get; set; }
	}
	public interface IPlotData <out T>
	{
		PlotMetaData MetaData { get; set; }
		public event Action<IPlotData<T>, GraphChange> Changed;
		public T Data { get; }
		public void SetData(object typeUnsafeData);
	}


	public class BasePlotData
	{
		public event Action<BasePlotData, GraphChange> Changed;
		internal void OnChange(GraphChange changeType) { if (Changed != null) Changed(this, changeType); }

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

		abstract object RawData { get; set; }
	}
}
