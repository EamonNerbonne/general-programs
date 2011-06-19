using System;
using System.IO;
using System.Threading;
using System.Web;
using EmnExtensions.Text;
using Newtonsoft.Json;
using SongDataLib;
using SongSearchSite.Code.Model;

namespace SongSearchSite.Code.Handlers {
	public class UpdateRatingHandler : IHttpHandler {
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {
			context.Response.ContentType = "application/json";
			try {
				var escapedSongUri = context.Request["songuri"];
				var newRating = context.Request["rating"].ParseAsInt32();
				var songdata = SongDbContainer.GetSongFromFullUri(SongDbContainer.AppBaseUri(context), escapedSongUri) as SongFileData;
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
					context.Response.Output.Write(JsonConvert.SerializeObject(new SimilarPlaylistError {
						error = "FileInUse",
						message = escapedSongUri + " is in use and not released for 60 seconds or more. Try again later.",
						fulltrace = ""
					                                                                   }));
				else
					context.Response.Output.Write(JsonConvert.SerializeObject(context.User.Identity.Name));
			} catch (Exception e) {
				context.Response.Output.Write(JsonConvert.SerializeObject(new SimilarPlaylistError {
				                                                                   	error = e.GetType().FullName, message = e.Message, fulltrace = e.ToString()
				                                                                   }));
			}
		}
	}
}
