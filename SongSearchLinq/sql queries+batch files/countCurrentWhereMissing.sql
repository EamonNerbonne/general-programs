select 
(select count(ArtistID) from Artist where CurrentSimilarArtistList IS NULL),
(select count(ArtistID) from  Artist where CurrentTopTracksList IS NULL),
(select count(TrackID) from Track where CurrentSimilarTrackList IS NULL)
