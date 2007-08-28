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
		public long start;
		/// <summary>
		/// the number of bytes requested
		/// </summary>
		public long length;

		public long lastByte { get { return start + length - 1; } }

		public static Range? CreateFromString(string rangeDef, long contentLength) {//i.e. "1020-2020" or "1234-"
			var points = rangeDef.Split('-');
			if(points.Length != 2) return null;
			var startP = points[0].ParseAsInt32();
			var endP = points[1].ParseAsInt32();

			if(startP == null && endP != null)
				return new Range { start = contentLength - (long)endP, length = (long)endP };
			else if(startP != null && endP == null)
				return new Range { start = (long)startP, length = contentLength - (long)startP };
			else if(startP != null && endP != null && startP < contentLength && startP <= endP)
				return new Range { start = (long)startP, length = Math.Min((long)endP + 1 - (long)startP, contentLength - (long)startP) };
			else
				return null;
		}
	}
}
