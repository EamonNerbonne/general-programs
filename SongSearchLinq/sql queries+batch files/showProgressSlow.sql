select 

CAST( (Select count(A.ArtistID) from Artist A left join SimilarArtistList L on A.ArtistID = L.ArtistID
 where L.ListID IS NULL AND A.IsAlternateOf IS NULL) AS REAL) / (select max(ArtistID) from Artist) as 'SimArtistRemaining'
 ,
 CAST( (Select count(A.ArtistID) from Artist A left join TopTracksList L on A.ArtistID = L.ArtistID
 where L.ListID IS NULL AND A.IsAlternateOf IS NULL) AS REAL) / (select max(ArtistID) from Artist) as 'TopTracksRemaining'
 ,
 CAST( (Select count(T.TrackID) from Track T left join SimilarTrackList L on T.TrackID = L.TrackID
 where L.ListID IS NULL) AS REAL) / (select max(TrackID) from Track) as 'SimTracksRemaining'