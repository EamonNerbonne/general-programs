﻿using System.Collections.Generic;
namespace EmnExtensions.Algorithms {
	public static class SetEqualsExtension {
		public static bool SetEquals<T>(this IEnumerable<T> xs, IEnumerable<T> ys) { return new HashSet<T>(xs).SetEquals(ys); }
	}
}
