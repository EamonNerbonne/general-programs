#pragma once
#include "utils.h"

template <typename T> T randomUnscalingMatrix(boost::mt19937 & rngParams, int dims) {
	T P(dims, dims);
	double Pdet = 0.0;
	while(!(Pdet >0.1 &&Pdet < 10)) {
		RandomMatrixInit(rngParams, P, 0, 1.0);
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

template <typename T> T randomScalingMatrix(boost::mt19937 & rngParams, int dims,double detScalePower ) {
	boost::uniform_01<boost::mt19937> uniform01_rand(rngParams);
	T P = randomUnscalingMatrix<T>(rngParams, dims);
	P*=exp(uniform01_rand()*2.0*detScalePower-detScalePower);
	return P;
}


template <typename T> T randomOrthogonalMatrix(boost::mt19937 & rngParams, int dims) {
	T P(dims,dims);
	double Pdet = 0.0;
	while(!(0.1 < fabs(Pdet) && fabs(Pdet) < 10)) {
		RandomMatrixInit(rngParams, P, 0, 1.0);
		Pdet = P.determinant();
		if(fabs(Pdet) <= std::numeric_limits<double>::epsilon()) continue;//exceedingly unlikely.
		Eigen::HouseholderQR<Eigen::MatrixXd> qrOfP(P);
		P = qrOfP.householderQ();
		Pdet = P.determinant();
		if(Pdet < 0.0) {
			P.col(0) *=-1;
			Pdet = P.determinant();
		}//Pdet should be 1, but we've lots of doubles and numeric accuracy is thus not perfect.
	}
	return P;
}


