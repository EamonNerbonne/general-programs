using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using EmnExtensions.Web;
using System.Net;
using SongDataLib;

namespace LastFMspider.OldApi {
	public class ScrobbleSubmitter {

		//see http://www.last.fm/api/submissions#1.2
		const string scrobbleBaseUri = @"http://post.audioscrobbler.com:80/";
		const string protocolVer = "1.2.1";//p
		const string clientName = "tst";//c
		const string clientVer = "1.0";//v
		const string userAgent = "songsearchplayer 0.1";

		static KeyValuePair<string, string> mkArg(string key, string value) { return new KeyValuePair<string, string>(key, value); }

		static string QuerySegment(IEnumerable<KeyValuePair<string, string>> args) {
			StringBuilder sb = new StringBuilder();
			foreach (var arg in args) {
				sb.Append(Uri.EscapeDataString(arg.Key));
				sb.Append('=');
				sb.Append(Uri.EscapeDataString(arg.Value));
				sb.Append('&');
			}
			if (sb.Length > 0) sb.Length = sb.Length - 1;
			return sb.ToString();
		}
		static string QuerySegment(params KeyValuePair<string, string>[] args) {
			return QuerySegment(args.AsEnumerable());
		}
		static readonly DateTime unixBaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		static long toUnix(DateTime dt) { return (long)(dt.ToUniversalTime() - unixBaseTime).TotalSeconds; }
		static DateTime fromUnix(long timestamp) { return unixBaseTime + TimeSpan.FromSeconds(timestamp); }

		static readonly Encoding lfmEncoding = Encoding.UTF8;
		static string HexEncode(byte[] bytes) { //lowercase!
			StringBuilder retval = new StringBuilder();
			foreach (byte b in bytes)
				retval.Append(Convert.ToString(b, 16));
			return retval.ToString();
		}

		static string md5(string inp) {
			MD5 md5Computer = MD5.Create();
			return HexEncode(md5Computer.ComputeHash(lfmEncoding.GetBytes(inp)));
		}

		static Uri MakeHandshakeUri(string lastfmUser, string lastfmPass) {
			long timestampNow = toUnix(DateTime.UtcNow);
			string authtoken = md5(md5(lastfmPass) + timestampNow);

			return new Uri(scrobbleBaseUri + "?" + QuerySegment(new[]{
				mkArg("hs","true"),
				mkArg("p",protocolVer),
				mkArg("c",clientName),
				mkArg("v",clientVer),
				mkArg("u",lastfmUser),
				mkArg("t",timestampNow.ToString()),
				mkArg("a",authtoken),
			}));
		}

		public static ScrobbleSubmitter DoHandshake(string lastfmUser, string lastfmPass) {
			Uri handshakeUri = MakeHandshakeUri(lastfmUser, lastfmPass);
			try {
				var handshakeResponse = UriRequest.Execute(handshakeUri, UserAgent: userAgent);
				if (handshakeResponse.StatusCode != HttpStatusCode.OK)
					return new ScrobbleSubmitter(Status.HardFailure, "Server response: " + handshakeResponse.StatusCode + "\nCannot recover automatically.");
				else return ParseResponse(handshakeResponse.ContentAsString);
			} catch (WebException we) {
				return new ScrobbleSubmitter(Status.HardFailure, we.ToString());
			}
		}

		private static ScrobbleSubmitter ParseResponse(string serverResponse) {
			string[] lines = serverResponse.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length == 0) return new ScrobbleSubmitter(Status.HardFailure, "Empty server response");
			string statusline = lines[0];
			if (statusline == "OK" && lines.Length == 4) {
				return new ScrobbleSubmitter(lines[1], lines[2], lines[3]);
			} else if (statusline == "BANNED") {
				return new ScrobbleSubmitter(Status.Banned, null);
			} else if (statusline == "BADAUTH") {
				return new ScrobbleSubmitter(Status.Badauth, null);
			} else if (statusline == "BADTIME") {
				return new ScrobbleSubmitter(Status.Badtime, serverResponse);
			} else if (statusline.StartsWith("FAILED")) {
				return new ScrobbleSubmitter(Status.Failed, serverResponse);
			} else {
				return new ScrobbleSubmitter(Status.HardFailure, "Invalid server response:\n" + serverResponse);
			}
		}

