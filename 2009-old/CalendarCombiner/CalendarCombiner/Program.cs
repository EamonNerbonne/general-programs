using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;
using System.Xml.Linq;
using EamonExtensionsLinq.DebugTools;
using AcloSync;
using GoogleCalendarUtils;

namespace CalendarCombiner
{
	class Program
	{
		static DateTime Max(DateTime a, DateTime b) {
			return a < b ? b : a;
		}
		static DateTime Min(DateTime a, DateTime b) {
			return a >= b ? b : a;
		}
		static string makeLegible(params Google.GData.Calendar.EventEntry[] a) {
			return string.Join("\n", (from x in a
											  select
												  string.Join(", ", (from y in Event.CreateEventsFromGoogleEventEntry(x) select y.ToString()).ToArray())).ToArray());

		}

		static void Main(string[] args) {
			GoogleCalendarService service = new GoogleCalendarService(args[0], args[1]);
			var calendar = service.GetOwnCalendarEntries().Where(cd => cd.Title == "Eamon Nerbonne").First();
			var googleEvents = service.GetCalendarEvents(calendar, new DateTime(2007, 10, 26, 18, 0, 0), new DateTime(2007, 10, 27)).Where(e=>e.Status != EventEntry.EventStatus.CANCELED).ToArray();
			foreach(var gE in googleEvents) {
				Console.WriteLine(makeLegible(gE));
				Console.WriteLine(gE.Status.Value);
			}
			var eamonEvents = googleEvents.SelectMany(e => Event.CreateEventsFromGoogleEventEntry(e));
			foreach(var ev in eamonEvents) {
				Console.WriteLine(ev.ToString());
			}


			Console.ReadKey();
		}

	}
}
