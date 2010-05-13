using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using EmnExtensions.Text;
using System.Runtime.Serialization;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
	public class SongSimilarityList {
		public SimilarTracksListId id;
		public SongRef songref;
		public SimilarTrack[] similartracks;
		public DateTime LookupTimestamp;
		public int? StatusCode;

		public static SongSimilarityList CreateErrorList(SongRef songref, int errorCode) { return new SongSimilarityList { LookupTimestamp = DateTime.UtcNow, StatusCode = errorCode, similartracks = new SimilarTrack[0], songref = songref }; }
	}

	public static class SimilarityTo {
		public static SimilarityTo<T> Create<T>(T id, float sim) { return new SimilarityTo<T>(id, sim); }
	}



	public struct TrackSimilarityListInfo {
		public readonly SongRef SongRef;
		public readonly TrackId TrackId;
		public readonly SimilarTracksListId ListID;
		public readonly DateTime? LookupTimestamp;
		readonly SimilarityList<TrackId,TrackId.Factory> _SimilarTracks;
		public IEnumerable<SimilarityTo<TrackId>> SimilarTracks { get { return _SimilarTracks.SimilarTracks; } }
		public readonly int? StatusCode;
		public TrackSimilarityListInfo(SimilarTracksListId listID, TrackId trackId, SongRef songref, DateTime? lookupTimestamp, int? statusCode,
			IEnumerable<SimilarityTo<TrackId>> sims) {
			this.SongRef = songref; this.TrackId = trackId; this.ListID = listID; this.LookupTimestamp = lookupTimestamp;
			this.StatusCode = statusCode; this._SimilarTracks = new SimilarityList<TrackId, TrackId.Factory>(sims);
		}
		public TrackSimilarityListInfo(SimilarTracksListId listID, TrackId trackId, SongRef songref, DateTime? lookupTimestamp, int? statusCode,
			byte[] sims) {
			this.SongRef = songref; this.TrackId = trackId; this.ListID = listID; this.LookupTimestamp = lookupTimestamp;
			this.StatusCode = statusCode; this._SimilarTracks = new SimilarityList<TrackId, TrackId.Factory>(sims ?? new byte[] { });
		}
		public static TrackSimilarityListInfo CreateUnknown(SongRef song) {
			return new TrackSimilarityListInfo(default(SimilarTracksListId), default(TrackId), song, null, null, default(byte[]));
		}
	}

	public struct SimilarityTo<T> {
		public readonly T OtherId;
		public readonly float Similarity;
		public SimilarityTo(T id, float sim) { this.OtherId = id; this.Similarity = sim; }
	}


	internal struct SimilarityList<T,F> where T : struct,IId
		where F:struct, IIdFactory<T>
	{
		internal readonly byte[] encodedSims;
		public bool HasValue { get { return encodedSims != null; } }
		public IEnumerable<SimilarityTo<T>> SimilarTracks { get { return DecodeRatingBlob(encodedSims); } }
		public SimilarityList(byte[] encodedSims) { this.encodedSims = encodedSims; }
		public SimilarityList(IEnumerable<SimilarityTo<T>> sims) { encodedSims = EncodeRatingBlob(sims); }



		static IEnumerable<SimilarityTo<T>> DecodeRatingBlob(byte[] arr) {
			F factory = default(F);

			using (var ms = new MemoryStream(arr))
			using (var br = new BinaryReader(ms))
				while (ms.Position < ms.Length) {
					var id = br.ReadUInt32();
					var sim = br.ReadSingle();
					yield return new SimilarityTo<T>(factory.Cast(id), sim);
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
