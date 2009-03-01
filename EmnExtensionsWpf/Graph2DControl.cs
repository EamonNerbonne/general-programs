using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace EmnExtensions.Wpf
{
	public class Graph2DControl : GraphDrawingControl
	{
		public static M[,] Map2D<T, M>(T[,] input, Func<int, int, T, M> mapFunc) {
			M[,] retval = new M[input.GetLength(0), input.GetLength(1)];
			for (int i = 0; i < input.GetLength(0); i++)
				for (int j = 0; j < input.GetLength(1); j++)
					retval[i, j] = mapFunc(i, j, input[i, j]);
			return retval;
		}



		double rMin, rMax;

		public double[,] GraphData { get; set; }
		public int XLength { get { return GraphData.GetLength(1); } }
		public int YLength { get { return GraphData.GetLength(0); } }
		public Func<double, Color> Colormap { get; set; }
		public double MinVal { get; set; }
		public double MaxVal { get; set; }
		public double RealMax { get { return rMax; } }
		public double RealMin { get { return rMin; } }
		public double X0 { get; set; }
		public double XFin { get; set; }
		public double Y0 { get; set; }
		public double YFin { get; set; }
		public double OuterX0 { get; private set; }
		public double OuterXFin { get; private set; }
		public double OuterY0 { get; private set; }
		public double OuterYFin { get; private set; }
		public int ScaleFactor { get; set; }
		public string ColorLabel { get; set; }

		public double ComputeScaledValue(double v) { return v > rMax ? 1.0 : (v < rMin ? 0.0 : (v - rMin) / (rMax - rMin)); }

		public void RecomputeBitmap() {
			if (ColorLabel == null && Name != null)
				ColorLabel = Name;
			rMin = double.MaxValue; 
			rMax = double.MinValue;
			foreach (double val in GraphData) {
				if (val > rMax) rMax = val;
				if (val < rMin) rMin = val;
			}
			if (MinVal.IsFinite()) rMin = MinVal;
			if (MaxVal.IsFinite()) rMax = MaxVal;

			double hDelta = YFin - Y0;
			double wDelta = XFin - X0;
			double hDeltaPP = hDelta / (YLength - 1);
			double wDeltaPP = wDelta / (XLength - 1);
			OuterY0 = Y0 - 0.5 * hDeltaPP;
			OuterYFin = YFin + 0.5 * hDeltaPP;
			OuterX0 = X0- 0.5 * wDeltaPP;
			OuterXFin = XFin + 0.5 * wDeltaPP;


			GraphDrawing = GraphUtils.MakeBitmapDrawing(
				GraphUtils.MakeColormappedBitmap(Map2D(GraphData, (i, j, v) => ComputeScaledValue(v)), Colormap,ScaleFactor),
				OuterY0, OuterYFin, OuterX0, OuterXFin);
		}
		public Graph2DControl() {
			MinVal = double.NaN;
			MaxVal = double.NaN;
			Colormap = Colormaps.Greyscale;
			ScaleFactor = 1;
		}

	}
}
