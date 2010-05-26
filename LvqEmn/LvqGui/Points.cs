using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace LvqGui {
	public static class Points {
		//public static double[,] ToArray(IEnumerable<Point> cliPoints) {

		//}

		public static IEnumerable<Point> ToMediaPoints(double[,] arrayPoints) {
			return Enumerable.Range(0, arrayPoints.GetLength(0)).Select(i => new Point(arrayPoints[i, 0], arrayPoints[i, 1]));
		}
	}

}
