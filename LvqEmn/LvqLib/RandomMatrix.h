#pragma once
#include "utils.h"
#include <boost/random/uniform_real.hpp>

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
		Eigen::HouseholderQR<Matrix_NN> qrOfP(P);
		P = qrOfP.householderQ();
		Pdet = P.determinant();
		if(Pdet < 0.0) {
			P.col(0) *=-1;
			Pdet = P.determinant();
		}//Pdet should be 1, but we've lots of doubles and numeric accuracy is thus not perfect.
	}
	return P;
}


template <typename T> void UniformRandomizeMatrix(T& mat, boost::mt19937 & rngParams, double min, double max) {
	boost::uniform_real<> distrib(min,max);
	boost::variate_generator<boost::mt19937&, boost::uniform_real<> > rndGen(rngParams, distrib);
	for(int j=0; j<mat.cols(); j++)
		for(int i=0; i<mat.rows(); i++)
			mat(i,j) = rndGen();
}


inline void randomProjectionMatrix(boost::mt19937 & rngParams, Matrix_NN & mat) {
	RandomMatrixInit(rngParams,mat,0.0,1.0);
	Eigen::JacobiSVD<Matrix_NN> svd(mat, Eigen::ComputeThinU | Eigen::ComputeThinV);
	if(mat.rows()>mat.cols())
		mat.noalias() = svd.matrixU();
	else
		mat.noalias() = svd.matrixV().transpose();
#ifndef NDEBUG
	for(int r=0;r<mat.rows();r++){
		for(int r0=0;r0<mat.rows();r0++){
			double dotprod = mat.row(r).dot(mat.row(r0));
			if(r==r0)
				assert(fabs(dotprod-1.0) <= std::numeric_limits<LvqFloat>::epsilon()*mat.cols());
			else 
				assert(fabs(dotprod) <= std::numeric_limits<LvqFloat>::epsilon()*mat.cols());
		}
	}
#endif
}

