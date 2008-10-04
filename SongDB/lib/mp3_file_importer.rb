require 'id3lib'

module Mp3Importer
  
  def read_tags_from_mp3_file
    tags = ID3Lib::Tag.new(filename)
    return {:artist  => tags.artist,
            :album   => tags.album,
            :title   => tags.title,
            :genre   => tags.genre,
            :year    => tags.year.to_i,
            :raw     => tags,
            :rawtype => "id3lib"
           }
  end
  
end