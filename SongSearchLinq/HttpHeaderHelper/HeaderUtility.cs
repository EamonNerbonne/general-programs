using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using EamonExtensionsLinq.Text;

namespace HttpHeaderHelper
{
	public static class HeaderUtility
	{

		public static PreconditionStatus CacheConditionIfModifiedSince(HttpContext context, ResourceInfo resource) {
			return HeaderParser.isResourceUpdated(context,HttpHeader.IfModifiedSince, resource);
		}

		public static PreconditionStatus CacheConditionIfNoneMatch(HttpContext context, ResourceInfo resource) {
			return HeaderParser.isResourceNew(context,HttpHeader.IfNoneMatch, resource);
		}

		public static bool IsResponse304NotModified(HttpContext context, ResourceInfo resource) {
			PreconditionStatus ifNoneMatch = CacheConditionIfNoneMatch(context, resource);
			PreconditionStatus ifModifiedSince = CacheConditionIfModifiedSince(context, resource);

			if(ifModifiedSince == PreconditionStatus.True || ifNoneMatch == PreconditionStatus.True)
				return false;//either ETags or LastModified says this request is out of date and can't be cached.
			else if(ifModifiedSince == PreconditionStatus.False || ifNoneMatch == PreconditionStatus.False)
				return true;//at least one caching directive think's this is explicitly OK to cache
			else
				return false;//neither caching directive is present.  Do not cache
		}

		public static bool IsResponse412PreconditionFailed(HttpContext context, ResourceInfo resource) {
			PreconditionStatus ifMatch = PreConditionIfMatch(context, resource);
			PreconditionStatus ifUnmodifiedSince = PreConditionIfUnmodifiedSince(context, resource);

			if(ifMatch == PreconditionStatus.False || ifUnmodifiedSince == PreconditionStatus.False)
				return true;//at least one precondition explicitely failed.
			else
				return false;
		}

		public static PreconditionStatus PreConditionIfUnmodifiedSince(HttpContext context, ResourceInfo resource) {
			return HeaderParser.isResourceUpdated(context, HttpHeader.IfUnmodifiedSince, resource).NegateStatus();
		}

		public static PreconditionStatus PreConditionIfMatch(HttpContext context, ResourceInfo resource) {
			return HeaderParser.isResourceNew(context,HttpHeader.IfMatch, resource).NegateStatus();
		}

		public static PreconditionStatus PreConditionIfRange(HttpContext context, ResourceInfo resource) {
			string ifRangeHeader = context.Request.Headers[HttpHeader.IfRange];
			if(ifRangeHeader.IsNullOrEmpty()) 	return PreconditionStatus.Unspecified;

			if(ifRangeHeader.StartsWith("\"") || ifRangeHeader.StartsWith("W/\"")) {//use etag logic
				return HeaderParser.isResourceNew(ifRangeHeader, resource).NegateStatus();//condition is satisfied when _not_ new
			} else {//use date logic
				return HeaderParser.isResourceUpdated(ifRangeHeader, resource).NegateStatus();//condition satisfied when _not_ updated;
			}
		}

		public static void SetPublicCache(HttpContext context, ResourceInfo resource, DateTime? expiresAt) {
			context.Response.Cache.SetCacheability(HttpCacheability.Public);
			
			//shouldn't be set if response code is 206 and If-Range was present and If-Range used a weak validator 
			context.Response.Cache.SetLastModified(resource.RoundedHttpTimeStamp);
			
			context.Response.Cache.SetETag(resource.ETag);//TODO deal with \" char's
			if(expiresAt != null) {
				context.Response.Cache.SetExpires((DateTime)expiresAt);
				context.Response.Cache.SetSlidingExpiration(true);
			}
		}


		static readonly string bytesStr = "bytes=";
		/// <summary>
		/// Parses the 'Range:' HTTP header, and returns all ranges requested
		/// </summary>
		/// <param name="context">The HTTPContext of the current request.</param>
		/// <param name="contentLength">The Total Length of the current resource, in bytes</param>
		/// <returns>null if the Range Header is not present, otherwise an array of all (valid) Ranges found.  
		/// If the client submitted an invalid request (such as when all ranges are invalid), the array will be empty.</returns>
		public static Range[] ParseRangeHeader(HttpContext context, long contentLength) {
			string rangeHeader = context.Request.Headers[HttpHeader.Range];
			if(rangeHeader.IsNullOrEmpty()) return null;
			
			if(!rangeHeader.StartsWith(bytesStr)) return new Range[] {};//range head present, but no valid ranges.
			
			rangeHeader = rangeHeader.Substring(bytesStr.Length);
			var ranges =
				from listEl in rangeHeader.Split(',')
				let rangeDef = listEl.Trim()
				where rangeDef.Length > 0
				let range = Range.CreateFromString(rangeDef, contentLength)
				where range != null
				select (Range)range;
			return ranges.ToArray();//this is a little too lenient, as it simply ignores invalid specifications instead of marking them as errors.
		}

	}
}
