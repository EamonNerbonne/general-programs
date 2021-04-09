using System;
using System.Windows;

namespace EmnExtensions.Wpf
{
    public static partial class WpfTools
    {
        public static T AsFrozen<T>(this T freezable)
            where T : Freezable => (T)freezable.GetAsFrozen();

        public static T AsFrozen<T>(this T freezable, Action<T> changer)
            where T : Freezable
        {
            var copy = (T)freezable.CloneCurrentValue();
            changer(copy);
            copy.Freeze();
            return copy;
        }
    }
}
