using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;


namespace HttpHeaderHelper
{
	public static class HeaderParser
	{
		static Regex listOfETagsRegex = new Regex("^\\s*(?<firstETag>(W/)?\"[^\"]*\")?(\\s*,\\s*(?<otherETags>(W/)?\"[^\"]*\")?)*\\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		public static string[] ParseETagList(string listOfETags) {
			Match m;

			lock(listOfETagsRegex) m = listOfETagsRegex.Match(listOfETags);

			if(!m.Success) return null;

			var etagsCaptured = m.Groups["firstETag"].Captures.Cast<Capture>().Concat(m.Groups["otherETags"].Captures.Cast<Capture>());
			var etags = etagsCaptured.Select(c => c.Value).ToArray();

			if(etags.Length == 0) return null;
			else return etags;
		}

	}
}
