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
			string ifModifiedSinceHeader = context.Request.Headers["If-Modified-Since"];
			if(ifModifiedSinceHeader.IsNullOrEmpty()) return PreconditionStatus.Unspecified;

			DateTime? requestTimeStamp = ifModifiedSinceHeader.ParseAsDateTime();
			if(requestTimeStamp == null) return PreconditionStatus.HeaderError;

			if(resource.RoundedHttpTimeStamp <= (DateTime)requestTimeStamp)
				return PreconditionStatus.False;//i.e. the local version is no newer
			else
				return PreconditionStatus.True;//i.e. the local version is newer.
		}

		public static PreconditionStatus PreConditionIfUnmodifiedSince(HttpContext context, ResourceInfo resource) {
			string ifUnmodifiedSinceHeader = context.Request.Headers["If-Unmodified-Since"];
			if(ifUnmodifiedSinceHeader.IsNullOrEmpty()) return PreconditionStatus.Unspecified;

			DateTime? requestTimeStamp = ifUnmodifiedSinceHeader.ParseAsDateTime();
			if(requestTimeStamp == null) return PreconditionStatus.HeaderError;

			if(resource.RoundedHttpTimeStamp <= (DateTime)requestTimeStamp)
				return PreconditionStatus.True;
			else
				return PreconditionStatus.False;
		}


		public static PreconditionStatus CacheConditionIfNoneMatch(HttpContext context, ResourceInfo resource) {
			string ifNoneMatchHeader = context.Request.Headers["If-None-Match"];

			if(ifNoneMatchHeader.IsNullOrEmpty())
				return PreconditionStatus.Unspecified;

			if(ifNoneMatchHeader == "*")
				return PreconditionStatus.False;

			string[] etags = HeaderParser.ParseETagList(ifNoneMatchHeader);
			if(etags == null) return PreconditionStatus.HeaderError;

			if(etags.Contains(resource.ETag))//note that this assumes that resource.ETag is a valid, quoted ETag, or is null
				return PreconditionStatus.False;
			else
				return PreconditionStatus.True;

		}

		public static PreconditionStatus PreConditionIfMatch(HttpContext context, ResourceInfo resource) {
			//TODO refactor with CacheConditionIfNoneMatch
			string ifMatchHeader = context.Request.Headers["If-Match"];
			if(ifMatchHeader.IsNullOrEmpty())
				return PreconditionStatus.Unspecified;

			if(ifMatchHeader == "*")
				return PreconditionStatus.True;

			string[] etags = HeaderParser.ParseETagList(ifMatchHeader);
			if(etags == null) return PreconditionStatus.HeaderError;

			if(etags.Contains(resource.ETag))
				return PreconditionStatus.True;
			else
				return PreconditionStatus.False;
		}

		public static PreconditionStatus PreConditionIfRange(HttpContext context, ResourceInfo resource) {
			//TODO refactor with CacheConditionIfNoneMatch
			string ifMatchHeader = context.Request.Headers["If-Range"];
			if(ifMatchHeader.IsNullOrEmpty())
				return PreconditionStatus.Unspecified;

			if(ifMatchHeader.StartsWith("\"") || ifMatchHeader.StartsWith("W/\"")) {//use etag logic

			}

			string[] etags = HeaderParser.ParseETagList(ifMatchHeader);
			if(etags == null) return PreconditionStatus.HeaderError;

			if(etags.Contains(resource.ETag))
				return PreconditionStatus.True;
			else
				return PreconditionStatus.False;
		}

		public static void SetPublicCache(HttpContext context, ResourceInfo resource, DateTime? expiresAt) {
			context.Response.Cache.SetCacheability(HttpCacheability.Public);
			context.Response.Cache.SetLastModified(resource.RoundedHttpTimeStamp);
			context.Response.Cache.SetETag(resource.ETag);//TODO deal with \" char's
			if(expiresAt != null) {
				context.Response.Cache.SetExpires((DateTime)expiresAt);
				context.Response.Cache.SetSlidingExpiration(true);
			}
		}

		public static bool IsRangeRequest(HttpContext context) {
			string rangeHeader = context.Request.Headers["Range"];
			return !rangeHeader.IsNullOrEmpty();
		}

		static readonly string bytesStr = "bytes=";
		public static Range[] ParseRangeHeader(HttpContext context, int contentLength) {
			string rangeHeader = context.Request.Headers["Range"];
			if(!rangeHeader.StartsWith(bytesStr)) return null;
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
