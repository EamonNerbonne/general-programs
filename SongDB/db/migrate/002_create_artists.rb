class CreateArtists < ActiveRecord::Migration
  def self.up
    create_table :artists do |t|
      t.column :name, :string
      t.column :slug, :string
      t.column :mbid, :string
      t.timestamps
    end rescue nil
    execute "DELETE FROM artists;"
    execute "INSERT INTO artists SELECT ArtistID, FullArtist, LowercaseArtist, NULL, NULL, NULL FROM Artist;"
    drop_table "Artist"
  end
  
  def self.down
  end
end
