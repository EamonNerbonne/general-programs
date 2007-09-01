using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HttpHeaderHelper
{
	public static class HttpHeader
	{
		public static readonly string IfModifiedSince = "If-Modified-Since";
		public static readonly string IfUnmodifiedSince = "If-Unmodified-Since";
		public static readonly string IfMatch = "If-Match";
		public static readonly string IfNoneMatch = "If-None-Match";
		public static readonly string IfRange = "If-Range";
		public static readonly string Range = "Range";
		public static readonly string ContentRange = "Content-Range";
		public static readonly string ContentLength = "Content-Length";
		public static readonly string AcceptRanges = "Accept-Ranges";

	}
}
