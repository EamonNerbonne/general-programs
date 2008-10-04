class AudioFile < ActiveRecord::Base
  include Mp3Importer
  
  belongs_to :track
  belongs_to :artist
  
  validates_uniqueness_of :filename
  
  def associate_with_artist_and_track
    tags = read_tags_from_mp3_file
    a = Artist.find_or_create_by_name(tags[:artist])
    t = Track.find_or_create_by_title_and_artist_id(tags[:title], a.id)
    
    self.artist = a
    self.track  = t
  end
  
  def associate_with_artist_and_track!
    associate_with_artist_and_track
    save
  end
  
end