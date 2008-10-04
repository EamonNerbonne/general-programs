class Similarity < ActiveRecord::Base
  belongs_to :track_a, :class_name => "Track"
  belongs_to :track_b, :class_name => "Track"
end