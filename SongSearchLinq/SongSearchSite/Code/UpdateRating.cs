using System;
using System.IO;
using System.Web;
using EmnExtensions.Text;
using Newtonsoft.Json;
using System.Linq;
using LastFMspider;
using SongDataLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SongSearchSite {
	public class UpdateRating : IHttpHandler {
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {
			context.Response.ContentType = "application/json";
			try {
				var escapedSongUri = context.Request["songuri"];
				var newRating = context.Request["rating"].ParseAsInt32();
				var path = Uri.UnescapeDataString(escapedSongUri);
				var songdata = SongDbContainer.GetSongFromFullUri(path) as SongFileData;
				songdata.rating = newRating == 0 ? null : newRating;

				bool ok = false;
				int sleep = 0;
				DateTime tryuntil = DateTime.UtcNow + TimeSpan.FromSeconds(60.0);
				while (!ok && DateTime.UtcNow < tryuntil) {
					try {
						songdata.WriteRatingToFile();
						ok = true;
					} catch (IOException e) {
						if (!e.Message.Contains("another process"))
							throw;
						else Thread.Sleep(sleep++);
					}
				}
				if (!ok)
					context.Response.Output.Write(JsonConvert.SerializeObject(new {
						error = "FileInUse",
						message = path + " is in use and not released for 60 seconds or more. Try again later.",
						fulltrace = ""
					}));
				else
					context.Response.Output.Write(JsonConvert.SerializeObject(context.User.Identity.Name));
			} catch (Exception e) {
				context.Response.Output.Write(JsonConvert.SerializeObject(new {
					error = e.GetType().FullName,
					message = e.Message,
					fulltrace = e.ToString(),
				}));
			}
		}
	}
}
