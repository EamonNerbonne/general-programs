using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace LastFMspider.LastFMSQLiteBackend {
	public interface IId {
		uint Id { get; }
		bool HasValue { get; }
	}

	public interface IIdFactory<T> {
		T CastToId(uint id);
	}

	[DebuggerDisplay("Id={id}")]
	public struct TrackId : IId {
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		public TrackId(uint id) { this.id = id; }
		internal TrackId(long? id) { this.id = (uint)(id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		public override bool Equals(object obj) { return obj is TrackId && ((TrackId)obj).id == id; }
		public override int GetHashCode() {return (int)id;}
		internal struct Factory : IIdFactory<TrackId> { public TrackId CastToId(uint id) { return new TrackId(id); } }
	}

	[DebuggerDisplay("Id={id}")]
	public struct ArtistId : IId {
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		public ArtistId(uint Id) { this.id = Id; }
		internal ArtistId(long? Id) { this.id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		internal struct Factory : IIdFactory<ArtistId> { public ArtistId CastToId(uint id) { return new ArtistId(id); } }
	}

	[DebuggerDisplay("Id={id}")]
	public struct SimilarTracksListId : IId {
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		public SimilarTracksListId(uint Id) { this.id = Id; }
		internal SimilarTracksListId(long? Id) { this.id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		internal struct Factory : IIdFactory<SimilarTracksListId> { public SimilarTracksListId CastToId(uint id) { return new SimilarTracksListId(id); } }
	}

	[DebuggerDisplay("Id={id}")]
	public struct SimilarArtistsListId : IId {
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		public SimilarArtistsListId(uint Id) { this.id = Id; }
		internal SimilarArtistsListId(long? Id) { this.id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		internal struct Factory : IIdFactory<SimilarArtistsListId> { public SimilarArtistsListId CastToId(uint id) { return new SimilarArtistsListId(id); } }
	}

	[DebuggerDisplay("Id={id}")]
	public struct TopTracksListId : IId {
		//TODO:be consistent: why is this not ArtistTopTracksListId
		internal readonly uint id;
		public uint Id { get { return id; } }
		public bool HasValue { get { return id != 0; } }
		public TopTracksListId(uint Id) { this.id = Id; }
		internal TopTracksListId(long? Id) { this.id = (uint)(Id ?? 0); }
		public override string ToString() { throw new NotImplementedException("May not use ToString - returns nonsense in db queries!"); }
		internal struct Factory : IIdFactory<TopTracksListId> { public TopTracksListId CastToId(uint id) { return new TopTracksListId(id); } }
	}
}
