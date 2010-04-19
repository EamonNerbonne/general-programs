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

namespace SongSearchSite
{
	public class ServingActivity
	{
		public sealed class ServedFileStatus : IDisposable
		{
			public readonly DateTime StartedAt;
			public readonly string remoteAddr;
			public readonly int MaxBytesPerSecond;
			public readonly string ServedFile;
			public readonly Range? ByteRange;

			//threadsafe since atomic:
			volatile public uint Duration;//int ticks of 1/10000 seconds
			volatile public uint ServedBytes;
			volatile public bool Done;

			public ServedFileStatus(string path,Range? byteRange, string remoteAddr, int maxBps) {
				this.StartedAt = DateTime.Now;
				this.remoteAddr = remoteAddr;
				this.MaxBytesPerSecond = maxBps;
				this.ServedFile = path;
				this.ByteRange = byteRange;
				ServingActivity.Enqueue(this);
			}

			public void Dispose() { Done = true; }
		}

		ServedFileStatus[] lastRequests;
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
				for (int i = 0; i < lastRequests.Length; i++) {
					int curIdx = (idx + lastRequests.Length - 1 - i) % lastRequests.Length;
					yield return lastRequests[curIdx];
				}
			}
		}

		static ServingActivity log = new ServingActivity(256);

		public static void Enqueue(ServedFileStatus file) { log.EnqueueM(file); }
		public static IEnumerable<ServedFileStatus> History { get { return log.HistoryM.Where(s => s != null); } }
	}

	public class SongServeRequestProcessor : IHttpRequestProcessor
	{
		public readonly static string prefix = "~/songs/";
		HttpRequestHelper helper;
		ISongData song = null;
		public SongServeRequestProcessor(HttpRequestHelper helper) { this.helper = helper; }

		public void ProcessingStart() { }

		public PotentialResourceInfo DetermineResource() {
			string reqPath = helper.Context.Request.AppRelativeCurrentExecutionFilePath;
			if (!reqPath.StartsWith(prefix))
				throw new Exception("Whoops, illegal request routing...  this should not be routed to this class!");

			string songNormedPath = reqPath.Substring(prefix.Length);
			song = SongContainer.GetSongByNormalizedPath(songNormedPath);
			if (song == null)
				return new ResourceError() {
					Code = 404,
					Message = "Could not find file '" + songNormedPath + "'.  Path is not indexed."
				};

			FileInfo fi = new FileInfo(song.SongPath);

			if (!fi.Exists)
				return new ResourceError() {
					Code = 404,
					Message = "Could not find file '" + song.SongPath + "' even though it's indexed as '" + songNormedPath + "'. Sorry.\n"
				};

			if (fi.Length > Int32.MaxValue)
				return new ResourceError() {
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

		public void WriteByteRange(Range range) { WriteHelper(range); }

		public void WriteEntireContent() { WriteHelper(null); }

		private void WriteHelper(Range? range) {
			//400kbps //alternative: extract from tag using taglib.  However, this isn't always the right (VBR) bitrate, and may thus fail.
			const int window = 4096;
			long fileByteCount = new FileInfo(song.SongPath).Length;
			double songSeconds = Math.Max(1.0, TagLib.File.Create(song.SongPath).Properties.Duration.TotalSeconds);
			int maxBytesPerSec = (int)(Math.Max(128 * 1024 / 8, Math.Min(fileByteCount / songSeconds, 320 * 1024 / 8)) * 1.25);

			using (var servingStatus = new ServingActivity.ServedFileStatus(song.SongPath,range, helper.Context.Request.UserHostAddress, maxBytesPerSec)) {
				const int fastStartSec = 2;
				byte[] buffer = new byte[window];
				Stopwatch timer = Stopwatch.StartNew();
				helper.Context.Response.Buffer = false;
				long end = range.HasValue ? range.Value.start + range.Value.length : fileByteCount;
				long start = range.HasValue ? range.Value.start : 0;

				bool streamEnded = false;
				using (var stream = new FileStream(song.SongPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					stream.Seek(start, SeekOrigin.Begin);
					while (!streamEnded && stream.Position < end && helper.Context.Response.IsClientConnected) {
						long maxPos = start + (long)(timer.Elapsed.TotalSeconds * maxBytesPerSec) + fastStartSec * maxBytesPerSec;
						long excessBytes = stream.Position - maxPos;
						if (excessBytes > 0)
							Thread.Sleep(TimeSpan.FromSeconds(excessBytes / (double)maxBytesPerSec));

						int bytesToRead = (int)Math.Min((long)window, end - stream.Position);
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
				}
			}
		}


		public static string guessMIME(string extension) {
			switch (extension.ToLowerInvariant()) {
				case ".mp3":
					return "audio/mpeg";
				case ".wma":
					return "audio/x-ms-wma";
				case ".wav":
					return "audio/wav";
				case ".ogg":
					return "application/ogg";
				case ".mpc":
				case ".mpp":
				case ".mp+":
					return "audio/x-musepack";
				default:
					return "application/octet-stream";//TODO fix!
			}
		}

	}

	/// <summary>
	/// Summary description for SongServeModule
	/// </summary>
	public class SongServeHandler : IHttpHandler
	{
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {

			HttpRequestHelper helper = new HttpRequestHelper(context);
			SongServeRequestProcessor processor = new SongServeRequestProcessor(helper);
			helper.Process(processor);
		}

	}
}