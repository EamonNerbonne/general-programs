puts "Initializing..."

require "rubygems"
require "activerecord"
require 'logger'

APP_ROOT = "#{File.dirname(__FILE__)}/.." unless defined?(APP_ROOT)
LOGGER   = Logger.new( File.join(APP_ROOT, "log/songdb.log") )
DBCONFIG = YAML.load_file(File.join(APP_ROOT, "config/database.yml"))

### Set up ActiveRecord
ActiveRecord::Base.establish_connection DBCONFIG['songdb']
ActiveRecord::Base.logger = LOGGER

### Preload ruby source files
files_to_load = Dir[File.join(APP_ROOT, "lib/*.rb")] +
                Dir[File.join(APP_ROOT, "models/*.rb")]
#files_to_load.delete(File.join(APP_ROOT, "config/boot.rb"))
files_to_load.map {|s| s.gsub(/(.*)\.rb/, '\1') }
files_to_load.each do |file|
  require file
end