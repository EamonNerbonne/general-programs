using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace HttpHeaderHelper {


	public abstract class PotentialResourceInfo { }

	public class ResourceError : PotentialResourceInfo {
		public int Code = 404;
		public string Message;
		public string ShortDescription;
	}

	public class ResourceInfo : PotentialResourceInfo {
		string eTag;
		DateTime? timeStamp;
		public DateTime? TimeStamp { get { return timeStamp; } set { timeStamp = value.HasValue ? value.Value.ToUniversalTime() : default(DateTime?); } }
		public ulong? ResourceLength { get; set; }
		internal DateTime? RoundedHttpTimeStamp { get { return timeStamp.HasValue ? DateTimeToHttpFormat(timeStamp.Value) : (DateTime?)null; } }
		public string ETag {
			get { return eTag; }
			set {
				if (value != null) //then we should verify that it's valid.
					if (!value.EndsWith("\"") || !value.StartsWith("\"") && !value.StartsWith("W/\""))
						throw new ArgumentException("eTag is invalid.  Must be a quoted string, optionally preceded by \"W/\".");
				eTag = value;
			}
		}

		public string MimeType { get; set; }

		private static DateTime DateTimeToHttpFormat(DateTime dt) {
			return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc);
		}

		public static string GenerateETagFrom(params object[] uniqueKeyData) {
			var bf = new BinaryFormatter();
			using(var ms = new MemoryStream()){
				bf.Serialize(ms, uniqueKeyData);
				return GenerateETagFrom(ms.ToArray());
			}
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
