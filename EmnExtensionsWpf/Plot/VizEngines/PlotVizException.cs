using System;
using System.Runtime.Serialization;

namespace EmnExtensions.Wpf.VizEngines
{
    [Serializable]
    public class PlotVizException : Exception
    {
        public PlotVizException() { }
        public PlotVizException(string message) : base(message) { }
        public PlotVizException(string message, Exception inner) : base(message, inner) { }
        protected PlotVizException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
