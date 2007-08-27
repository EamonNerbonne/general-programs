using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EamonExtensionsLinq.Text
{
	public static class StringFeatures
	{
		public static bool IsNullOrEmpty(this string str) {
			return str == null || str.Length == 0;
		}

	}
}
