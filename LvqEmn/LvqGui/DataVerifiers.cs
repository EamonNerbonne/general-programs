// ReSharper disable UnusedMember.Global
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace LvqGui {
	static class DataVerifiers {
		static readonly Brush OK = Brushes.Transparent;
		static readonly Brush BAD = Brushes.Yellow;

		public static bool IsInt32(string value) { int ignore; return Int32.TryParse(value, out ignore); }
		public static bool IsDouble(string value) { double ignore; return Double.TryParse(value, out ignore); }
		public static bool IsDoublePositive(string value) { double ignore; return Double.TryParse(value, out ignore) && ignore > 0.0; }
		public static bool IsInt32Positive(string value) { int intVal; return Int32.TryParse(value, out intVal) && intVal > 0; }
		public static void VerifyTextBox(TextBox textBox, Func<string, bool> isOK) { textBox.Background = isOK(textBox.Text) ? OK : BAD; }
	}
}
