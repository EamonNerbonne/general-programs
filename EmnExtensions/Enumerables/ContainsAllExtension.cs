﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions {
	public static class ContainsAllExtension {
		public static bool ContainsAll<T>(this IEnumerable<T> list, IEnumerable<T> shouldContain) {
			var set = new HashSet<T>(shouldContain);
			int origCount = set.Count;
			set.IntersectWith(list);
			return set.Count == origCount;
		}
	}
}