using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web;
using HttpHeaderHelper;
using SongDataLib;

namespace SongSearchSite.Code.Handlers
{
	public class SongServeRequestProcessor : IHttpRequestProcessor {
		readonly HttpRequestHelper helper;
		ISongFileData song;
		public SongServeRequestProcessor(HttpRequestHelper helper) { this.helper = helper; }

		public void ProcessingStart() { }

		public PotentialResourceInfo DetermineResource() {
			HttpContext context = helper.Context;
			song = SongDbContainer.GetSongFromFullUri(SongDbContainer.AppBaseUri(context), helper.Context.Request.AppRelativeCurrentExecutionFilePath.Substring(2));
			if (song == null)
				return new ResourceError {
				                         	Code = 404,
				                         	Message = "Could not find file '" + helper.Context.Request.AppRelativeCurrentExecutionFilePath + "'.  Path is not indexed."
				                         };

			FileInfo fi = new FileInfo(song.SongUri.LocalPath);

			if (!fi.Exists)
				return new ResourceError {
				                         	Code = 404,
				                         	Message = "Could not find file '" + song.SongUri + "' even though it's indexed as '" + helper.Context.Request.AppRelativeCurrentExecutionFilePath + "'. Sorry.\n"
				                         };

			if (fi.Length > Int32.MaxValue)
				return new ResourceError {
				                         	Code = 413,
				                         	Message = "Requested File " + song.SongUri + " is too large!"
				                         };	//should now actually support Int64's, but to be extra cautious
			//this should never happen, and never be necessary, but just to be sure...


			return
				new ResourceInfo {
				                 	TimeStamp = File.GetLastWriteTimeUtc(song.SongUri.LocalPath),
				                 	ETag = ResourceInfo.GenerateETagFrom(fi.FullName, fi.Length, fi.LastWriteTimeUtc.Ticks),
				                 	MimeType = guessMIME(Path.GetExtension(song.SongUri.LocalPath)),
				                 	ResourceLength = (ulong)fi.Length
				                 };
		}

		public DateTime? DetermineExpiryDate() {
			return DateTime.UtcNow.AddMonths(1);
		}

		public bool SupportRangeRequests {
			get { return true; }
		}

		public void WriteByteRange(Range range) { WriteHelper(range); }

		public void WriteEntireContent() { WriteHelper(null); }

		private void WriteHelper(Range? range) {

			const int window = 4096;
			long fileByteCount = new FileInfo(song.SongUri.LocalPath).Length;
			double songSeconds = Math.Max(1, song.Length);
			int maxBytesPerSecSong = (int)(Math.Max(256 * 1024 / 8, Math.Min(fileByteCount / (double)songSeconds, 320 * 1024 / 8)) * 1.25);
			int maxBytesPerSec = 300*1000;//alternative: use extract from tag using taglib.
			using (var servingStatus = new ServingActivity.ServedFileStatus(song.SongUri.LocalPath, range, helper.Context.Request.UserHostAddress, helper.Context.User.Identity.Name, maxBytesPerSec)) {
				const int fastStartSec = 10;
				byte[] buffer = new byte[window];
				Stopwatch timer = Stopwatch.StartNew();
				helper.Context.Response.Buffer = false;
				long end = range.HasValue ? range.Value.start + range.Value.length : fileByteCount;
				long start = range.HasValue ? range.Value.start : 0;

				bool streamEnded = false;
				using (var stream = new FileStream(song.SongUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					stream.Seek(start, SeekOrigin.Begin);
					while (!streamEnded && stream.Position < end && helper.Context.Response.IsClientConnected) {
						long maxPos = start + (long)(timer.Elapsed.TotalSeconds * maxBytesPerSec) + fastStartSec * maxBytesPerSecSong;
						long excessBytes = stream.Position - maxPos;
						if (excessBytes > 0 && !helper.Context.Request.IsLocal)
							Thread.Sleep(TimeSpan.FromSeconds(excessBytes / (double)maxBytesPerSec));

						int bytesToRead = (int)Math.Min(window, end - stream.Position);
						int i = 0;
						while (i < bytesToRead && helper.Context.Response.IsClientConnected) {
							int bytesRead = stream.Read(buffer, i, bytesToRead - i);
							if (bytesRead == 0) {
								//this is odd; normally it shouldn't be possible to have an "end" that's beyond the stream end, but whatever.
								streamEnded = true;
								break;
							}
							helper.Context.Response.OutputStream.Write(buffer, i, bytesRead);
							i += bytesToRead;
							servingStatus.Duration = (uint)(timer.Elapsed.TotalSeconds * 10000);
							servingStatus.ServedBytes = (uint)(stream.Position - start);
						}
					}
					helper.Context.Response.OutputStream.Flush();
					helper.Context.Response.Flush();
					servingStatus.Duration = (uint)(timer.Elapsed.TotalSeconds * 10000);
					servingStatus.ServedBytes = (uint)(stream.Position - start);
				}
			}
		}

		public const string MIME_MP3 = "audio/mpeg";
		public const string MIME_WMA = "audio/x-ms-wma";
		public const string MIME_WAV = "audio/wav";
		public const string MIME_OGG = "audio/ogg";
		public const string MIME_MPC = "audio/x-musepack";
		public const string MIME_BINARY = "application/octet-stream";


		public static string guessMIME(string extension) {
			switch (extension.ToLowerInvariant()) {
				case ".mp3": return MIME_MP3;
				case ".wma": return MIME_WMA;
				case ".wav": return MIME_WAV;
                case ".opus":
                case ".ogg": return MIME_OGG;
				case ".mpc":
				case ".mpp":
				case ".mp+": return MIME_MPC;
				default: return MIME_BINARY;
			}
		}
		public static string guessMIME(ISongFileData song) { return guessMIME(Path.GetExtension(song.SongUri.LocalPath)); }
	}
}