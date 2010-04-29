#pragma once
#include "stdafx.h"

template <typename T> T sqr(T val) {return val*val;}

void makeRandomOrder(boost::mt19937 & randGen, int*const toFill, int count);

template <typename T> double projectionSquareNorm(T const & projectionMatrix) {
#if EIGEN3
	return (projectionMatrix.transpose() * projectionMatrix).diagonal().sum();
#else
	return (projectionMatrix.transpose() * projectionMatrix).lazy().diagonal().sum();
#endif
}

template <typename T> void normalizeMatrix(T & projectionMatrix) {
	double norm = projectionSquareNorm(projectionMatrix);
	double scaleBy = 1.0 / sqrt(norm);
#if EIGEN3
	projectionMatrix *= scaleBy; 
#else
	projectionMatrix = (scaleBy * projectionMatrix).lazy(); //TODO:can't I just use the eigen3 path here?
#endif
}

using namespace Eigen;

template <typename T> void projectionRandomizeUniformScaled(boost::mt19937 & randGen, T & projectionMatrix) { //initializes all coefficients randomly to -1..1, then normalizes.
	boost::uniform_01<boost::mt19937> uniform01_rand(randGen);

	for(int col=0; col < projectionMatrix.cols(); col++)
		for(int row=0; row < projectionMatrix.rows(); row++)  //column-major storage
			projectionMatrix(row,col) = uniform01_rand()*2.0-1.0;
		
	normalizeMatrix(projectionMatrix);
}

