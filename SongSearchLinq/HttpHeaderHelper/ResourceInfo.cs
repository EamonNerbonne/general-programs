using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace HttpHeaderHelper
{


	public abstract class PotentialResourceInfo { }

	public class ResourceError:PotentialResourceInfo
	{
		public int Code=404;
		public string Message=null;
		public string ShortDescription=null;
	}

	public class ResourceInfo:PotentialResourceInfo
	{
		string eTag=null;
		DateTime? timeStamp=null;
		string mimeType = null;
		ulong? resourceLength=null;
		public DateTime? TimeStamp { get { return timeStamp; } set { timeStamp = value; } }
		public ulong? ResourceLength { get { return resourceLength; } set { resourceLength = value; } }
		internal DateTime? RoundedHttpTimeStamp { get { return timeStamp.HasValue?DateTimeToHttpFormat(timeStamp.Value):(DateTime?)null; } }
		public string ETag {
			get { return eTag; }
			set {
				if(value != null) //then we should verify that it's valid.
					if(!value.EndsWith("\"") || !value.StartsWith("\"") && !value.StartsWith("W/\""))
						throw new ArgumentException("eTag is invalid.  Must be a quoted string, optionally preceded by \"W/\".");
				eTag = value;
			}
		}
		public string MimeType { get { return mimeType; } set { mimeType = value; } }
		private static DateTime DateTimeToHttpFormat(DateTime dt) {
			dt = dt.ToUniversalTime();
			DateTime dtUtcRounded = new DateTime(dt.Year, dt.Month, dt.Day,	dt.Hour, dt.Minute, dt.Second,DateTimeKind.Utc);
			return dtUtcRounded;
		}

		public static string GenerateETagFrom(params object[] uniqueKeyData) {
			return GenerateETagFrom(string.Join("\n",uniqueKeyData.Select(data=>data==null?"[null]":data.ToString()).ToArray()));
		}
		public static string GenerateETagFrom(string uniqueKeyData) {
			return GenerateETagFrom(Encoding.UTF8.GetBytes(uniqueKeyData));
		}
		public static string GenerateETagFrom(byte[] uniqueKeyData) {
			MD5 hasher = MD5.Create();
			byte[] hashData = hasher.ComputeHash(uniqueKeyData);
			string base64Etag = "\"" + Convert.ToBase64String(hashData) + "\"";
			return base64Etag;
		}
	}
}
