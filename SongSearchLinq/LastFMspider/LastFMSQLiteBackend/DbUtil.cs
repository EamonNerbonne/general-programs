using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LastFMspider.LastFMSQLiteBackend {
	internal static class DbUtil {
		//throws an exception when casting DbNull to non-nullable type.
		public static T CastDbObjectAs<T>(this object dbObject) {
			return (T)(
				dbObject == DBNull.Value
				? null
				: dbObject);
		}
		//converts ticks to UtcDateTime
		public static DateTime? CastDbObjectAsDateTime(this object dbObject) {
			return dbObject == DBNull.Value
				? (DateTime?)null
				: new DateTime((long)dbObject, DateTimeKind.Utc);
		}

		public static IEnumerable<Tuple<uint, float>> DecodeRatingBlob(byte[] arr) {
			using (var ms = new MemoryStream(arr))
			using (var br = new BinaryReader(ms))
				while (br.PeekChar() != -1) {
					var id = br.ReadUInt32();
					var sim = br.ReadSingle();
					yield return Tuple.Create(id, sim);
				}
		}

		public static byte[] EncodeRatingBlob(IEnumerable<Tuple<uint, float>> ratings) {
			using (var ms = new MemoryStream()) {
				using (var bw = new BinaryWriter(ms))
					foreach (var entry in ratings) {
						bw.Write(entry.Item1);
						bw.Write(entry.Item2);
					}
				return ms.ToArray();
			}
		}
	}
}
