#pragma once
#include <boost/random/mersenne_twister.hpp>

template<typename arrayT>
void shuffle(boost::mt19937 & randGen, arrayT arr, size_t size){
	for(size_t i = 0; i<size;++i)
		swap(arr[i],arr[i+randGen() %(size-i)]);
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
