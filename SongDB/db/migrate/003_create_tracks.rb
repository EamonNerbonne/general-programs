class CreateTracks < ActiveRecord::Migration
  def self.up
    create_table :tracks do |t|
      t.integer :artist_id
      t.string  :title
      t.string  :slug
      t.string  :mbid
      t.integer :lookup_timestamp
      t.integer :info_timestamp
      t.integer :listeners
      t.integer :playcount
      t.integer :duration
      t.integer :lastfm_id
      t.timestamps
    end rescue nil
    add_index :tracks, :artist_id rescue nil
    execute "DELETE FROM tracks;"
    execute "INSERT INTO tracks SELECT t.TrackID, ArtistID, FullTitle, LowercaseTitle, NULL, LookupTimestamp, InfoTimestamp, Listeners, Playcount, Duration, LastFmId, NULL, NULL
               FROM Track t LEFT OUTER JOIN TrackInfo ti
                            ON t.TrackID = ti.TrackID;"
    drop_table "Track"
    drop_table "TrackInfo"
    drop_table "Mbid"
  end
  
  def self.down
  end
end