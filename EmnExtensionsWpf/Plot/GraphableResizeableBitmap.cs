using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;

namespace EmnExtensions.Wpf.Plot
{
	public abstract class GraphableResizeableBitmap : GraphableData
	{

		public GraphableResizeableBitmap() { BitmapScalingMode = BitmapScalingMode.Linear; }
		public BitmapScalingMode BitmapScalingMode { get { return m_scalingMode; } set { m_scalingMode = value; if (m_drawing != null) RenderOptions.SetBitmapScalingMode(m_drawing, value); } }
		BitmapScalingMode m_scalingMode;

		const int EXTRA_RESIZE_PIX = 256;
		double m_dpiX = 96.0, m_dpiY = 96.0;
		protected WriteableBitmap m_bmp;
		RectangleGeometry m_clipGeom = new RectangleGeometry();
		TranslateTransform m_offsetTransform = new TranslateTransform();
		DrawingGroup m_drawing = new DrawingGroup();
		Rect m_outerBounds = Rect.Empty;

		public override void DrawGraph(DrawingContext context) {
			context.DrawDrawing(m_drawing);
			Trace.WriteLine("redraw");
		}

		protected abstract Rect? OuterDataBound { get; }

		static Rect SnapRect(Rect r, double multX, double multY) { return new Rect(new Point(Math.Floor(r.Left / multX) * multX, Math.Floor(r.Top / multY) * multY), new Point(Math.Ceiling((r.Right + 0.01) / multX) * multX, Math.Ceiling((r.Bottom + 0.01) / multY) * multY)); }

		public override void SetTransform(Matrix dataToDisplay, Rect displayClip) {
			if (dataToDisplay.IsIdentity) //TODO: is this a good test for no-show?
				using(m_drawing.Open())
					return;
			Rect drawingClip = displayClip;
			Rect? outerDataBound = OuterDataBound;
			if (outerDataBound.HasValue)
				drawingClip.Intersect(Rect.Transform(outerDataBound.Value, dataToDisplay));

			Rect snappedDrawingClip = SnapRect(drawingClip, 96.0 / m_dpiX, 96.0 / m_dpiY);
			int pW = (int)Math.Ceiling(snappedDrawingClip.Width * m_dpiX / 96.0);
			int pH = (int)Math.Ceiling(snappedDrawingClip.Height * m_dpiY / 96.0);

			Matrix dataToBitmap = dataToDisplay;
			dataToBitmap.Translate(-snappedDrawingClip.X, -snappedDrawingClip.Y);
			dataToBitmap.Scale(m_dpiX / 96.0, m_dpiY / 96.0);

			if (m_offsetTransform.X != snappedDrawingClip.X || m_offsetTransform.Y != snappedDrawingClip.Y)
			{
				m_offsetTransform.X = snappedDrawingClip.X;
				m_offsetTransform.Y = snappedDrawingClip.Y;
			}
			m_clipGeom.Rect = snappedDrawingClip;//TODO: maybe better to clip after transform and then to clip to pW/pH?
			//TODO2: this clips to nearest pixel boundary; but a tighter clip is possible to sub-pixel accuracy.

			if (m_bmp == null || m_bmp.PixelWidth < pW || m_bmp.PixelHeight < pH) {
				int width = Math.Max(m_bmp == null ? 1 : m_bmp.PixelWidth, pW + (int)(EXTRA_RESIZE_PIX));
				int height = Math.Max(m_bmp == null ? 1 : m_bmp.PixelHeight, pH + (int)(EXTRA_RESIZE_PIX));
				m_bmp = new WriteableBitmap(width, height, m_dpiX, m_dpiY, PixelFormats.Bgra32, null);
				using (var context = m_drawing.Open())
				{
					context.PushGuidelineSet(new GuidelineSet(new[] { 0.0 }, new[] { 0.0 }));
					context.PushClip(m_clipGeom);
					context.PushTransform(m_offsetTransform);
					context.DrawImage(m_bmp, new Rect(0, 0, m_bmp.Width, m_bmp.Height));
					context.Pop();
					context.Pop();
					context.Pop();
				}
				Trace.WriteLine("new WriteableBitmap");
			}

			UpdateBitmap(pW, pH, dataToBitmap);
			//painting.
			Trace.WriteLine("retransform");
		}

		protected abstract void UpdateBitmap(int pW, int pH, Matrix dataToBitmap);
	}
}
