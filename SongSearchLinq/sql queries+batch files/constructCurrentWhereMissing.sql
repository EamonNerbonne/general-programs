BEGIN TRANSACTION;
update Artist Set CurrentSimilarArtistList=(select max(ListID) from SimilarArtistList L where L.ArtistID = Artist.ArtistID) where CurrentSimilarArtistList IS NULL;
update Artist Set CurrentTopTracksList=(select max(ListID) from TopTracksList L where L.ArtistID = Artist.ArtistID) where CurrentTopTracksList IS NULL;
update Track Set CurrentSimilarTrackList=(select max(ListID) from SimilarTrackList L where L.TrackID = Track.TrackID) where CurrentSimilarTrackList IS NULL;
COMMIT TRANSACTION;