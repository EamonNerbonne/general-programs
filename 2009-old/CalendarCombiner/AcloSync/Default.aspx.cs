using System;
using System.Configuration;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Xml.Linq;
using System.Collections.Generic;
using GoogleCalendarUtils;

namespace AcloSync
{
	public partial class _Default : System.Web.UI.Page
	{
		GoogleCalendarService GoogleService {
			get {
				return Session["GoogleCalendarService"] as GoogleCalendarService;
			}
			set {
				Session["GoogleCalendarService"] = value;
			}
		}
		AcloCalendarSource AcloCalendarSource {
			get {
				AcloCalendarSource src = Context.Cache["AcloCalendarSource"] as AcloCalendarSource;
				if(src == null)
					Context.Cache["AcloCalendarSource"] = src = new AcloCalendarSource(Context);
				return src;
			}
		}

		void FillAcloOptions() {
			foreach(string activity in AcloCalendarSource.categories)
				AcloActivitiesCheckBoxList.Items.Add(activity);
		}


		protected override void OnInit(EventArgs e) {
			base.OnInit(e);
			FillAcloOptions();
			FillGoogleOptions();
		}

		protected void Page_Load(object sender, EventArgs e) {
		}

		bool googleOptionsSet = false;
		void FillGoogleOptions() {
			if(GoogleService == null|| googleOptionsSet) return;
			
			googleOptionsSet=true;
			foreach(CalendarDef def in GoogleService.GetAllCalendarEntries()) {
				ListItem li = new ListItem(def.Title, def.Uri);
				li.Attributes.CssStyle.Add(HtmlTextWriterStyle.BackgroundColor, def.entry.Color);
				li.Attributes.CssStyle.Add(HtmlTextWriterStyle.Color, "white");
				li.Attributes.CssStyle.Add(HtmlTextWriterStyle.Padding, "0.1em");
				li.Attributes.CssStyle.Add(HtmlTextWriterStyle.Width, "100%");
				li.Attributes.CssStyle.Add(HtmlTextWriterStyle.Display, "block");
				CalenderCheckBoxList.Items.Add(li);
			}
		}

		protected void SendGoogleAuthButton_Click1(object sender, EventArgs e) {
			GoogleService = new GoogleCalendarService(googleUsername.Text, googlePassword.Text);
			FillGoogleOptions();
			for(int i = 0; i < CalenderCheckBoxList.Items.Count; i++) { //now we initialize the calendar selection to contain those calendars you can edit.
				ListItem item =CalenderCheckBoxList.Items[i];
				item.Selected = GoogleService.GetCalendarByUri(item.Value).IsOwned && item.Text!= "AcloSync";
			}
		}

		static DateTime Max(DateTime a, DateTime b) {
			return a < b ? b : a;
		}
		static DateTime Min(DateTime a, DateTime b) {
			return a >= b ? b : a;
		}

		static readonly TimeSpan marginTime = TimeSpan.FromMinutes(0);
		protected void GrilledRoosterButton_Click(object sender, EventArgs e) {
			//Determine the events in google calendar
			var googleEvents =
							//take all checked "calendars"
				from li in CalenderCheckBoxList.Items.Cast<ListItem>()
				where li.Selected
							//find the appropriate google calendar
				let calendarDefinition = GoogleService.GetCalendarByUri(li.Value)			
							//and query them for events within the aclo-calendar's timespan
				from ev in GoogleService.GetCalendarEvents(
										calendarDefinition,
										AcloCalendarSource.start,
										AcloCalendarSource.end)
				select ev;
			
			//…but not those which are "all-day events" if those weren't explicitely enabled…
			if(!ConsiderAllDayEventsCheckBox.Checked)
				googleEvents = googleEvents.Where(ev => !ev.Times[0].AllDay);

			//Convert google's events into our events - specifically, if the event is recurring,
			//simply split it into multiple-non-recurring instances.
			var ourEvents =
				from googleEvent in googleEvents
				from ourEvent in Event.CreateEventsFromGoogleEventEntry(googleEvent)
				select ourEvent;

			ourEvents = ourEvents.ToArray();//run query and store in array.

			//determine the class of all categories of viable ACLO-activities
			var categories = new HashSet<string>(
				from li in AcloActivitiesCheckBoxList.Items.Cast<ListItem>()
				where li.Selected
				select li.Text
				);

			//Finally, only consider those aclo events which don't overlap:  specifically, where all
			//gcal events are non conflicting. Two events don't conflict when the earlier ending
			//(plus marginTime) is before the later start time.
			var nonOverlappingAclo = (
				from aE in AcloCalendarSource.AcloEvents
				where categories.Contains(aE.Title)
				where ourEvents.All(gE =>
				Min(aE.EndTime, gE.EndTime) + marginTime <=
				Max(aE.StartTime, gE.StartTime))
				select aE
				).ToArray();//store the query result in an array.

			//now take all those non-overlapping aclo events, and place them in a new
			//google calendar!
			GoogleService.FillCalendar(
				CalendarNameTextBox.Text,
				DeleteFirstCheckBox.Checked,
				nonOverlappingAclo);
			//TestVerifier((Google.GData.Calendar.EventEntry[])googleEvents, (Event[])ourEvents, (Event[])AcloCalendarSource.AcloEvents, (Event[])nonOverlappingAclo);
		}

		void TestVerifier(Google.GData.Calendar.EventEntry[] a, Event[] gcal, Event[] aclo, Event[] nonoverlapaclo) {
			gcal = Sortit(gcal); aclo = Sortit(aclo); nonoverlapaclo = Sortit(nonoverlapaclo);

			DateTime oct23h18 = new DateTime(2007, 10, 23,18,0,0);
			DateTime oct24 = new DateTime(2007, 10, 24);

			Event[] gcal2 = GetOn(gcal, oct23h18, oct24).ToArray();
			Event[] nonoverlapaclo2 = GetOn(nonoverlapaclo, oct23h18, oct24).ToArray();



		}

		string makeLegible(params Google.GData.Calendar.EventEntry[] a) {
			return string.Join("\n",(from x in a select
					  string.Join(", ",(from y in Event.CreateEventsFromGoogleEventEntry(x) select y.ToString()).ToArray())).ToArray());

		}

		Event[] Sortit(Event[] gcal) {
			return gcal.OrderBy(a => a.StartTime).ToArray();
		}


		IEnumerable<Event> GetOn(IEnumerable<Event> es, DateTime start, DateTime end) {
			foreach(Event e in es)
				if(e.StartTime >= start && e.EndTime <= end)
					yield return e;
		}

		IEnumerable<Event> Containing(IEnumerable<Event> es, string shouldContain) {
			foreach(Event e in es)
				if(e.Title.Contains(shouldContain) || e.Text.Contains(shouldContain))
					yield return e;
		}

	}
}
