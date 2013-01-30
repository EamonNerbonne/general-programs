using System;
using System.Collections.Generic;

namespace HtmlGenerator
{
	static class SListHelpers
	{
		public static T[] ToArray<T>(this SList<T> list)
		{
			if (list == null)
				return SList<T>.EmptyArray;
			var buffer = new List<T>();
			do
			{
				buffer.Add(list.Item);
				list = list.Next;
			} while (list != null);
			var arr = buffer.ToArray();
			Array.Reverse(arr);
			return arr;
		}
		public static SList<T> Prepend<T>(this SList<T> list, T item) { return new SList<T>(list, item); }

	}
}