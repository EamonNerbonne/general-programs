using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Diagnostics;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public abstract class VizDynamicBitmap<T> : PlotVizBase<T> {
		public VizDynamicBitmap() { BitmapScalingMode = BitmapScalingMode.Linear; }
		public BitmapScalingMode BitmapScalingMode { get { return m_scalingMode; } set { m_scalingMode = value; if (m_drawing != null) RenderOptions.SetBitmapScalingMode(m_drawing, value); } }
		BitmapScalingMode m_scalingMode;

		//DrawingGroup painting = new DrawingGroup();
		const int EXTRA_RESIZE_PIX = 128;
		protected WriteableBitmap m_bmp;
		RectangleGeometry m_clipGeom = new RectangleGeometry();
		MatrixTransform m_bitmapToDisplayTransform = new MatrixTransform();
		DrawingGroup m_drawing = new DrawingGroup();

		public sealed override void DrawGraph(DrawingContext context) {
			Trace.WriteLine("redraw");
			context.DrawDrawing(m_drawing);
		}

		static Rect SnapRect(Rect r, double multX, double multY) { return new Rect(new Point(Math.Floor(r.Left / multX) * multX, Math.Floor(r.Top / multY) * multY), new Point(Math.Ceiling((r.Right + 0.01) / multX) * multX, Math.Ceiling((r.Bottom + 0.01) / multY) * multY)); }

		public sealed override void SetTransform(Matrix dataToDisplay, Rect displayClip, double dpiX, double dpiY) {
			if (displayClip.IsEmpty) //TODO: is this a good test for no-show?
				using (m_drawing.Open())
					return;


			Rect drawingClip = ComputeRelevantDisplay(displayClip, OuterDataBound, dataToDisplay);

			Rect snappedDrawingClip = SnapRect(drawingClip, 96.0 / dpiX, 96.0 / dpiY);


			var dataToBitmapToDisplay = SplitDataToDisplay(dataToDisplay, snappedDrawingClip, dpiX, dpiY);

			m_bitmapToDisplayTransform.Matrix = dataToBitmapToDisplay.Item2;

			m_clipGeom.Rect = snappedDrawingClip;
			//TODO: maybe better to clip after transform and then to clip to pW/pH?
			//Also, this clips to nearest pixel boundary; but a tighter clip is possible to sub-pixel accuracy:
			//m_clipGeom.Rect = drawingClip;

			int pW = (int)(0.5 + snappedDrawingClip.Width * dpiX / 96.0);
			int pH = (int)(0.5 + snappedDrawingClip.Height * dpiY / 96.0);
			if (m_bmp == null || m_bmp.PixelWidth < pW || m_bmp.PixelHeight < pH) {
				int width = Math.Max(m_bmp == null ? 1 : m_bmp.PixelWidth, pW + (int)(EXTRA_RESIZE_PIX));
				int height = Math.Max(m_bmp == null ? 1 : m_bmp.PixelHeight, pH + (int)(EXTRA_RESIZE_PIX));
				m_bmp = new WriteableBitmap(width, height, dpiX, dpiY, PixelFormats.Bgra32, null);
				using (var context = m_drawing.Open()) {
					context.PushGuidelineSet(new GuidelineSet(new[] { 0.0 }, new[] { 0.0 }));
					context.PushClip(m_clipGeom);
					context.PushTransform(m_bitmapToDisplayTransform);
					context.DrawImage(m_bmp, new Rect(0, 0, m_bmp.Width, m_bmp.Height));
					context.Pop();
					context.Pop();
					context.Pop();
				}
				Trace.WriteLine("new WriteableBitmap");
			}

			UpdateBitmap(pW, pH, dataToBitmapToDisplay.Item1);
			//painting.
			Trace.WriteLine("retransform");
		}

		static Rect ComputeRelevantDisplay(Rect clip, Rect? dataBounds, Matrix dataToDisplay) {
			if (dataBounds.HasValue)
				clip.Intersect(Rect.Transform(dataBounds.Value, dataToDisplay));
			return clip;
		}

		static Tuple<Matrix, Matrix> SplitDataToDisplay(Matrix dataToDisplay, Rect snappedDrawingClip, double dpiX, double dpiY) {

			Matrix dataToBitmap = dataToDisplay;
			dataToBitmap.Translate(-snappedDrawingClip.X, -snappedDrawingClip.Y); //transform real-location --> coordinates
			dataToBitmap.Scale(dpiX / 96.0, dpiY / 96.0); //transform from abstract units --> pixels

			Matrix bitmapToDisplay = Matrix.Identity;
			dataToBitmap.Scale(96.0 / dpiX, 96.0 / dpiY); //transform pixels --> abstract units
			bitmapToDisplay.Translate(snappedDrawingClip.X, snappedDrawingClip.Y); //transform coordinates --> real-location

			return Tuple.Create(dataToBitmap, bitmapToDisplay);
		}

		protected abstract void UpdateBitmap(int pW, int pH, Matrix dataToBitmap);

		//DataBound includes the portion of the data to display; may exclude irrelevant portions.  
		//The actual display may be larger due to various reasons and that can be inefficient.
		//We can't clip to DataBound, however, since _if_ there's more space leaving out irrelevant portions is misleading (cut-off scatter plots, etc.)
		//OuterDataBound is the utmost outer bound:
		//providing one is an optimization that permits using a smaller bitmap; the rest of the drawing is just left blank then.
		//if you don't provide an OuterDataBound, the entire display clip will be available as a WriteableBitmap.
		protected abstract Rect? OuterDataBound { get; }

		public override bool SupportsColor { get { return false; } }
	}
}
