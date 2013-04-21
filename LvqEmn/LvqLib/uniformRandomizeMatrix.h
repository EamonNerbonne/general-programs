#pragma once
#include <boost/random/mersenne_twister.hpp>
#include <boost/random/uniform_real.hpp>
#include <Eigen/Core>
#include "randomMatrixInit.h"



template <typename T> void uniformRandomizeMatrix(T& mat, boost::mt19937 & rngParams, double min, double max) {
	boost::uniform_real<> distrib(min,max);
	boost::variate_generator<boost::mt19937&, boost::uniform_real<> > rndGen(rngParams, distrib);
	for(int j=0; j<mat.cols(); j++)
		for(int i=0; i<mat.rows(); i++)
			mat(i,j) = rndGen();
}

