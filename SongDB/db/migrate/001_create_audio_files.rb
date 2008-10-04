class CreateAudioFiles < ActiveRecord::Migration
  def self.up
    create_table :audio_files do |t|
      t.column :filename, :text
      t.column :modified,  :datetime
      t.column :track_id,   :integer
      t.column :created_at, :datetime
      t.column :updated_at, :datetime
      t.timestamps
    end
    add_index  :audio_files, :track_id
  end
  
  def self.down
    drop_table :audio_files
  end
end
