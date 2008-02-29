using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Google.GData.Calendar;
using Google.GData.Extensions;

namespace GoogleCalendarUtils
{
	public class Event
		{
			public string Where { get; set; }
			public string Title { get; set; }
			public string Text { get; set; }
			public DateTime StartTime { get; set; }
			public DateTime EndTime { get; set; }
			public EventEntry AsGoogleEventEntry() {
				EventEntry retval = new EventEntry(Title, Text, Where);
				retval.Times.Add(new When(StartTime, EndTime));
				return retval;
			}
			public static IEnumerable<Event> CreateEventsFromGoogleEventEntry(EventEntry gEvent) {
				foreach(When when in gEvent.Times)
					yield return new Event {
						StartTime = when.StartTime,
						EndTime = when.EndTime,
						Where = gEvent.Locations[0].Label,
						Title = gEvent.Title.Text,
						Text = gEvent.Content.Content
					};
			}
			public static Event CreateFromXElement(XElement xEl) {
				return new Event {
					Where = (string)xEl.Element("where"),
					Text = (string)xEl.Element("description"),
					Title = (string)xEl.Element("title"),
					EndTime = (DateTime)xEl.Element("end"),
					StartTime = (DateTime)xEl.Element("start")
				};
			}

			public override string ToString() {
				return
					"(Event; Where:" + Where + "; Title:" + Title + "; Start:" + StartTime + "; End:" + EndTime + ")";
			}
		}
}
