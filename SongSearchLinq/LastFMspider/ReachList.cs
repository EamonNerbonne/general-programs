using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider.LastFMSQLiteBackend;
using System.IO;

namespace LastFMspider {
	public struct HasReach<T> {
		public readonly T ForId;
		public readonly long Reach;
		public HasReach(T id, long reach) { this.ForId = id; this.Reach = reach; }
	}

	internal struct ReachList<T, F>
		where T : struct, IId
		where F : struct, IIdFactory<T> {
		internal readonly byte[] encodedSims;
		public bool HasValue { get { return encodedSims != null; } }
		public IEnumerable<HasReach<T>> Rankings { get { return DecodeRatingBlob(encodedSims); } }
		public ReachList(byte[] encodedSims) { this.encodedSims = encodedSims; }
		public ReachList(IEnumerable<HasReach<T>> sims) { encodedSims = EncodeRatingBlob(sims); }

		static IEnumerable<HasReach<T>> DecodeRatingBlob(byte[] arr) {
			F factory = default(F);

			using (var ms = new MemoryStream(arr))
			using (var br = new BinaryReader(ms))
				while (ms.Position < ms.Length) {
					var id = br.ReadUInt32();
					var reach = br.ReadInt64();
					yield return new HasReach<T>(factory.Cast(id), reach);
				}
		}

		static byte[] EncodeRatingBlob(IEnumerable<HasReach<T>> rankings) {
			using (var ms = new MemoryStream()) {
				using (var bw = new BinaryWriter(ms))
					foreach (var entry in rankings) {
						bw.Write(entry.ForId.Id);
						bw.Write(entry.Reach);
					}
				return ms.ToArray();
			}
		}
	}
}
