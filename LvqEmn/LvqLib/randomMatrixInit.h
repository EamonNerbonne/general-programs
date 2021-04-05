#pragma once
#include <Eigen/Core>

//randomizes all values of the matrix; each is independently drawn from a normal distribution with provided mean and sigma (=stddev).
template<typename T> void randomMatrixInit(std::mt19937 & rng, Eigen::MatrixBase< T>& mat, double mean, double sigma) {
	std::normal_distribution<> distrib(mean,sigma);
	for(int j=0; j<mat.cols(); j++)
		for(int i=0; i<mat.rows(); i++)
			mat(i,j) = distrib(rng);
}

