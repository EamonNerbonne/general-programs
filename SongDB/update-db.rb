require 'config/boot'

puts "Database contains #{AudioFile.count} files."

puts "Scanning for files..."
files = Dir["/Volumes/Music/Music/**/*.mp3"]
files.each do |file|
  
  record = AudioFile.find_or_initialize_by_filename(file)
  
  if record.new_record? 
    if record.save
      puts "+++ #{record.filename}"
    else
      puts "EEE #{record.errors.full_messages}"
    end
  end
  
  if (record.modified.nil?) || (record.modified < (mtime = File.stat(file).mtime))
    record.modified = mtime
    record.associate_with_artist_and_track rescue nil
    record.save rescue nil
  end
  
end