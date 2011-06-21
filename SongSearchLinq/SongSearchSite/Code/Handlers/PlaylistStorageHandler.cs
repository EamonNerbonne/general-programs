using System;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace SongSearchSite.Code.Handlers {
	public class PlaylistStorageHandler : IHttpHandler {
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {
			context.Response.ContentType = "application/json";
			context.Response.ContentEncoding = Encoding.UTF8;
			Action<object> SetResult = obj => context.Response.Output.Write(JsonConvert.SerializeObject(obj));
			SetResult(ChooseAction(context));
		}

		static object ChooseAction(HttpContext context) {
			var db = SongDbContainer.PlaylistDb;
			switch (context.Request.AppRelativeCurrentExecutionFilePath) {
				case "~/update-playlist":
					return db.UpdatePlaylistContents(context.User.Identity.Name, context.Request["playlistTitle"], DateTime.UtcNow, context.Request["playlistContents"], long.Parse(context.Request["lastVersionId"]));
				case "~/store-playlist":
					return db.StoreNewPlaylist(context.User.Identity.Name, context.Request["playlistTitle"], DateTime.UtcNow, context.Request["playlistContents"]);
				case "~/load-playlist":
					db.UpdatePlaycount(long.Parse(context.Request["playlistID"]), DateTime.UtcNow);
					return db.LoadPlaylist(long.Parse(context.Request["playlistID"]));
				case "~/rename-playlist":
					db.RenamePlaylist(long.Parse(context.Request["playlistID"]), context.User.Identity.Name, context.Request["newName"]);
					return null;
				case "~/update-playcount":
					//					db.UpdatePlaycount(long.Parse(context.Request["playlistID"]), DateTime.UtcNow);
					//					return null;
					throw new NotImplementedException();
				case "~/list-all-playlists":
					return db.ListAllPlaylists().OrderBy(entry => entry.Username != context.User.Identity.Name).ThenByDescending(entry => (entry.CumulativePlayCount + entry.PlayCount * 2) / Math.Max(1.0, 1.0 + (DateTime.UtcNow - entry.LastPlayedTimestamp).TotalDays)).ToArray();
				case "~/list-user-playlists":
					return db.ListAllPlaylists().Where(entry => entry.Username == context.User.Identity.Name).OrderByDescending(entry => (entry.CumulativePlayCount + entry.PlayCount * 2) / Math.Max(1.0, 1.0 + (DateTime.UtcNow - entry.LastPlayedTimestamp).TotalDays)).ToArray();
				default:
					throw new ArgumentOutOfRangeException("Cannot handle: " + context.Request.AppRelativeCurrentExecutionFilePath);
			}
		}
	}
}
