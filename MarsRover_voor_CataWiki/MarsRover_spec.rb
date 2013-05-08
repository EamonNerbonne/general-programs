require "wrong/adapters/rspec"
require "./MarsRover.rb"
require "./WrongWin32"
#require "wrong"

include Wrong


describe RoverPosition do
	it "can create positions" do
		pos = RoverPosition.new 1, 2
		pos.should be_an_instance_of RoverPosition
	end
	#apparently comments look like this
	it "stores position" do
		pos = RoverPosition.new 1, 2
		assert{ pos.x == 1 and pos.y == 2 }
	end

	it "implements eql (y)" do
		posA = RoverPosition.new 1, 2
		posB = RoverPosition.new 1, 3
		posC = RoverPosition.new 1, 2
		assert{ posA == posC and posA != posB }
	end
	it "implements eql (x)" do
		posA = RoverPosition.new 1, 2
		posB = RoverPosition.new 2, 2
		posC = RoverPosition.new 1, 2
		assert{ posA == posC and posA != posB }
	end
	
	it "implements hash" do
		posA = RoverPosition.new 10, 20
		posB = RoverPosition.new 42, 37
		posC = RoverPosition.new 10, 20
		assert{ posA.hash == posC.hash and posA.hash != posB.hash }
		#note that hash collision is not a correctness issue
	end

	it "implements add" do
		posA = RoverPosition.new 10, 20
		posB = RoverPosition.new 32, 17
		posC = posA + posB
		assert{ posC == RoverPosition.new( 42, 37 ) }
	end
end

describe RoverOrientation do
	it "can interpret north" do
		north = RoverOrientation.parse "N"
		north.should_not be_nil
	end
	it "wont interpret nonsense" do
		assert { RoverOrientation.parse("xx") == nil }
	end
	it "knows south is right from east" do
		east = RoverOrientation.parse "E"
		south = RoverOrientation.parse "S"
		assert{ east.right == south }
	end
	it "knows west is left from north" do
		west = RoverOrientation.parse "W"
		north = RoverOrientation.parse "N"
		assert{ north.left == west }
	end
end

describe RoverState do
	it "can be created" do
		state = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::N )
	end

	it "implements eql/hash" do
		state1 = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::N )
		state2 = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::N )
		assert{ state1 == state2 and state1.hash == state2.hash }
	end
	it "implements eql/hash (when not equal)" do
		state1 = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::N )
		stateDiffO = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::E )
		stateDiffP = RoverState.new(RoverPosition.new(10, 21), RoverOrientation::N )
		assert{ state1 != stateDiffO and state1 != stateDiffP }
	end

	it "can access orientation" do
		state1 = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::N )
		assert{ state1.orientation == RoverOrientation::N }
	end

	it "can access position" do
		state1 = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::N )
		assert{ state1.position == RoverPosition.new(10, 20) }
	end

	it "can turn left" do
		state1 = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::W )
		state2 = state1.turnLeft
		assert{ state2.position == state1.position and state2.orientation == RoverOrientation::S }
	end

	it "can turn right" do
		state1 = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::N )
		state2 = state1.turnRight
		assert{ state2.position == state1.position and state2.orientation == RoverOrientation::E }
	end

	it "can step" do
		state1 = RoverState.new(RoverPosition.new(10, 20), RoverOrientation::E )
		state2 = state1.step
		assert{ state2.position == RoverPosition.new(11, 20) and state2.orientation == RoverOrientation::E }
	end

	it "integration example step" do
		state1 = RoverState.new(RoverPosition.new(0, 0), RoverOrientation::N )
		state2 = state1.step.turnRight.step.turnRight
		assert{ state2.position == RoverPosition.new(1, 1) and state2.orientation == RoverOrientation::S }
	end

	it "is immutable" do
		state1 = RoverState.new(RoverPosition.new(0, 0), RoverOrientation::N )
		state2 = state1.step.turnRight.step.turnLeft
		assert{ state1.position == RoverPosition.new(0, 0) and state1.orientation == RoverOrientation::N }
	end

	it "can parse" do
		state1 = RoverState.new(RoverPosition.new(42, 37), RoverOrientation::W )
		state2 = RoverState.parse "42 37 W"
		assert{ state1 == state2 }
	end

	it "can print" do
		state1 = RoverState.new(RoverPosition.new(42, 37), RoverOrientation::W )
		state2 = RoverState.parse "42 37 W"
		assert{ state1.to_s == "42 37 W" }
	end


	it "can interpret action code L" do
		state1 = RoverState.new(RoverPosition.new(42, 37), RoverOrientation::W )
		state2 = state1.doAction "L"
		assert{ state2.to_s == "42 37 S" }
	end
	it "can interpret action code R" do
		state1 = RoverState.new(RoverPosition.new(42, 37), RoverOrientation::W )
		state2 = state1.doAction "R"
		assert{ state2.to_s == "42 37 N" }
	end
	it "can interpret action code M" do
		state1 = RoverState.new(RoverPosition.new(42, 37), RoverOrientation::W )
		state2 = state1.doAction "M"
		assert{ state2.to_s == "41 37 W" }
	end

	it "integration example doAction" do
		state1 = RoverState.new(RoverPosition.new(0, 0), RoverOrientation::N )
		state2 = state1.doAction "MRMR"
		assert{ state2.to_s == "1 1 S" }
	end

	it "integration example trackAction" do
		state1 = RoverState.new(RoverPosition.new(0, 0), RoverOrientation::N )
		statesText = state1.trackAction("MRMR")
					.map {|s| s.to_s }
					.join(";")

		assert{ statesText == "0 0 N;0 1 N;0 1 E;1 1 E;1 1 S" }
	end
end

describe PlateauSizeValidator do
	it "accepts a state" do
		validator = PlateauSizeValidator.new(5,7)
		assert{ validator.isValid?(RoverState.parse "3 3 N") }
	end
	it "accepts 0 0" do 
		validator = PlateauSizeValidator.new(5,7)
		assert{ validator.isValid?(RoverState.parse "0 0 N") }
	end
	it "accepts boundary Y" do
		validator = PlateauSizeValidator.new(5,7)
		assert{ validator.isValid?(RoverState.parse "0 7 N") }
	end
	it "accepts boundary X" do
		validator = PlateauSizeValidator.new(8,4)
		assert{ validator.isValid?(RoverState.parse "8 3 N") }
	end
	it "rejects boundary X" do 
		validator = PlateauSizeValidator.new(7,4)
		assert{ !validator.isValid?(RoverState.parse "8 3 N") }
	end
	it "rejects boundary Y" do
		validator = PlateauSizeValidator.new(3,7)
		assert{ !validator.isValid?(RoverState.parse "3 8 N") }
	end
	it "rejects negative" do
		validator = PlateauSizeValidator.new(3,7)
		assert{ !validator.isValid?(RoverState.parse("0 0 S").step) }
	end
end

describe CollisionValidator do
	it "accepts a state" do
		validator = CollisionValidator.new
		assert{ validator.isValid?(RoverState.parse "3 3 N") }
	end
	it "accepts a state" do
		validator = CollisionValidator.new
		validator.add! (RoverPosition.new(3,4))
		assert{ validator.isValid?(RoverState.parse "4 3 N") }
	end
	it "rejects a collision" do
		validator = CollisionValidator.new
		validator.add! (RoverPosition.new(3,4))
		validator.add! (RoverPosition.new(5,7))
		assert{ !validator.isValid?(RoverState.parse "5 7 N") }
	end
end
