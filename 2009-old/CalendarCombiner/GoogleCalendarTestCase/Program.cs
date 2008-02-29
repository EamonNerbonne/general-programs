using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;
using GoogleCalendarUtils;

namespace GoogleCalendarTestCase
{
	class Program
	{
		const string OwnCalendarsURL = "http://www.google.com/calendar/feeds/default/owncalendars/full";
		const string AllCalendarsURL = "http://www.google.com/calendar/feeds/default/allcalendars/full";

		static void Main(string[] args) {
			GoogleCalendarService service = new GoogleCalendarService(args[0], args[1]);

			//CalendarDef calendar = service.GetOwnCalendarEntries().Where(cd => cd.Title == "Eamon Nerbonne").First();


			AddEntries(service.ServiceImplementation, 
				"http://www.google.com/calendar/feeds/jo@gmail.com/private/full"//calendar.Uri
				);
			/*
			CalendarQuery query = new CalendarQuery();
			query.Uri = new Uri(AllCalendarsURL);

			CalendarFeed allCalendars = service.Query(query);
	
			string userEmail = allCalendars.Authors[0].Email;
			foreach(CalendarEntry entry in allCalendars.Entries) {
			}*/
		}
		static void AddEntries(CalendarService service,string feedUri) {


			EventQuery query = new EventQuery(feedUri);
			EventFeed feed = service.Query(query);

			// create a batch entry to insert a new event.
			EventEntry toCreate = new EventEntry();
			toCreate.Title.Text = "new event";
			toCreate.Content.Content = "test";

			When eventTime = new When();
			eventTime.StartTime = DateTime.Now;
			eventTime.EndTime = eventTime.StartTime.AddHours(1);
			toCreate.Times.Add(eventTime);

			toCreate.BatchData = new GDataBatchEntryData();
			toCreate.BatchData.Id = "d";
			toCreate.BatchData.Type = GDataBatchOperationType.insert;

			// add the entries to new feed.
			AtomFeed batchFeed = new AtomFeed(feed);
			batchFeed.Entries.Add(toCreate);

			EventFeed batchResultFeed =
(EventFeed)service.Batch(batchFeed, new Uri(feed.Batch));
		}

	}
}
