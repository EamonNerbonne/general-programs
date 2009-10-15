using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace LVQeamon
{
	static class DataVerifiers
	{
		static Brush OK = Brushes.Transparent;
		static Brush BAD = Brushes.Yellow;

		public static bool IsInt32(string value) { int ignore; return Int32.TryParse(value, out ignore); }
		public static bool IsDouble(string value) { double ignore; return Double.TryParse(value, out ignore); }
		public static bool IsDoublePositive(string value) { double ignore; return Double.TryParse(value, out ignore) && ignore > 0.0; }
		public static bool IsInt32Positive(string value) { int intVal; return Int32.TryParse(value, out intVal) && intVal > 0; }
		public static void VerifyTextBox(TextBox textBox, Func<string, bool> isOK) { textBox.Background = isOK(textBox.Text) ? OK : BAD;  }
	}
}
