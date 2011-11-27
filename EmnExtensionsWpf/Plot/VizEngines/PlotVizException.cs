using System;
using System.Linq;

namespace EmnExtensions.Wpf.VizEngines
{
	[Serializable]
	public class PlotVizException : Exception
	{
		public PlotVizException() { }
		public PlotVizException(string message) : base(message) { }
		public PlotVizException(string message, Exception inner) : base(message, inner) { }
		protected PlotVizException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
