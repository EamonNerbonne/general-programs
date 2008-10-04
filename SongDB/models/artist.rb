class Artist < ActiveRecord::Base
  has_many :tracks
  has_many :audio_files, :through => :tracks
  
  before_save :downcase_mbid
  
  protected
    def downcase_mbid
      mbid.downcase! if mbid
    end
end