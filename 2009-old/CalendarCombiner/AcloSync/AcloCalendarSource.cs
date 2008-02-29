using System;
using System.Configuration;
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
	public class AcloCalendarSource
	{
		XDocument[] acloEventsDocs;
   	public Event[] AcloEvents;
		public DateTime start, end;
		public string[] categories;

		public AcloCalendarSource(HttpContext context) {
			acloEventsDocs = new XDocument[] {
			 XDocument.Load(context.Server.MapPath("events-1.xml")),
			 XDocument.Load(context.Server.MapPath("events-2.xml"))
			};
			var events =
				from xEl in acloEventsDocs.SelectMany(d=>d.Root.Elements("event"))
				let ev = Event.CreateFromXElement(xEl)
				//				orderby ev.StartTime
				select ev;
			AcloEvents = events.ToArray();
			start = AcloEvents.Min(ev => ev.StartTime);
			end = AcloEvents.Max(ev => ev.EndTime);
			categories = (
				 from ev in events
				 group ev by ev.Title into g
				 orderby g.Count() descending
				 select g.Key
				 ).ToArray();
		}
	}
}
