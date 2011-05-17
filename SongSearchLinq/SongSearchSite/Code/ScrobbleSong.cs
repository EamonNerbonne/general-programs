using System;
using System.Web;
using Newtonsoft.Json;
using System.Linq;
using LastFMspider;
using SongDataLib;
using System.Collections.Generic;
using System.Diagnostics;
using LastFMspider.OldApi;
using SongSearchSite.Code.Model;

namespace SongSearchSite {
	public class ScrobbleSongHandler : IHttpHandler {
		public bool IsReusable { get { return true; } }

		class ScrobbleMessage {
			// ReSharper disable UnaccessedField.Local
#pragma warning disable 649
			public string user, pass;
			public string href, label;
			public string status;
#pragma warning restore 649
			// ReSharper restore UnaccessedField.Local
		}
		static SongRef SongGuess(string label) {
			if (label == null) return null;
			int sepIdx = label.IndexOf(" - ");
			if (sepIdx == -1) return null;
			if (label.IndexOf(" - ", sepIdx + 1) != -1) return null;//ambiguous
			return SongRef.Create(label.Substring(0, sepIdx), label.Substring(sepIdx + 3));
		}

		public void ProcessRequest(HttpContext context) {
			try {
				ScrobbleMessage command = JsonConvert.DeserializeObject<ScrobbleMessage>(context.Request["scrobbler"]);
				var fullSongData = SongDbContainer.GetSongFromFullUri(Uri.UnescapeDataString(command.href)) as SongFileData;

				SongRef songGuess = SongGuess(command.label);
				bool haveSomeGuess = fullSongData != null || songGuess != null;
				if (haveSomeGuess) {
					var submitter = ScrobbleSubmitter.DoHandshake(command.user, command.pass);
					if (submitter.IsOK) {
						var ok = fullSongData != null
							? submitter.SubmitNowPlaying(fullSongData)
							: submitter.SubmitNowPlaying(songGuess);
						context.Response.JsonOutput(new {
							status = submitter.HandshakeStatus,
							is_ok = submitter.IsOK,
							completed = ok,
						});
					} else {
						context.Response.JsonOutput(new {
							status = submitter.HandshakeStatus,
							is_ok = submitter.IsOK,
							completed = false,
						});
					}
				} else
					context.Response.JsonOutput(new {
						status = false,
						is_ok = false,
						completed = false,
						songerror = true,
					});

			} catch (Exception e) {
				context.Response.JsonOutput(new SimilarPlaylistError {
				                                     	error = e.GetType().FullName, message = e.Message, fulltrace = e.ToString()
				                                     });
			}
		}
	}
}
