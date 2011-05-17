using System;
//using System.Web.SessionState;
using System.Linq;
using HttpHeaderHelper;
using System.Collections.Generic;

namespace SongSearchSite {
	public class ServingActivity {
		public sealed class ServedFileStatus : IDisposable {
			public readonly DateTime StartedAtLocalTime;
			public readonly string RemoteAddr;
			public readonly string User;
			public readonly int MaxBytesPerSecond;
			public readonly string ServedFile;
			public readonly Range? ByteRange;

			//threadsafe since atomic:
			volatile public uint Duration;//int ticks of 1/10000 seconds
			volatile public uint ServedBytes;
			volatile public bool Done;

			public ServedFileStatus(string path, Range? byteRange, string remoteAddr, string username, int maxBps) {
				StartedAtLocalTime = DateTime.Now;
				RemoteAddr = remoteAddr;
				MaxBytesPerSecond = maxBps;
				ServedFile = path;
				User = username;
				ByteRange = byteRange;
				Enqueue(this);
			}

			public void Dispose() { Done = true; }
		}

		readonly ServedFileStatus[] lastRequests;
		int nextWriteIdx;
		ServingActivity(int history) {
			lastRequests = new ServedFileStatus[history];
		}

		void EnqueueM(ServedFileStatus file) {
			lock (this) {
				lastRequests[nextWriteIdx] = file;
				nextWriteIdx = (nextWriteIdx + 1) % lastRequests.Length;
			}
		}

		IEnumerable<ServedFileStatus> HistoryM {
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
}