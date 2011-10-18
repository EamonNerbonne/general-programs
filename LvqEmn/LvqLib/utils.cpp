#include "stdafx.h"
#include "utils.h"

#include "shuffle.h"
using std::vector;


void makeRandomOrder(boost::mt19937 & randGen, int* const toFill, int count){
	using std::random_shuffle;
	using std::accumulate;
	using boost::bind;

	for(int i=0;i<count;++i)
		toFill[i]=i;

	//boost::function<int (int max)> rnd = bind(rnd_helper, randGen, _1);

	random_shuffle(toFill, toFill +count, [&](int options) { return randGen()%options;});
//	shuffle(randGen,toFill,count);
	assert(accumulate(toFill,toFill+count,0) == (sqr(count) - count) /2 );
}

Matrix_NN shuffleMatrixCols(boost::mt19937 & randGen, Matrix_NN const & src){
	using boost::scoped_array;
	scoped_array<int> idxs(new int[src.cols()]);
	makeRandomOrder(randGen,idxs.get(),static_cast<int>(src.cols()));

	Matrix_NN retval(src.rows(),src.cols());

	for(int colI=0;colI<src.cols();++colI)
		retval.col(idxs[colI]) = src.col(colI);
	return retval;
}
