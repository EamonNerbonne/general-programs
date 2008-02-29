using System;
using System.IO;
using System.Web;
using System.Web.SessionState;
using HttpHeaderHelper;
using SongDataLib;

namespace SongSearchSite
{
	public class SongServeRequestProcessor : IHttpRequestProcessor
	{
		public readonly static string prefix = "~/songs/";
		HttpRequestHelper helper;
		ISongData song = null;
		public SongServeRequestProcessor(HttpRequestHelper helper) { this.helper = helper; }

		public void ProcessingStart() {
		}

		public PotentialResourceInfo DetermineResource() {
			string reqPath = helper.Context.Request.AppRelativeCurrentExecutionFilePath;
			if(!reqPath.StartsWith(prefix)) throw new Exception("Whoops, illegal request routing...  this should not be routed to this class!");

			string songNormedPath = reqPath.Substring(prefix.Length);
			song = SongContainer.GetSongByNormalizedPath(songNormedPath);
			if(song == null) return new ResourceError() {
				Code = 404,
				Message = "Could not find file '" + songNormedPath + "'.  Path is not indexed."
			};

			FileInfo fi = new FileInfo(song.SongPath);

			if(!fi.Exists) return new ResourceError() {
				Code = 404,
				Message = "Could not find file '" + song.SongPath + "' even though it's indexed as '" + songNormedPath + "'. Sorry.\n"
			};

			if(fi.Length > Int32.MaxValue) return new ResourceError() {
				Code = 413,
				Message = "Requested File " + song.SongPath + " is too large!"
			};	//should now actually support Int64's, but to be extra cautious
			//this should never happen, and never be necessary, but just to be sure...


			return
				new ResourceInfo {
					TimeStamp = File.GetLastWriteTimeUtc(song.SongPath),
					ETag = ResourceInfo.GenerateETagFrom(fi.FullName, fi.Length, fi.LastWriteTimeUtc.Ticks, song.ConvertToXml(null)),
					MimeType = guessMIME(Path.GetExtension(song.SongPath)),
					ResourceLength = (ulong)fi.Length
				};
		}

		public DateTime? DetermineExpiryDate() {
			return DateTime.Now.AddMonths(1);
		}

		public bool SupportRangeRequests {
			get { return true; }
		}

		public void WriteByteRange(Range range) {
			helper.Context.Response.Buffer = false;
			helper.Context.Response.TransmitFile(song.SongPath, range.start, range.length);
		}



		public void WriteEntireContent() {
			helper.Context.Response.Buffer = false;
			helper.Context.Response.TransmitFile(song.SongPath);
		}
		public static string guessMIME(string extension) {
			switch(extension.ToLowerInvariant()) {
				case ".mp3": return "audio/mpeg";
				case ".wma": return "audio/x-ms-wma";
				case ".wav": return "audio/wav";
				case ".ogg": return "application/ogg";
				case ".mpc":
				case ".mpp":
				case ".mp+": return "audio/x-musepack";
				default:
					return "application/octet-stream";//TODO fix!
			}
		}

	}

	/// <summary>
	/// Summary description for SongServeModule
	/// </summary>
	public class SongServeHandler : IHttpHandler, IRequiresSessionState
	{
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {

			HttpRequestHelper helper = new HttpRequestHelper(context);
			SongServeRequestProcessor processor = new SongServeRequestProcessor(helper);
			helper.Process(processor);
		}

	}
}