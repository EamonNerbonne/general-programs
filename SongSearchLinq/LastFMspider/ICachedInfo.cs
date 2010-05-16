using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
	public interface ICachedInfo<TSelf, TSelfId>
		where TSelfId : IId
		where TSelf : ICachedInfo<TSelf, TSelfId> {
		TSelfId ListID { get; }
		DateTime? LookupTimestamp { get; }
		int? StatusCode { get; }
	}
}
