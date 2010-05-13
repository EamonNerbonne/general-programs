using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.Web;
using System.Xml;
using System.IO;
using EmnExtensions;
using System.Net;
using System.Diagnostics;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider.OldApi {
	public class OldApiClient {
		static readonly TimeSpan minReqDelta = new TimeSpan(0, 0, 0, 0, 750);//no more than one request per second.
		static DateTime nextRequestWhenInternal = DateTime.Now;
		static object syncRoot = new object();

		static void waitUntilFree() {
			while (true) {
				TimeSpan sleepSpan;
				DateTime nextRequestWhen;
				lock (syncRoot)
					nextRequestWhen = nextRequestWhenInternal;
				var now = DateTime.Now;
				if (nextRequestWhen > now) {
					sleepSpan = nextRequestWhen - now;
				} else {
					lock (syncRoot) {
						nextRequestWhenInternal = nextRequestWhenInternal + minReqDelta;
						if (nextRequestWhenInternal < now) nextRequestWhenInternal = now;
					}
					break;
				}

				System.Threading.Thread.Sleep(sleepSpan);
			}
		}


		const string baseApiUrl = "http://ws.audioscrobbler.com/1.0/";

		public static Uri MakeUri(string category, string method, params string[] otherParams) {
			//double-escape data strings!!! LFM bug.
			return new Uri(baseApiUrl + category + "/" + string.Join("",
				otherParams.Select(s => Uri.EscapeDataString(Uri.EscapeDataString(s).Replace(".", "%2e")) + "/").ToArray()) + method + ".xml");
		}

		public static UriRequest MakeUriRequest(string category, string method, params string[] otherParams) {
			int retryCount = 0;
			while (true)
				try { return MakeUriRequestNoRetry(category, method, otherParams); } catch (WebException we) {
					retryCount++;
					if (retryCount > 5)
						throw;
					HttpWebResponse wr = we.Response as HttpWebResponse;
					if (wr != null && wr.StatusCode == HttpStatusCode.NotFound)
						throw;
				}
		}

		public static UriRequest MakeUriRequestNoRetry(string category, string method, params string[] otherParams) {
			waitUntilFree();
			return UriRequest.Execute(MakeUri(category, method, otherParams));
		}


		static XmlReaderSettings xmlSettings = new XmlReaderSettings { CheckCharacters = false, };
		static string ConvertControlChars(string xmlString) {//unfortunately necessary for the last.fm old-style webservices, since those contain invalid chars.
			StringBuilder newStr = new StringBuilder();
			foreach (char c in xmlString) {
				if ((c >= (char)0x20 && c < (char)0xd800) || c == (char)0xA || c == (char)0x9 || c == (char)0xD || (c >= (char)0xE000 && c <= (char)0xFFFD)) {
					newStr.Append(c);
				} else {
					newStr.Append("&#x" + Convert.ToString((int)c, 16) + ";");
				}
			}
			return newStr.ToString();
		}

		public static class Artist {
			public static ApiArtistTopTracks GetTopTracksRaw(string artist) {
				try {
					var req = MakeUriRequest("artist", "toptracks", artist);
					var xmlReader = XmlReader.Create(new StringReader(ConvertControlChars(req.ContentAsString)), xmlSettings);
					return ApiArtistTopTracks.Deserialize(xmlReader);

				} catch (WebException we) { //if for some reason the server ain't happy...
					HttpWebResponse wr = we.Response as HttpWebResponse;
					if (wr.StatusCode == HttpStatusCode.NotFound)
						return null;
					else
						throw;
				}
			}

			public static ArtistTopTracksList GetTopTracks(string artist) {
				try {
					ApiArtistTopTracks artistTopTracks = OldApiClient.Artist.GetTopTracksRaw(artist);
					return artistTopTracks == null
						? ArtistTopTracksList.CreateErrorList(artist, -1)
						: new ArtistTopTracksList {
							Artist = artistTopTracks.artist,
							LookupTimestamp = DateTime.UtcNow,
							TopTracks = artistTopTracks.track.EmptyIfNull().Select(toptrack => new ArtistTopTrack {
								Track = toptrack.name,
								Reach = toptrack.reach,
							}).ToArray(),
							StatusCode = 0,
						};
				} catch (Exception e) {
					int errCode = 1;//unknown
					if (e is WebException)
						errCode = ((int)((WebException)e).Status) + 2;//2-22
					else if (e is InvalidOperationException) //probably xml
						errCode = 32;
					else
						throw; //this is truly unexpected...
					Debug.Assert(errCode > 1 && errCode <= 32, "errcode==" + errCode + "; out of range");

					return ArtistTopTracksList.CreateErrorList(artist, errCode);
				}
			}


			public static ApiArtistSimilarArtists GetSimilarArtistsRaw(string artist) {
				try {
					var req = MakeUriRequest("artist", "similar", artist);
					var xmlReader = XmlReader.Create(new StringReader(ConvertControlChars(req.ContentAsString)), xmlSettings);
					return ApiArtistSimilarArtists.Deserialize(xmlReader);
				} catch (WebException we) { //if for some reason the server ain't happy...
					HttpWebResponse wr = we.Response as HttpWebResponse;
					if (wr != null && wr.StatusCode == HttpStatusCode.NotFound)
						return null;
					else
						throw;
				}
			}

			public static ArtistSimilarityList GetSimilarArtists(string artist) {
				ApiArtistSimilarArtists simArtists = GetSimilarArtistsRaw(artist);
				try {
					return simArtists == null
						? ArtistSimilarityList.CreateErrorList(artist, -1)
						: new ArtistSimilarityList {
							Artist = simArtists.artistName,
							LookupTimestamp = DateTime.UtcNow,
							Similar = simArtists.artist.EmptyIfNull().Select(simArtist => new SimilarArtist {
								Artist = simArtist.name,
								Rating = simArtist.match,
							}).ToArray(),
							StatusCode = 0,
						};
				} catch (Exception e) {
					int errCode = 1;//unknown
					if (e is WebException)
						errCode = ((int)((WebException)e).Status) + 2;//2-22
					else if (e is InvalidOperationException) //probably xml
						errCode = 32;
					else
						throw; //this is truly unexpected...
					Debug.Assert(errCode > 1 && errCode <= 32, "errcode==" + errCode + "; out of range");

					return ArtistSimilarityList.CreateErrorList(artist, errCode);
				}

			}
		}
		public static class Track {
			public static ApiTrackSimilarTracks GetSimilarTracksRaw(SongRef songref) {
				try {
					var req = MakeUriRequest("track", "similar", songref.Artist, songref.Title);
					var xmlReader = XmlReader.Create(new StringReader(ConvertControlChars(req.ContentAsString)), xmlSettings);
					return ApiTrackSimilarTracks.Deserialize(xmlReader);
				} catch (WebException we) { //if for some reason the server ain't happy...
					HttpWebResponse wr = we.Response as HttpWebResponse;
					if (wr != null && wr.StatusCode == HttpStatusCode.NotFound)
						return null;
					else
						throw;
				}
			}

			public static SongSimilarityList GetSimilarTracks(SongRef songref) {
				try {
					ApiTrackSimilarTracks simTracks = GetSimilarTracksRaw(songref);
					return simTracks == null
						? SongSimilarityList.CreateErrorList(songref, -1)
						: new SongSimilarityList {
							LookupTimestamp = DateTime.UtcNow,
							songref = songref,
							similartracks = simTracks.track.EmptyIfNull().Select(simTrack => new SimilarTrack {
								similarity = simTrack.match,
								similarsong = SongRef.Create(simTrack.artist.name, simTrack.name),
							}).ToArray(),
							StatusCode = 0,
						};
				} catch (Exception e) {
					int errCode = 1;//unknown; rethrow if unknown.
					if (e is WebException)
						errCode = ((int)((WebException)e).Status) + 2;//2-22
					else if (e is InvalidOperationException) //probably xml
						errCode = 32;
					else
						throw;

					Debug.Assert(errCode > 1 && errCode <= 32, "errcode==" + errCode + "; out of range");

					return SongSimilarityList.CreateErrorList(songref, errCode);
				}

			}

		}
	}
}

