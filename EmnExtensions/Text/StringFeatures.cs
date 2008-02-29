
namespace EamonExtensionsLinq.Text
{
	public static class StringFeatures
	{
		public static bool IsNullOrEmpty(this string str) {
			return str == null || str.Length == 0;
		}


	}
}
