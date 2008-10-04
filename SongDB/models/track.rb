class Track < ActiveRecord::Base
  belongs_to :artist
  has_many :audio_files, :through => :artist
  
  before_save :downcase_mbid
  
  protected
    def downcase_mbid
      mbid.downcase! if mbid
    end
end