		public enum Status {
			Undefined, HardFailure, OK, Banned, Badauth, Badtime, Failed, Badsession
		}
		readonly string sessionId, nowPlayingUri, submitUri;
		string failMessage;
		Status status;

		ScrobbleSubmitter(string sessionId, string nowPlayingUri, string submitUri) {
			status = Status.OK;
			this.nowPlayingUri = nowPlayingUri;
			this.submitUri = submitUri;
			this.sessionId = sessionId;
			failMessage = null;
		}
		private ScrobbleSubmitter(Status errorCode, string errorMessage) {
			if (errorCode == Status.OK)
				throw new ArgumentException("Cannot have OK status in error constructor");

			status = errorCode;
			failMessage = errorMessage;
		}

		public bool IsOK { get { return status == Status.OK; } }
		public bool IsAuthFailure { get { return status == Status.Badauth; } }
		public bool IsBadSession { get { return status == Status.Badsession; } }
		public string FailureMessage { get { return status == Status.OK ? null : (failMessage ?? status.ToString()); } }
		public Status HandshakeStatus { get { return status; } }

		public bool SubmitNowPlaying(SongFileData songdata) {
			return SubmitNowPlaying(songdata.artist, songdata.title, songdata.album, songdata.length, songdata.track);
		}
		public bool SubmitNowPlaying(SongRef songdata) {
			return SubmitNowPlaying(songdata.Artist, songdata.Title);
		}

		bool SubmitNowPlaying(string artist,string title,string album=null, int length=0,int track=0) {
			if (!IsOK)
				throw new InvalidOperationException("Can't submit now playing, status == " + status);
			if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title))
				throw new ArgumentException("Can't submit; incomplete tags!");

			try {
				var resp = UriRequest.Execute(new Uri(nowPlayingUri), UserAgent: userAgent, PostData: QuerySegment(new[]{
					mkArg("s",sessionId),
					mkArg("a",artist),
					mkArg("t",title),
					mkArg("b",album??""),
					mkArg("l",length==0?"":length.ToString()),
					mkArg("n",track==0?"":track.ToString()),
					mkArg("m",""),//TODO
				}));
				if (resp.ContentAsString != null && resp.ContentAsString.StartsWith("OK"))
					return true;
				else {
					status = Status.Badsession;
					return false;
				}
			} catch (WebException we) {
				status = Status.HardFailure;
				failMessage = we.ToString();
				return false;
			}
		}
		//public bool SubmitScrobble(SongData songdata,) {
		//    if (!IsOK)
		//        throw new InvalidOperationException("Can't submit now playing, status == " + status);
		//    if (string.IsNullOrEmpty(songdata.artist) || string.IsNullOrEmpty(songdata.title))
		//        throw new ArgumentException("Can't submit; incomplete tags!");

		//    try {
		//        var resp = UriRequest.Execute(new Uri(submitUri), UserAgent: userAgent, PostData: QuerySegment(new[]{
		//            mkArg("s",sessionId),
		//            mkArg("a[0]",songdata.artist),
		//            mkArg("t[0]",songdata.title),
		//            mkArg("b",songdata.album??""),
		//            mkArg("l",songdata.Length==0?"":songdata.Length.ToString()),
		//            mkArg("n",songdata.track==0?"":songdata.track.ToString()),
		//            mkArg("m",""),//TODO
		//        }));
		//        if (resp.ContentAsString != null && resp.ContentAsString.StartsWith("OK"))
		//            return true;
		//        else {
		//            status = Status.Badsession;
		//            return false;
		//        }
		//    } catch (WebException we) {
		//        status = Status.HardFailure;
		//        failMessage = we.ToString();
		//        return false;
		//    }
		//}


	}
}
