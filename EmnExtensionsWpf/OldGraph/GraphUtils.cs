using System;
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
		public static bool IsFiniteNonEmpty(this Rect rect) { return rect.Width.IsFinite() && rect.Height.IsFinite() && rect.Height * rect.Width > 0; }

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

		/// <summary>
		/// Makes a StreamGeometry based point-cloud (quite fast).
		/// </summary>
		public static StreamGeometry PointCloud(IEnumerable<Point> setOfPoints) {

			StreamGeometry geom = new StreamGeometry();
			using (var context = geom.Open()) {
				foreach (var point in setOfPoints) {
					if (IsOK(point)) {
						context.BeginFigure(point, false, false);

						context.LineTo(point, true, false);
					}
				}
			}
			//geom.Freeze();//can't freeze since that breaks transform changes.
			return geom;
		}

		/// <summary>
		/// Makes a filled-square based point cloud; radius is in data-space, so distorted (fast)
		/// </summary>
		public static StreamGeometry PointCloud4(IEnumerable<Point> setOfPoints, double radius) {
			StreamGeometry geom = new StreamGeometry();
			geom.FillRule = FillRule.Nonzero;
			using (var context = geom.Open()) {
				foreach (var point in setOfPoints) {
					if (IsOK(point)) {
						point.Offset(-radius, -radius);
						context.BeginFigure(point, true, false);
						point.Offset(radius * 2, 0.0);
						context.LineTo(point, true, false);
						point.Offset(0, radius * 2);
						context.LineTo(point, true, false);
						point.Offset(-2 * radius, 0.0);
						context.LineTo(point, true, false);


						//point.Offset(radius, 0);
						//context.BeginFigure(point, true, true);


						//point.Offset(-2 * radius, 0);
						//context.ArcTo(point, new Size(radius, radius), 0.0, true, SweepDirection.Clockwise, true, true);
						//point.Offset(2 * radius, 0);
						//context.ArcTo(point, new Size(radius, radius), 0.0, true, SweepDirection.Clockwise, true, true);
					}
				}
			}
			//geom.Freeze();//can't freeze since that breaks transform changes.
			return geom;
		}

		/// <summary>
		/// Makes a DrawingVisual based point cloud (slow)
		/// </summary>
		public static DrawingVisual PointCloud2(IEnumerable<Point> setOfPoints, Brush brush, double radius, out Rect bounds) {

			DrawingVisual vis = new DrawingVisual();
			bounds = Rect.Empty;
			using (DrawingContext ctx = vis.RenderOpen()) {
				foreach (var point in setOfPoints) {
					if (IsOK(point)) {
						ctx.DrawEllipse(brush, null, point, radius, radius);
						bounds.Union(point);
					}
				}
			}

			return vis;
		}

		/// <summary>
		/// Makes a DrawingGroup based point cloud (slow)
		/// </summary>
		public static DrawingGroup PointCloud3(IEnumerable<Point> setOfPoints, Brush brush, double radius, out Rect bounds) {
			DrawingGroup drawing = new DrawingGroup();
			bounds = Rect.Empty;
			using (DrawingContext ctx = drawing.Open()) {
				foreach (var point in setOfPoints) {
					if (IsOK(point)) {
						ctx.DrawEllipse(brush, null, point, radius, radius);
						bounds.Union(point);
					}
				}
			}
			drawing.Freeze();
			return drawing;
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

		/// <summary>
		/// Extends the line made by the last figure in the geometry to the given point.  
		/// </summary>
		/// <param name="toGeom">The PathGeometry to extend.</param>
		/// <param name="point">The point to draw a line to.  Note; IsOK(point) must hold (i.e.
		/// no NaN of Inf points) for sane results;</param>
		public static void AddPoint(PathGeometry toGeom, Point point) {
			var figs = toGeom.Figures;
			var lastFig = figs.Count > 0 ? figs[figs.Count - 1] : null;
			if (lastFig == null) {
				lastFig = new PathFigure {
					StartPoint = point
				};
				toGeom.Figures.Add(lastFig);
			} else {
				lastFig.Segments.Add(new LineSegment(point, true));
			};
		}

		public static Rect ExpandRect(this Rect src, Thickness withMargin) {
			return new Rect(src.X - withMargin.Left, src.Y - withMargin.Top, src.Width + withMargin.Left + withMargin.Right, src.Height + withMargin.Top + withMargin.Bottom);
		}
		public static Rect ShrinkRect(this Rect src, Thickness withMargin) {
			return new Rect(src.X + withMargin.Left, src.Y + withMargin.Top, src.Width - withMargin.Left - withMargin.Right, src.Height - withMargin.Top - withMargin.Bottom);
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
			int i = 0;
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					inlinearray[i++] = image[y, x];
			return BitmapSource.Create(w, h, 96.0, 96.0, PixelFormats.Gray8, null, inlinearray, w);
		}

		public static uint ToNativeColor(this Color colorstruct) {
			return ((uint)colorstruct.A << 24) | ((uint)colorstruct.R << 16) | ((uint)colorstruct.G << 8) | ((uint)colorstruct.B);
		}
		
		public static BitmapSource MakeColormappedBitmap<T>(T[,] image, Func<T, Color> colormap) {
			return MakeColormappedBitmap(image, colormap, 1);
		}
		public static BitmapSource MakeColormappedBitmap<T>(T[,] image, Func<T, Color> colormap, int sampleFactor) {
			int w = image.GetLength(1) * sampleFactor, h = image.GetLength(0) * sampleFactor;
			uint[] inlinearray = new uint[w * h];
			int i = 0;
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					inlinearray[i++] = ToNativeColor(colormap(image[y / sampleFactor, x / sampleFactor]));
			return BitmapSource.Create(w, h, 96.0, 96.0, PixelFormats.Bgra32, null, inlinearray, w * 4);
		}

		public static Drawing MakeBitmapDrawing(BitmapSource bitmap, double yStart, double yEnd, double xStart, double xEnd) {
			var img = new ImageDrawing(bitmap, new Rect(0, 0, bitmap.Width, bitmap.Height));

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
