using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HttpHeaderHelper
{
	public class ResourceInfo
	{
		public DateTime InteralTimeStamp { get { return timeStamp; } }
		public DateTime RoundedHttpTimeStamp { get { return DateTimeToHttpFormat(timeStamp); } }
		public string ETag { get { return eTag; } }

		public ResourceInfo(DateTime timeStamp, string eTag) {
			if(eTag != null) //then we should verify that it's valid.
				if(!eTag.EndsWith("\"") || !eTag.StartsWith("\"") && !eTag.StartsWith("W/\""))
					throw new ArgumentException("eTag is invalid.  Must be a quoted string, optionally preceded by \"W/\".", eTag);
			this.timeStamp = timeStamp;
			this.eTag = eTag;
		}

		string eTag;
		DateTime timeStamp;

		static DateTime DateTimeToHttpFormat(DateTime dt) {
			dt = dt.ToUniversalTime();//TODO is this necessary?  Does this break if DateTime is already in UTC?
			DateTime dtUtcRounded = new DateTime(dt.Year, dt.Month, dt.Day,
				dt.Hour, dt.Minute, dt.Second);
			return dtUtcRounded;
		}

	}
}
