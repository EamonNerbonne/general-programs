#pragma once
#include "stdafx.h"
#include "Eigen/EigenValues"

typedef Eigen::Matrix2d TMatrix;
typedef PMatrix TPoints;
typedef Eigen::Vector2d TPoint;


//template <typename TPoints,typename TMatrix, typename TPoint>
class PrincipalComponentAnalysis {
	
public:
	static TPoint MeanPoint(Eigen::MatrixBase<TPoints>const & points) {
		return points.rowwise().sum() * (1.0/points.cols());
	}
	static TMatrix Covariance(Eigen::MatrixBase<TPoints>const & points, TPoint const & mean) {
		TPoint diff = TPoint::Zero(points.rows());
		TMatrix cov = TMatrix::Zero(points.rows(),points.rows());
		for(int i=0;i<points.cols();++i) {
			diff.noalias() = points.col(i) - mean;
			cov.noalias() += diff * diff.transpose();
		}
		return cov * (1.0/(points.cols()-1.0));
	}


	//static TPoint MeanPoint(Eigen::MatrixBase<TPoints>const & points) {
	//	TPoint mean = TPoint::Zero(points.rows());
	//	for(int i=0;i<points.cols();++i)
	//		mean+=points.col(i);
	//	return mean * (1.0/points.cols());
	//}

	static void DoPca(Eigen::MatrixBase<TPoints>const & points ) {
		TPoint mean = MeanPoint(points);
		TMatrix covarianceMatrix = Covariance(points, mean);

		Eigen::EigenSolver<TMatrix> solver(covarianceMatrix, true);


	}

};