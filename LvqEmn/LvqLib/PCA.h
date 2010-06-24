#pragma once
#include "stdafx.h"


template <typename TPoints,typename TMatrix, typename TPoint>
class PrincipalComponentAnalysis {
	using namespace Eigen;
public:
	static TPoint MeanPoint(MatrixBase<TPoints>const & points) {
		return points.rowwise().sum() * (1.0/points.cols());
	}
	static TMatrix Covariance(MatrixBase<TPoints>const & points, TPoint const & mean) {
		TPoint diff = TPoint::Zero(points.rows());
		TMatrix cov = TMatrix::Zero(points.rows(),points.rows());
		for(int i=0;i<points.cols();++i) {
			diff.noalias() = points.col(i) - mean;
			cov.noalias() += diff * diff.transpose();
		}
		return cov * (1.0/(points.cols()-1.0));
	}


	static TPoint MeanPoint(MatrixBase<TPoints>const & points) {
		TPoint mean = TPoint::Zero(points.rows());
		for(int i=0;i<points.cols();++i)
			mean+=points.col(i);
		return mean * (1.0/points.cols());
	}

	static void DoPca(MatrixBase<TPoints>const & points ) {
		int dims = points.rows();


	}

}