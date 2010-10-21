using System.Windows;

namespace EmnExtensions.Wpf {
	public static partial class WpfTools {
		public static T AsFrozen<T>(this T freezable) where T : Freezable {
			return (T)freezable.GetAsFrozen(); 
		}
	}
}
