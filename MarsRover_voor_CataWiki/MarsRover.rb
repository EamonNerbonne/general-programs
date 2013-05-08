require 'set'
class RoverPosition
	attr_reader :x, :y
	def initialize(x,y)
		@x=x
		@y=y
	end
	def == other
		@x == other.x and @y == other.y
	end
	alias eql? ==
	def hash
		@x + 37*@y
	end

	def + other
		RoverPosition.new @x + other.x, @y + other.y
	end
end

class RoverOrientation
	attr_reader :char, :step
	
	def initialize(idx, char, step)
		@idx=idx 
		@char=char
		@step=step
	end

	N =	RoverOrientation.new( 0, "N", RoverPosition.new( 0, 1 ))
	E = RoverOrientation.new( 1, "E", RoverPosition.new( 1, 0 ))
	S = RoverOrientation.new( 2, "S", RoverPosition.new( 0,-1 ))
	W = RoverOrientation.new( 3, "W", RoverPosition.new(-1, 0 ))

	private_class_method :new

	@@byIdx = [ N, E, S, W ]
	@@byLetter = Hash[@@byIdx.map { |orientation|
		[orientation.char, orientation]
	}]

	
	def self.parse(char) 	@@byLetter[char]	end
	def right()				@@byIdx[ (@idx + 1) % 4]	end
	def left()				@@byIdx[ (@idx + 3) % 4]	end
end

class RoverState
	attr_reader :position, :orientation
	def initialize(position, orientation)
		@position=position
		@orientation=orientation
	end
	def == other
		@position == other.position and @orientation == other.orientation
	end
	alias eql? ==
	def hash
		@position.hash + 37*@orientation.hash
	end
	def turnLeft
		RoverState.new @position, @orientation.left
	end
	def turnRight
		RoverState.new @position, @orientation.right
	end
	def step
		RoverState.new @position + @orientation.step, @orientation
	end

	def self.parse str
		match = /(\d+) (\d+) ([NSEW])/.match(str)
		pos = RoverPosition.new(Integer(match[1]) , Integer(match[2]))
		orientation = RoverOrientation.parse match[3]
		RoverState.new( pos, orientation )
	end
	def to_s
		@position.x.to_s + " " + @position.y.to_s + " " + @orientation.char
	end

	@@actionHash = {
		"L" => lambda{|x| x.turnLeft },
		"R" => lambda{|x| x.turnRight },
		"M" => lambda{|x| x.step }
	}
	def doAction code
		code.chars.inject(self) { |state, char| @@actionHash[char].(state) }
	end
	def trackAction code
		code.chars.inject([self]) { |states, char| 
			states.push(@@actionHash[char].(states.last))
		}
	end

end
class PlateauSizeValidator
	def initialize(x,y)
		@x=x
		@y=y
		puts "#{@x}:#{@y}"
	end
	def isValid?(state)
		state.position.x <= @x and state.position.y <= @y and
			state.position.x >= 0 and state.position.y >= 0
	end
end
class CollisionValidator
	def initialize
		@posSet = Set.new
	end
	def add! position
		@posSet.add position
	end
	def isValid?(state)
		!@posSet.include?(state.position)
	end
end

class MarsRover

	def initialize
		@validators = [ ]
	end



	def process stream
		addSizeValidator stream
		addCollisionValidator
		while !stream.eof? do 
			stateStr = stream.gets.chomp
			actionStr = stream.gets.chomp
			state = RoverState.parse stateStr
			path = state.trackAction actionStr
			path.each { |stepState| checkValid stepState }
			@collisionValidator.add! path.last.position
			puts "#{path.last}"
		end
	end

	def addSizeValidator stream
		boundary = stream.gets.split(" ").map {|s| Integer(s) }
		@validators.push PlateauSizeValidator.new( boundary[0], boundary[1] )
	end
	def addCollisionValidator 
		@collisionValidator = CollisionValidator.new
		@validators.push @collisionValidator
	end
	def checkValid state
		@validators.each {|validator|
			if !validator.isValid?(state) then
				$stderr.puts( "Warning: dubious state #{state} per #{validator}")
			end
		}
	end
end

