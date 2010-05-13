﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertArtistSimilarity : AbstractLfmCacheQuery {
		public InsertArtistSimilarity(LastFMSQLiteCache lfm)
			: base(lfm) {

			rating = DefineParameter("@rating");

			listID = DefineParameter("@listID");

			lowerArtistB = DefineParameter("@lowerArtistB");

		}
		protected override string CommandText {
			get {
				return @"
INSERT OR REPLACE INTO [SimilarArtist] (ListID, ArtistB, Rating) 
SELECT @listID as ListID, B.ArtistID, @rating as Rating
FROM Artist B
WHERE B.LowercaseArtist= @lowerArtistB
";
			}
		}

		DbParameter listID, lowerArtistB, rating;



		public void Execute(SimilarArtistsListId listID, string artistB, double rating) {
			lock (SyncRoot) {
				lfmCache.InsertArtist.Execute(artistB);//we could also replace casing... whatever...

				this.rating.Value = rating;
				this.listID.Value = listID.id;
				this.lowerArtistB.Value = artistB.ToLatinLowercase();
				CommandObj.ExecuteNonQuery();
			}
		}

	}



}
