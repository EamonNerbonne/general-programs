using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;

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

		public static object[] ExecuteGetTopRow(this DbCommand command) {
			using (var reader = command.ExecuteReader()) {
				if (!reader.Read())
					return null;
				object[] retval = new object[reader.FieldCount];
				int read=reader.GetValues(retval);
				if (retval.Length != read) throw new Exception("Logic error: returned inconsistent number of fields?");
				return retval;
			}
		}
	}
}
