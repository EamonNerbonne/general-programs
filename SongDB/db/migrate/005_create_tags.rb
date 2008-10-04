class CreateTags < ActiveRecord::Migration
  def self.up
    create_table :tags do |t|
      t.string :name
    end rescue nil
    
    execute "DELETE FROM tags;"
    execute "INSERT INTO tags SELECT TagID, LowercaseTag FROM Tag;"
    
    drop_table "Tag"
    
    create_table :taggings do |t|
      t.integer :tag_id
      t.integer :track_id
      t.integer :tag_count
    end rescue nil
    
    add_index :taggings, :tag_id
    add_index :taggings, :track_id
    
    execute "DELETE FROM taggings;"
    execute "INSERT INTO taggings SELECT TrackTagID, TagID, TrackID, TagCount FROM TrackTag;"
    
    drop_table "TrackTag"
  end
  
  def self.down
  end
end