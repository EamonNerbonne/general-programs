using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFMspider.LastFMSQLiteBackend {
	public struct TrackId {
		internal readonly uint Id;
		public bool HasValue { get { return Id != 0; } }
		internal TrackId(uint Id) { this.Id = Id; }
		internal TrackId(long? Id) { this.Id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
	}
	public struct ArtistId {
		internal readonly uint Id;
		public bool HasValue { get { return Id != 0; } }
		internal ArtistId(uint Id) { this.Id = Id; }
		internal ArtistId(long? Id) { this.Id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
	}
	public struct SimilarTracksListId {
		internal readonly uint Id;
		public bool HasValue { get { return Id != 0; } }
		internal SimilarTracksListId(uint Id) { this.Id = Id; }
		internal SimilarTracksListId(long? Id) { this.Id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
	}
	public struct SimilarArtistsListId {
		internal readonly uint Id;
		public bool HasValue { get { return Id != 0; } }
		internal SimilarArtistsListId(uint Id) { this.Id = Id; }
		internal SimilarArtistsListId(long? Id) { this.Id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
	}
	public struct TopTracksListId {
		internal readonly uint Id;
		public bool HasValue { get { return Id != 0; } }
		internal TopTracksListId(uint Id) { this.Id = Id; }
		internal TopTracksListId(long? Id) { this.Id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
	}
}
