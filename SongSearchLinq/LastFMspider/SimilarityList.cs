using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
	public struct SimilarityTo<T> {
		public readonly T OtherId;
		public readonly float Similarity;
		public SimilarityTo(T id, float sim) { this.OtherId = id; this.Similarity = sim; }
	}

	internal struct SimilarityList<T, F>
		where T : struct,IId
		where F : struct, IIdFactory<T> {
		internal readonly byte[] encodedSims;
		public bool HasValue { get { return encodedSims != null; } }
		public IEnumerable<SimilarityTo<T>> Similarities { get { return DecodeRatingBlob(encodedSims); } }
		public SimilarityList(byte[] encodedSims) { this.encodedSims = encodedSims; }
		public SimilarityList(IEnumerable<SimilarityTo<T>> sims) { encodedSims = EncodeRatingBlob(sims); }

		static IEnumerable<SimilarityTo<T>> DecodeRatingBlob(byte[] arr) {
			if (arr == null) yield break;

			F factory = default(F);
			using (var ms = new MemoryStream(arr))
			using (var br = new BinaryReader(ms))
				while (ms.Position < ms.Length) {
					var id = br.ReadUInt32();
					var sim = br.ReadSingle();
					yield return new SimilarityTo<T>(factory.CastToId(id), sim);
				}
		}

		static byte[] EncodeRatingBlob(IEnumerable<SimilarityTo<T>> ratings) {
			using (var ms = new MemoryStream()) {
				using (var bw = new BinaryWriter(ms))
					foreach (var entry in ratings) {
						bw.Write(entry.OtherId.Id);
						bw.Write(entry.Similarity);
					}
				return ms.ToArray();
			}
		}
	}
}
