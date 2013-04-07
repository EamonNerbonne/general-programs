#pragma once
#include <boost/random/mersenne_twister.hpp>
#include <Eigen/Core>

//randomizes all values of the matrix; each is independently drawn from a normal distribution with provided mean and sigma (=stddev).
template<typename T> void randomMatrixInit(boost::mt19937 & rng, Eigen::MatrixBase< T>& mat, double mean, double sigma) {
	using namespace boost;
	normal_distribution<> distrib(mean,sigma);
	variate_generator<mt19937&, normal_distribution<> > rndGen(rng, distrib);
	for(int j=0; j<mat.cols(); j++)
		for(int i=0; i<mat.rows(); i++)
			mat(i,j) = rndGen();
}

