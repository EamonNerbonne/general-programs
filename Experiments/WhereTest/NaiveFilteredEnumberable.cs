using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhereTest {
	static class NaiveFilteredEnumberable {
		static IEnumerable<T> Where<T>(IEnumerable<T> list, Func<T, bool> pred) {
			foreach (var item in list)
				if (pred(item))
					yield return item;
		}
	}
}
