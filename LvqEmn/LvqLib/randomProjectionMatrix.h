#pragma once
#include <boost/random/mersenne_twister.hpp>
#include <Eigen/Core>
#include "randomMatrixInit.h"


inline void randomProjectionMatrix(std::mt19937 & rngParams, Matrix_NN & mat) {
	randomMatrixInit(rngParams,mat,0.0,1.0);
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
				assert(fabs(dotprod-1.0) <= sqrt(std::numeric_limits<LvqFloat>::epsilon()*mat.cols()));
			else 
				assert(fabs(dotprod) <= std::numeric_limits<LvqFloat>::epsilon()*mat.cols());
		}
	}
#endif
}
