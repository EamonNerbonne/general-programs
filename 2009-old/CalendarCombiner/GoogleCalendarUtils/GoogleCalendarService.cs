using System;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using Google.GData.Calendar;
using Google.GData.Client;
using System.Collections.Generic;
using System.Text;

namespace GoogleCalendarUtils
{


	public class GoogleCalendarService
	{
		const string OwnCalendarsURL = "http://www.google.com/calendar/feeds/default/owncalendars/full";
		const string AllCalendarsURL = "http://www.google.com/calendar/feeds/default/allcalendars/full";
		const string ValidGoogleColorPurple = "#5229A3";
		string username;
		string password;
		public readonly CalendarService ServiceImplementation;
		CalendarFeed allCalendars;
		string userEmail;
		Dictionary<string, CalendarDef> calendarsByUri;
		public GoogleCalendarService(string username, string password) {
			this.password = password;
			this.username = username;
			ServiceImplementation = new CalendarService("AcloSync-AcloSync-2");
			ServiceImplementation.Credentials = new GDataCredentials(username, password);

			InitCalendars();
		}
		
		
		void InitCalendars(){
			CalendarQuery query = new CalendarQuery();
			query.Uri = new Uri(AllCalendarsURL);
			this.allCalendars = ServiceImplementation.Query(query);
			this.userEmail=allCalendars.Authors[0].Email;
			this.calendarsByUri = new Dictionary<string,CalendarDef>();
			foreach(CalendarEntry entry in allCalendars.Entries) {
				CalendarDef def = new CalendarDef(entry);
				calendarsByUri.Add(def.Uri, def);
			}
		}


		public IEnumerable<CalendarDef> GetAllCalendarEntries() {
			return calendarsByUri.Values;
		}

		public IEnumerable<CalendarDef> GetOwnCalendarEntries() {
			return calendarsByUri.Values.Where(cd => cd.IsOwned);
		}

		public IEnumerable<CalendarDef> GetOtherCalendarEntries() {
			return calendarsByUri.Values.Where(cd => !cd.IsOwned);
		}

		public CalendarDef GetCalendarByUri(string Uri) {
			return calendarsByUri[Uri];
		}
		const int retrievenum = 1000;
		public IEnumerable<EventEntry> GetCalendarEvents(CalendarDef calDef, DateTime start, DateTime end) {
			EventQuery myEventQuery =
				new EventQuery(calDef.Uri) {
					StartTime = start,
					EndTime = end,
					NumberToRetrieve = retrievenum
				};
			return TmpConv(ServiceImplementation.Query(myEventQuery).Entries);
		}

		private IEnumerable<EventEntry> TmpConv(AtomEntryCollection coll) {
#if DEBUG
			string g = null;
			if(coll.Count== retrievenum)
				g = "g";
#endif
			List<EventEntry> retval=new List<EventEntry>();
			foreach(EventEntry ev in coll) {
				retval.Add(ev);
			}
			return retval;
		}

		public string FillCalendar(string calendarTitle, bool deleteOldCalendar, IEnumerable<Event> withEvents) {
			StringBuilder errorMessages = new StringBuilder(); 
			//TODO: ,maybe actually delete old calendar??????

			CalendarEntry calendar = new CalendarEntry();
			calendar.Title.Text = calendarTitle;
			calendar.Summary.Text = "This is when get you move your bottom.";
			calendar.TimeZone = "Europe/Amsterdam";
			calendar.Hidden = false;
			calendar.Color = ValidGoogleColorPurple;

			Uri postUri = new Uri(OwnCalendarsURL);
			CalendarEntry createdCalendar = (CalendarEntry)ServiceImplementation.Insert(postUri, calendar);

			EventQuery myEventQuery = new EventQuery(createdCalendar.Links[0].AbsoluteUri);
			EventFeed myEventFeed = ServiceImplementation.Query(myEventQuery);

			Queue<Event> eventQueue = new Queue<Event>(withEvents);

			while(eventQueue.Count > 0) {
				AtomFeed batchFeed = new AtomFeed(myEventFeed);
				
				for(int i=0;i<50&&eventQueue.Count>0;i++) {
					Event next = eventQueue.Dequeue();
					EventEntry gEvent = next.AsGoogleEventEntry();
					gEvent.BatchData = new GDataBatchEntryData(GDataBatchOperationType.insert);
					batchFeed.Entries.Add(gEvent);
				}
				Console.WriteLine("Submitting...");
				EventFeed batchResult22 = (EventFeed)ServiceImplementation.Batch(batchFeed, new Uri(myEventFeed.Batch));
				foreach(var str in from ev in batchResult22.Entries.Cast<EventEntry>()
										 let code = ev.BatchData.Status.Code
										 where code != 200 && code != 201
										 select Event.CreateEventsFromGoogleEventEntry(ev).ToString())
					errorMessages.AppendLine(str);
			}
			return errorMessages.ToString();
		}
	}
}
