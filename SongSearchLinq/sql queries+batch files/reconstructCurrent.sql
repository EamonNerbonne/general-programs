BEGIN TRANSACTION;
update Artist Set CurrentSimilarArtistList=(select max(ListID) from SimilarArtistList L where L.ArtistID = Artist.ArtistID);
update Artist Set CurrentTopTracksList=(select max(ListID) from TopTracksList L where L.ArtistID = Artist.ArtistID);
update Track Set CurrentSimilarTrackList=(select max(ListID) from SimilarTrackList L where L.TrackID = Track.TrackID);
COMMIT TRANSACTION;