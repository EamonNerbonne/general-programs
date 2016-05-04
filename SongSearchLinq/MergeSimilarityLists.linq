<Query Kind="Program">
  <Reference Relative="LastFMspider\bin\Release\EmnExtensions.dll">C:\VCS\emn\programs\SongSearchLinq\LastFMspider\bin\Release\EmnExtensions.dll</Reference>
  <Reference Relative="LastFMspider\bin\Release\LastFMspider.dll">C:\VCS\emn\programs\SongSearchLinq\LastFMspider\bin\Release\LastFMspider.dll</Reference>
  <Reference Relative="LastFMspider\bin\Release\Microsoft.Experimental.IO.dll">C:\VCS\emn\programs\SongSearchLinq\LastFMspider\bin\Release\Microsoft.Experimental.IO.dll</Reference>
  <Reference Relative="LastFMspider\bin\Release\nunit.framework.dll">C:\VCS\emn\programs\SongSearchLinq\LastFMspider\bin\Release\nunit.framework.dll</Reference>
  <Reference Relative="LastFMspider\bin\Release\SongData.dll">C:\VCS\emn\programs\SongSearchLinq\LastFMspider\bin\Release\SongData.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Core.dll</Reference>
  <Reference Relative="LastFMspider\bin\Release\System.Data.SQLite.dll">C:\VCS\emn\programs\SongSearchLinq\LastFMspider\bin\Release\System.Data.SQLite.dll</Reference>
  <Reference Relative="LastFMspider\bin\Release\TagLib.Portable.dll">C:\VCS\emn\programs\SongSearchLinq\LastFMspider\bin\Release\TagLib.Portable.dll</Reference>
  <NuGetReference>CsQuery</NuGetReference>
  <NuGetReference>Dapper</NuGetReference>
  <NuGetReference>ExpressionToCodeLib</NuGetReference>
  <NuGetReference>morelinq</NuGetReference>
  <Namespace>CsQuery</Namespace>
  <Namespace>Dapper</Namespace>
  <Namespace>ExpressionToCodeLib</Namespace>
  <Namespace>LastFMspider</Namespace>
  <Namespace>MoreLinq</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Runtime.CompilerServices</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>LastFMspider.LastFMSQLiteBackend</Namespace>
</Query>

void Main()
{
	var tools = new SongTools();

	using (var db = tools.LastFmCache)
	{
		db.Connection.Execute(@"pragma cache_size=262144;");
		int startId = 0;
		var lastTrack = db.Connection.Query<int>(@"
			select max(TrackID)
			from Track
		").Single();
		long lastTotalFiveSeconds=0;
		Stopwatch sw = Stopwatch.StartNew();
		while (true)
		{
			var nextTracks = db.Connection.Query<int>(@"
				select distinct TrackID
				from SimilarTrackList
				where TrackID > @startId and length(SimilarTracks)>0
				order by TrackID 
				limit 100000
			", new { startId }).ToArray();
	
			if (nextTracks.Length == 0)
				break;
			db.DoInTransaction(() =>
			{
				foreach (var trackId in nextTracks)
				{
					var trackSims = db.Connection.Query<Bla>(@"
					select TrackID, SimilarTracks
					from SimilarTrackList
					where TrackID = @trackId and length(SimilarTracks)>0
					order by TrackID 
				", new { trackId }).ToArray();

					var similarities = trackSims.Select(o =>
					{
						var simList = new SimilarityList<TrackId, TrackId.Factory>(o.SimilarTracks).Similarities;
						var baseSimilarity = simList.FirstOrDefault().Similarity;
						return simList.Select(simTo => new SimilarityTo<TrackId>(simTo.OtherId, simTo.Similarity / baseSimilarity))
							.ToArray();
					})
						.ToArray();

					var merged = similarities.SelectMany(a => a)
						.GroupBy(a => a.OtherId, a => a.Similarity)
						.Select(g => new SimilarityTo<TrackId>(g.Key, g.Sum()))
						.OrderByDescending(o => o.Similarity);

					var newSimList = new SimilarityList<TrackId, TrackId.Factory>(merged);

					db.Connection.Execute(@"
					update Track
					set CurrentSimilarTrackList = @simTracks
					where TrackID = @trackId
				", new { trackId, simTracks = newSimList.encodedSims });
					startId = trackId + 1;
					if (sw.ElapsedMilliseconds / 5000 > lastTotalFiveSeconds)
					{
						lastTotalFiveSeconds = sw.ElapsedMilliseconds / 5000;
						$"{trackId} done ({100.0 * trackId / lastTrack}%) after {sw.Elapsed}.".Dump();
					}
				}
			});
		}
	}
}

// Define other methods and classes here
class Bla
{
	public int TrackID;
	public byte[] SimilarTracks;
}