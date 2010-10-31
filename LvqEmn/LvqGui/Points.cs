using System.Windows;

namespace LvqGui {
	public static class Points {
		public static Point[] ToMediaPoints(double[,] arrayPoints) {
			int pointCount = arrayPoints.GetLength(0);
			Point[] retval = new Point[pointCount];
			for (int i = 0; i < pointCount; ++i) retval[i] = GetPoint(arrayPoints,i);
			return retval;
		}

		public static Point GetPoint(double[,] arrayPoints, int index) {
			return new Point(arrayPoints[index, 0], arrayPoints[index, 1]);
		}
	}
}
