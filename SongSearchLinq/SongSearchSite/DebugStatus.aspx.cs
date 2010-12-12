using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Xml.Linq;

namespace SongSearchSite
{
	public partial class DebugStatus : Page
	{
		protected void Page_Load(object sender, EventArgs e) { 
		}
		protected void WriteRows() {
			var statusTable=
			new XElement("table",

				new XElement("tr", new XAttribute("style","background:#ccc"),
					new XElement("td"),
					new XElement("td", new XElement("b","File")," [byte-range]"),
					new XElement("td", "IP"),
					new XElement("td", "Started" ),
					new XElement("td", "Target bitrate"),
					new XElement("td", "Served KB"),
					new XElement("td", "Duration (s)"),
					new XElement("td", "Actual bitrate")
					),
				from sAct in ServingActivity.History
				select new XElement("tr",
					new XElement("td", sAct.Done ? "" : "*"),
					new XElement("td",
						Path.GetDirectoryName(sAct.ServedFile)+"\\",
						new XElement("b",Path.GetFileName(sAct.ServedFile)),
						sAct.ByteRange.HasValue ? " [" + sAct.ByteRange.Value + "]" : null
						),
					new XElement("td", sAct.RemoteAddr),
					new XElement("td", sAct.StartedAtLocalTime.ToLocalTime().Date == DateTime.Now.Date? sAct.StartedAtLocalTime.ToString ("T") : sAct.StartedAtLocalTime.ToShortDateString() ),
					new XElement("td", sAct.MaxBytesPerSecond * 8 / 1024),
					new XElement("td", sAct.ServedBytes/1024),
					new XElement("td", (sAct.Duration / 10000.0).ToString("N1") ),

					new XElement("td", sAct.Duration == 0 ? 0 : (int)(sAct.ServedBytes / (sAct.Duration / 10000.0) * 8 / 1024))
					)
					);
			Response.Write(statusTable.ToString(SaveOptions.DisableFormatting));
			Response.Write(new XElement("div",
				"Memory Usage: " + GC.GetTotalMemory(Request["GC"] == "true") / (double)(1 << 20) + "MB").ToString(SaveOptions.DisableFormatting));
		}
	}
}