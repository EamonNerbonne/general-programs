using System;
using System.IO;
using System.Web;
//using System.Web.SessionState;
using System.Linq;
using HttpHeaderHelper;
using SongDataLib;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace SongSearchSite {
	public class ServingActivity {
		public sealed class ServedFileStatus : IDisposable {
			public readonly DateTime StartedAtLocalTime;
			public readonly string RemoteAddr;
			public readonly int MaxBytesPerSecond;
			public readonly string ServedFile;
			public readonly Range? ByteRange;

			//threadsafe since atomic:
			volatile public uint Duration;//int ticks of 1/10000 seconds
			volatile public uint ServedBytes;
			volatile public bool Done;

			public ServedFileStatus(string path, Range? byteRange, string remoteAddr, int maxBps) {
				StartedAtLocalTime = DateTime.Now;
				RemoteAddr = remoteAddr;
				MaxBytesPerSecond = maxBps;
				ServedFile = path;
				ByteRange = byteRange;
				Enqueue(this);
			}

			public void Dispose() { Done = true; }
		}

		readonly ServedFileStatus[] lastRequests;
		int nextWriteIdx;
		private ServingActivity(int history) {
			lastRequests = new ServedFileStatus[history];
		}

		private void EnqueueM(ServedFileStatus file) {
			lock (this) {
				lastRequests[nextWriteIdx] = file;
				nextWriteIdx = (nextWriteIdx + 1) % lastRequests.Length;
			}
		}

		private IEnumerable<ServedFileStatus> HistoryM {
			get {
				int idx;
				lock (this) idx = nextWriteIdx;
				return lastRequests.Select((t, i) => (idx + lastRequests.Length - 1 - i) % lastRequests.Length)
					.Select(curIdx => lastRequests[curIdx]);
			}
		}

		static readonly ServingActivity log = new ServingActivity(256);

		static void Enqueue(ServedFileStatus file) { log.EnqueueM(file); }
		public static IEnumerable<ServedFileStatus> History { get { return log.HistoryM.Where(s => s != null); } }
	}

	public class SongServeRequestProcessor : IHttpRequestProcessor {
		readonly HttpRequestHelper helper;
		ISongFileData song;
		public SongServeRequestProcessor(HttpRequestHelper helper) { this.helper = helper; }

		public void ProcessingStart() { }

		public PotentialResourceInfo DetermineResource() {
			song = SongDbContainer.GetSongFromFullUri(helper.Context.Request.AppRelativeCurrentExecutionFilePath.Substring(2));
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
			//400kbps //alternative: extract from tag using taglib.  However, this isn't always the right (VBR) bitrate, and may thus fail.
			const int window = 4096;
			long fileByteCount = new FileInfo(song.SongUri.LocalPath).Length;
			double songSeconds = Math.Max(1.0, TagLib.File.Create(song.SongUri.LocalPath).Properties.Duration.TotalSeconds);
			int maxBytesPerSec = (int)(Math.Max(128 * 1024 / 8, Math.Min(fileByteCount / songSeconds, 320 * 1024 / 8)) * 1.25);

			using (var servingStatus = new ServingActivity.ServedFileStatus(song.SongUri.LocalPath, range, helper.Context.Request.UserHostAddress, maxBytesPerSec)) {
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
						long maxPos = start + (long)(timer.Elapsed.TotalSeconds * maxBytesPerSec) + fastStartSec * maxBytesPerSec;
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
				case ".ogg": return MIME_OGG;
				case ".mpc":
				case ".mpp":
				case ".mp+": return MIME_MPC;
				default: return MIME_BINARY;
			}
		}
		public static string guessMIME(ISongFileData song) { return guessMIME(Path.GetExtension(song.SongUri.LocalPath)); }
	}

	/// <summary>
	/// Summary description for SongServeModule
	/// </summary>
	public class SongServeHandler : IHttpHandler {
		public bool IsReusable { get { return true; } }
		public void ProcessRequest(HttpContext context) {
			HttpRequestHelper helper = new HttpRequestHelper(context);
			SongServeRequestProcessor processor = new SongServeRequestProcessor(helper);
			helper.Process(processor);
		}

	}
}