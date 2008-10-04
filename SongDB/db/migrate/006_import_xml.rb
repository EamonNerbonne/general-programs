require 'libxml'
require 'base64'
require 'parsedate'
require File.join(APP_ROOT, 'config/boot')

class ImportXml < ActiveRecord::Migration
  def self.up
    doc   = LibXML::XML::Document.file(File.join(APP_ROOT, 'db/music.xml'))
    songs = doc.find("//songs/song").to_a
    len = songs.length
    songs.each_with_index do |i, idx|
      af = AudioFile.find_or_create_by_filename(Base64.decode64(i.attributes['uriUtfB64']))
      af.modified = Time.gm(*ParseDate.parsedate(i.attributes['lastmodified']))

      a  = Artist.find_or_create_by_name(i.attributes['artist'])
      t  = Track.find_or_create_by_artist_id_and_title(i.attributes['title'])
      af.track = t

      af.save rescue puts "#{af.errors.full_messages}"
      puts "[#{idx}/#{len}] #{af.filename}" if idx % 100 == 0
    end
  end
  
  def self.down
  end
end