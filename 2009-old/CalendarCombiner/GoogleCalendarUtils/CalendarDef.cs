using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.GData.Calendar;

namespace GoogleCalendarUtils
{
	public struct CalendarDef
	{
		public string Uri { get { return entry.Links[0].AbsoluteUri; } }
		public string Title { get { return entry.Title.Text; } }
		public bool IsOwned { get { return entry.AccessLevel == "owner"; } }

		public readonly CalendarEntry entry;
		public CalendarDef(CalendarEntry entry) { this.entry = entry; }
	}
}
