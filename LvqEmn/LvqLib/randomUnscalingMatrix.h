#pragma once
#include <random>
#include <Eigen/Core>
#include "randomMatrixInit.h"


template <typename T> T randomUnscalingMatrix(std::mt19937 & rngParams, int dims) {
	T P(dims, dims);
	double Pdet = 0.0;
	while(!(Pdet >0.1 &&Pdet < 10)) {
		randomMatrixInit(rngParams, P, 0, 1.0);
		Pdet = P.determinant();
		assert(Pdet!=0);
		if(fabs(Pdet) <= std::numeric_limits<double>::epsilon()) continue;//exceedingly unlikely.
		
		if(Pdet < 0.0) //sign doesn't _really_ matter.
			P.col(0) *=-1;
		double scale= pow (fabs(Pdet),-1.0/double(dims));
		assert(scale==scale);

		P *= scale;
		Pdet = P.determinant();
	}
	return P;
}