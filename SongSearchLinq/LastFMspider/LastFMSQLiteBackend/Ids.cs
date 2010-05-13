using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFMspider.LastFMSQLiteBackend {
	internal interface IId {
		uint Id { get; }
		bool HasValue { get; }
	}

	internal interface IIdFactory<T> {
		T Cast(uint id);
	}

	public struct TrackId : IId {
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		internal TrackId(uint id) { this.id = id; }
		internal TrackId(long? id) { this.id = (uint)(id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		internal struct Factory : IIdFactory<TrackId> { public TrackId Cast(uint id) { return new TrackId(id); } }
	}
	public struct ArtistId : IId {
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		internal ArtistId(uint Id) { this.id = Id; }
		internal ArtistId(long? Id) { this.id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		internal struct Factory : IIdFactory<ArtistId> { public ArtistId Cast(uint id) { return new ArtistId(id); } }
	}
	public struct SimilarTracksListId : IId {
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		internal SimilarTracksListId(uint Id) { this.id = Id; }
		internal SimilarTracksListId(long? Id) { this.id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		internal struct Factory : IIdFactory<SimilarTracksListId> { public SimilarTracksListId Cast(uint id) { return new SimilarTracksListId(id); } }
	}
	public struct SimilarArtistsListId : IId {
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		internal SimilarArtistsListId(uint Id) { this.id = Id; }
		internal SimilarArtistsListId(long? Id) { this.id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		internal struct Factory : IIdFactory<SimilarArtistsListId> { public SimilarArtistsListId Cast(uint id) { return new SimilarArtistsListId(id); } }
	}
	public struct TopTracksListId : IId {
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		internal TopTracksListId(uint Id) { this.id = Id; }
		internal TopTracksListId(long? Id) { this.id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		internal struct Factory : IIdFactory<TopTracksListId> { public TopTracksListId Cast(uint id) { return new TopTracksListId(id); } }
	}
}
