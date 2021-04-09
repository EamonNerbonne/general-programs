using System;
using System.Globalization;
using System.Windows.Data;

namespace LvqGui
{
    public sealed class HexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(string) || !(value is uint)) {
                throw new InvalidOperationException("Cannot convert from " + value.GetType() + " to " + targetType);
            }

            return System.Convert.ToString((uint)value, 16);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string) || targetType != typeof(uint)) {
                throw new InvalidOperationException("Cannot convert from " + value.GetType() + " to " + targetType);
            }

            return System.Convert.ToUInt32((string)value, 16);
        }
    }
}
