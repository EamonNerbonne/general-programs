using System;
using System.Web;
using EmnExtensions.Text;
using Newtonsoft.Json;
using System.Linq;
using LastFMspider;
using SongDataLib;
using System.Collections.Generic;
using System.Diagnostics;

namespace SongSearchSite {
	public class UpdateRating : IHttpHandler {
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {
			try {
				var escapedSongUri = context.Request["songuri"];
				var newRating = context.Request["rating"].ParseAsInt32();
				var path = Uri.UnescapeDataString(escapedSongUri);
				var songdata = SongDbContainer.GetSongFromFullUri(path) as SongFileData;
				songdata.rating = newRating == 0 ? null : newRating;
				songdata.WriteRatingToFile();
				context.Response.Output.Write(context.User.Identity.Name);
			} catch (Exception e) {
				context.Response.ContentType = "application/json";
				context.Response.Output.Write(
					JsonConvert.SerializeObject(
						new Dictionary<string, object> {
						{ "error",e.GetType().FullName},
						{"message",e.Message},
						{"fulltrace",e.ToString()},
					})
				);
			}
		}
	}
}
