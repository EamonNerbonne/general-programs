// ReSharper disable UnusedMember.Global

using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace LvqGui
{
    static class UserInputVerifiers
    {
        static readonly Brush OK = Brushes.Transparent;
        static readonly Brush BAD = Brushes.Yellow;

        public static bool IsInt32(string value)
        {
            return int.TryParse(value, out var ignore);
        }

        public static bool IsDouble(string value)
        {
            return double.TryParse(value, out var ignore);
        }

        public static bool IsDoublePositive(string value)
        {
            return double.TryParse(value, out var ignore) && ignore > 0.0;
        }

        public static bool IsInt32Positive(string value)
        {
            return int.TryParse(value, out var intVal) && intVal > 0;
        }

        public static void VerifyTextBox(TextBox textBox, Func<string, bool> isOK) => textBox.Background = isOK(textBox.Text) ? OK : BAD;
    }
}
