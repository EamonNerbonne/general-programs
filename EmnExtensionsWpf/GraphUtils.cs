﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EmnExtensions.Wpf
{
	public static class GraphUtils
	{
		public static PathGeometry LineWithErrorBars(Point[] lineOfPoints, double[] ErrBars) {
			PathGeometry geom = new PathGeometry();
			PathFigure fig = null;
			foreach (Point startPoint in lineOfPoints.Take(1)) {
				fig = new PathFigure();
				fig.StartPoint = startPoint;
				geom.Figures.Add(fig);
			}
			foreach (Point point in lineOfPoints.Skip(1)) {
				fig.Segments.Add(new LineSegment(point, true));
			}
			Rect bounds = geom.Bounds;
			double errWidth = bounds.Width / 200.0;
			for (int i = 0; i < lineOfPoints.Length; i++) {
				if (ErrBars[i].IsFinite()) {
					PathFigure errf = new PathFigure();
					errf.StartPoint = lineOfPoints[i] + new Vector(-errWidth, -ErrBars[i]);
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(errWidth, -ErrBars[i]), true));
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(0, -ErrBars[i]), false));
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(0, ErrBars[i]), true));
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(errWidth, ErrBars[i]), false));
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(-errWidth, ErrBars[i]), true));
					geom.Figures.Add(errf);
				}
			}

			return geom;
		}

		public static PathGeometry LineAsPathGeometry(IEnumerable<Point> lineOfPoints) {
			lineOfPoints = lineOfPoints.SkipWhile(p => !IsOK(p));

			PathGeometry geom = new PathGeometry();
			PathFigure fig = null;
			foreach (Point startPoint in lineOfPoints.Take(1)) {
				fig = new PathFigure();
				fig.StartPoint = startPoint;
				geom.Figures.Add(fig);
			}
			bool wasOK = true;
			foreach (Point point in lineOfPoints.Skip(1)) {
				if (IsOK(point)) {
					fig.Segments.Add(new LineSegment(point, wasOK));
					wasOK = true;
				} else {
					wasOK = false;
				}
			}
			return geom;
		}

		public static StreamGeometry Line(IEnumerable<Point> lineOfPoints) {
			StreamGeometry geom = new StreamGeometry();
			using (var context = geom.Open()) {
				bool wasOK = false;
				foreach (var point in lineOfPoints) {
					if (IsOK(point)) {
						if (wasOK)
							context.LineTo(point, true, true);
						else
							context.BeginFigure(point, false, false);
						wasOK = true;
					} else wasOK = false;
				}
			}
			//geom.Freeze();//can't freeze since that breaks transform changes.
			return geom;
		}

		public static IEnumerable<Point> PathFigurePoints(PathFigure fig) {
			if (fig == null) yield break;
			yield return fig.StartPoint;
			foreach (LineSegment lineTo in fig.Segments)
				yield return lineTo.Point;
		}

		public static void AddPoint(PathFigure fig, Point point) {
			fig.Segments.Add(new LineSegment(point, true));
		}

		public static Matrix TransformShape(Rect fromPosition, Rect toPosition, bool flipVertical) {
			Matrix translateThenScale = Matrix.Identity;
			//we first translate to origin since that's just easier
			translateThenScale.Translate(-fromPosition.X, -fromPosition.Y);
			//now we scale the graph to the appropriate dimensions
			translateThenScale.Scale(toPosition.Width / fromPosition.Width, toPosition.Height / fromPosition.Height);
			//then we flip the graph vertically around the viewport middle since in our graph positive is up, not down.
			if (flipVertical)
				translateThenScale.ScaleAt(1.0, -1.0, 0.0, toPosition.Height / 2.0);
			//now we push the graph to the right spot, which will usually simply be 0,0.
			translateThenScale.Translate(toPosition.X, toPosition.Y);

			return translateThenScale;
		}


		private static bool IsOK(Point p) { return p.X.IsFinite() && p.Y.IsFinite(); }

		public static BitmapSource MakeGreyBitmap(byte[,] image) {
			int w = image.GetLength(1), h = image.GetLength(0);
			byte[] inlinearray = new byte[w * h];
			int i=0;
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					inlinearray[i++] = image[y, x];
			return BitmapSource.Create(w, h, 96.0, 96.0, PixelFormats.Gray8, null, inlinearray, w);
		}

		public static uint ToNativeColor(this Color colorstruct) {
			return ((uint)colorstruct.A << 24) | ((uint)colorstruct.R << 16) | ((uint)colorstruct.G << 8) | ((uint)colorstruct.B);
		}
		public static BitmapSource MakeColormappedBitmap(double[,] image, Func<double, Color> colormap) {
			return MakeColormappedBitmap(image, colormap, 1);
		}
		public static BitmapSource MakeColormappedBitmap(double[,] image, Func<double, Color> colormap, int sampleFactor) {
			int w = image.GetLength(1)*sampleFactor, h = image.GetLength(0)*sampleFactor;
			uint[] inlinearray = new uint[w * h];
			int i = 0;
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					inlinearray[i++] = ToNativeColor(colormap(image[y/sampleFactor, x/sampleFactor]));
			return BitmapSource.Create(w, h, 96.0, 96.0, PixelFormats.Bgra32, null, inlinearray, w * 4);
		}

		public static Drawing MakeBitmapDrawing(BitmapSource bitmap, double yStart,double yEnd, double xStart, double xEnd) {
			var img= new ImageDrawing(bitmap, new Rect(0, 0, bitmap.Width, bitmap.Height));

			Matrix trans = Matrix.Identity;
			trans.Scale((xEnd - xStart) / bitmap.Width, (yEnd - yStart) / bitmap.Height);
			trans.Translate(xStart, yStart);
			MatrixTransform transD = new MatrixTransform(trans);
			Rect clipRect = transD.TransformBounds(new Rect(0, 0, bitmap.Width, bitmap.Height));
			var retval = new DrawingGroup();
			using (var context = retval.Open()) {
				context.PushClip(new RectangleGeometry(clipRect));
				context.PushTransform(transD);
				context.DrawDrawing(img);
				context.Pop();
				context.Pop();
			}
			RenderOptions.SetBitmapScalingMode(retval, BitmapScalingMode.NearestNeighbor);
			return retval;
		}
	}

}
