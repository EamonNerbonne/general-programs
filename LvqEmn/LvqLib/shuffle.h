#pragma once
#include <boost/random/mersenne_twister.hpp>

template<typename arrayT>
void shuffle(boost::mt19937 & randGen, arrayT& arr, unsigned size){
	for(unsigned i = 0; i<size;++i) {
		unsigned range = size-i;
		unsigned capped;
		while(true) {
			unsigned randNum = randGen();
			capped = randNum % range;
			// size-i might not divide 2^32 evenly; so reject rolls that are too high; they're too high if they're in the highest, incomplete bucket
			//if the bucket was incomplete; then I can't add range - 1 - capped without overflowing.
			// so if I overflow; try again
			if(randNum <= randNum + (range - 1 - capped)) break;
		}
		std::swap(arr[i],arr [ i + capped]);
	}
}
// (Slower) alternative is something like:
//	random_shuffle(start, end, shuffle_rnd_helper(randGen) );
//
//struct shuffle_rnd_helper {
//	boost::mt19937 & randGen;
//	shuffle_rnd_helper(boost::mt19937 & randGen) : randGen(randGen) {}
//	int operator()(int max) {return randGen()%max;}
//};

//[& randGen](int max) -> int {return randGen()%max;}
