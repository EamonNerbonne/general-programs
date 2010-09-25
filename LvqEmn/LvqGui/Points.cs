using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace LvqGui {
	public static class Points {
		public static Point[] ToMediaPoints(double[,] arrayPoints) {
			int pointCount = arrayPoints.GetLength(0);
			Point[] retval = new Point[pointCount];
			for(int i=0;i<pointCount;++i) retval[i] = new Point(arrayPoints[i, 0], arrayPoints[i, 1]);
			return retval;
		}
	}

}
