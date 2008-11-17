using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using EmnExtensions.Text;



namespace HttpHeaderHelper
{
	internal static class HeaderParser
	{
		static Regex listOfETagsRegex = new Regex("^\\s*(?<firstETag>(W/)?\"[^\"]*\")?(\\s*,\\s*(?<otherETags>(W/)?\"[^\"]*\")?)*\\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		internal static string[] ParseETagList(string listOfETags) {
			Match m;

			lock(listOfETagsRegex) m = listOfETagsRegex.Match(listOfETags);

			if(!m.Success) return null;

			var etagsCaptured = m.Groups["firstETag"].Captures.Cast<Capture>().Concat(m.Groups["otherETags"].Captures.Cast<Capture>());
			var etags = etagsCaptured.Select(c => c.Value).ToArray();

			if(etags.Length == 0) return null;
			else return etags;
		}

		internal static PreconditionStatus NegateStatus(this PreconditionStatus status) {
			switch(status) {
				case PreconditionStatus.False: return PreconditionStatus.True;
				case PreconditionStatus.True: return PreconditionStatus.False;
				default: return status;
			}
		}

		internal static PreconditionStatus isResourceUpdated(HttpContext context, string headerName, ResourceInfo res) {
			return isResourceUpdated(context.Request.Headers[headerName], res);
		}
		internal static PreconditionStatus isResourceUpdated(string headerVal, ResourceInfo resource) {
			if(headerVal.IsNullOrEmpty()) return PreconditionStatus.Unspecified;

			DateTime? requestTimeStamp = headerVal.ParseAsDateTime();
			if(requestTimeStamp == null) return PreconditionStatus.HeaderError;

			if(resource.RoundedHttpTimeStamp == (DateTime)requestTimeStamp)
				//use exact equality, because if the document is older.. well.. it's still different!
				//this becomes esp. important in the case of partial downloads validated by means of date.
				return PreconditionStatus.False;//i.e. the local version is no newer
			else
				return PreconditionStatus.True;//i.e. the local version is newer (or older - in any case, updated).
		}

		internal static PreconditionStatus isResourceNew(HttpContext context, string headerName, ResourceInfo res) {
			return isResourceNew(context.Request.Headers[headerName], res);
		}
		internal static PreconditionStatus isResourceNew(string knownETags, ResourceInfo resource) {

			if(knownETags.IsNullOrEmpty())
				return PreconditionStatus.Unspecified;

			if(knownETags == "*")
				return PreconditionStatus.False;

			string[] etags = HeaderParser.ParseETagList(knownETags);
			if(etags == null) return PreconditionStatus.HeaderError;

			if(etags.Contains(resource.ETag))//note that this assumes that resource.ETag is a valid, quoted ETag, or is null
				return PreconditionStatus.False;
			else
				return PreconditionStatus.True;
		}

	}
}
