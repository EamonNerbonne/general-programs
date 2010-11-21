using System;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LastFMspider.LastFMSQLiteBackend {

	public class InsertArtistSimilarityList : AbstractLfmCacheQuery {
		public InsertArtistSimilarityList(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			artistID = DefineParameter("@artistID");
			lookupTimestamp = DefineParameter("@lookupTimestamp");
			statusCode = DefineParameter("@statusCode");
			listBlob = DefineParameter("@listBlob", DbType.Binary);
		}
		readonly DbParameter artistID, lookupTimestamp, statusCode, listBlob;
		protected override string CommandText {
			get {
				return @"
INSERT INTO [SimilarArtistList] (ArtistID, LookupTimestamp, StatusCode,SimilarArtists) 
VALUES (@artistID, @lookupTimestamp, @statusCode, @listBlob);

SELECT L.ListID
FROM SimilarArtistList L
WHERE L.ArtistID = @artistID
AND L.LookupTimestamp = @lookupTimestamp
LIMIT 1
";
			} //TODO: make this faster: if I write the where clause +implicit where clause of the joins in another order, is that more efficient?  Also: maybe encode sim-lists as one column
		}

		public ArtistSimilarityListInfo Execute(ArtistSimilarityList simList) {
			return DoInLockedTransaction(() => {
				ArtistId baseId = lfmCache.InsertArtist.Execute(simList.Artist);
				var listImpl = new SimilarityList<ArtistId, ArtistId.Factory>(
						from simArtist in simList.Similar
						select new SimilarityTo<ArtistId>(lfmCache.UpdateArtistCasing.Execute(simArtist.Artist), (float)simArtist.Rating)
					);

				artistID.Value = baseId.Id;
				lookupTimestamp.Value = simList.LookupTimestamp.ToUniversalTime().Ticks;
				statusCode.Value = simList.StatusCode;
				listBlob.Value = listImpl.encodedSims;
				SimilarArtistsListId listId = new SimilarArtistsListId(CommandObj.ExecuteScalar().CastDbObjectAs<long>());

				if (simList.LookupTimestamp.ToUniversalTime() > DateTime.UtcNow - TimeSpan.FromDays(1.0))
					lfmCache.ArtistSetCurrentSimList.Execute(listId); //presume if this is recently downloaded, then it's the most current.

				return new ArtistSimilarityListInfo(listId, new ArtistInfo { ArtistId = baseId, Artist = simList.Artist }, simList.LookupTimestamp.ToUniversalTime(), simList.StatusCode, listImpl);
			});
		}
	}
}
