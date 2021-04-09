using System.Windows;
using System;

namespace EmnExtensions.Wpf {
    public static partial class WpfTools {
        public static T AsFrozen<T>(this T freezable) where T : Freezable {
            return (T)freezable.GetAsFrozen(); 
        }
        public static T AsFrozen<T>(this T freezable,Action<T> changer) where T : Freezable {
            T copy=(T)freezable.CloneCurrentValue();
            changer(copy);
            copy.Freeze();
            return copy;
        }
    }
}
