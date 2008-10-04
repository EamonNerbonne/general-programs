class CreateSimilarities < ActiveRecord::Migration
  def self.up
    create_table :similarities do |t|
      t.integer :track_a_id
      t.integer :track_b_id
      t.float   :rating
      t.timestamps
    end rescue nil
    
    add_index :similarities, [:track_a_id, :track_b_id]
    add_index :similarities, :track_a_id
    add_index :similarities, :track_b_id
    add_index :similarities, :rating
    
    execute "DELETE FROM similarities;"
    execute "INSERT INTO similarities SELECT SimilarTrackID, TrackA, TrackB, Rating, NULL, NULL FROM SimilarTrack;"
    drop_table "SimilarTrack"
  end
  
  def self.down
  end
end