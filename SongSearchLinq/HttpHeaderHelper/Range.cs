using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq.Text;

namespace HttpHeaderHelper
{
	public struct Range
	{
		/// <summary>
		/// the index of the first byte
		/// </summary>
		public int start;
		/// <summary>
		/// the number of bytes requested
		/// </summary>
		public int length;

		public static Range? CreateFromString(string rangeDef, int contentLength) {//i.e. "1020-2020" or "1234-"
			var points = rangeDef.Split('-');
			if(points.Length != 2) return null;
			var startP = points[0].ParseAsInt32();
			var endP = points[1].ParseAsInt32();

			if(startP == null && endP != null)
				return new Range { start = contentLength - (int)endP, length = (int)endP };
			else if(startP != null && endP == null)
				return new Range { start = (int)startP, length = contentLength - (int)startP };
			else if(startP != null && endP != null && startP < contentLength && startP <= endP)
				return new Range { start = (int)startP, length = Math.Min((int)endP + 1 - (int)startP, contentLength - (int)startP) };
			else
				return null;
		}
	}
}
