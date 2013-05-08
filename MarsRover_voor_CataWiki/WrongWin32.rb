if RUBY_PLATFORM.downcase =~ /(win|w)32$/
  class Wrong::Config
    def self.read_here_or_higher(file, dir = ".")
      File.read(file)
    end
  end
end
