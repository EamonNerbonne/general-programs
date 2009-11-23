using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public class VizDelegateBitmap : VizDynamicBitmap
	{
		public VizDelegateBitmap() { UpdateBitmapDelegate = DefaultUpdateBitmapDelegate; }
		static void DefaultUpdateBitmapDelegate(WriteableBitmap bmp, Matrix mat, int pixelWidth, int pixelHeight, object data) { }
		protected override Rect? OuterDataBound { get { return m_OuterDataBound; } }
		public Rect? MaximalDataBound { get { return m_OuterDataBound; } set { m_OuterDataBound = value; OnChange(GraphChange.Projection); } }
		Rect? m_OuterDataBound;

		/// <summary>
		/// This delegate is called whenever the bitmap needs to be updated.
		/// The first parameter is the bitmap that needs to be written to (eventual locking is the responsibility of the client code).
		/// The second parameter is the matrix projecting data point to pixel coordinates (to project pixel coordinates to data space, it must be inverted)
		/// The third and forth parater are the width and height (respectively) of the region in the bitmap that is onscreen.  This region may be smaller than the overall writeable bitmap, and always starts at 0,0.
		/// The final parameter is the data to be plotted - this is simply IPlotData.RawData passed for convenience
		/// </summary>
		public Action<WriteableBitmap, Matrix, int, int, object> UpdateBitmapDelegate { get; set; }

		protected override void UpdateBitmap(int pW, int pH, Matrix dataToBitmap) { UpdateBitmapDelegate(m_bmp, dataToBitmap, pW, pH, Owner.RawData); }
		public override void DataChanged(object newData) { OnChange(GraphChange.Projection); }
	}
}
