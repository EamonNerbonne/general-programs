using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public struct DataRange
	{
		double m_start, m_end;
		public double Interval { get { return m_end - m_start; } }
		public double Start { get { return m_start; } }
		public double End { get { return m_end; } }

		public static DataRange Empty { get { return new DataRange { m_start = double.NaN, m_end = double.NaN }; } }
		public DataRange(double start,double end) {m_start=start;m_end=end; Rect r; r.U
	}
}
