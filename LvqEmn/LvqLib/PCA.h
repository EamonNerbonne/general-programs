#pragma once
#include "stdafx.h"
#include "Eigen/EigenValues"



//template <typename TPoints,typename TMatrix, typename TPoint>
class PrincipalComponentAnalysis {
//typedef Eigen::Matrix2d TMatrix;
//typedef PMatrix TPoints;
//typedef Eigen::Vector2d TPoint;
	typedef Eigen::MatrixXd TMatrix;
typedef Eigen::MatrixXd TPoints;
typedef Eigen::VectorXd TPoint;

public:
	static TPoint MeanPoint(Eigen::MatrixBase<TPoints>const & points) {
		return points.rowwise().sum() * (1.0/points.cols());
	}

	static void CovarianceInto(Eigen::MatrixBase<TPoints>const & points, TMatrix & into) {
		CovarianceInto(points,MeanPoint(points),into);
	}
	static void CovarianceInto(Eigen::MatrixBase<TPoints> const & points, TPoint const & mean,TMatrix & into) {
		into.noalias() = (points.colwise() - mean) * (points.colwise() - mean).transpose() * (1.0/(points.cols()-1.0)) ;
	}


	static TMatrix Covariance(Eigen::MatrixBase<TPoints>const & points) {
		return Covariance(points,MeanPoint(points));
	}
	static TMatrix Covariance(Eigen::MatrixBase<TPoints> const & points, TPoint const & mean) {
		return (points.colwise() - mean) * (points.colwise() - mean).transpose() * (1.0/(points.cols()-1.0))  ;
	}

		//	TMatrix retval;
		//retval.noalias() = 
		// (points.colwise() - mean) * (points.colwise() - mean).transpose() * (1.0/(points.cols()-1.0));
		//return retval;


	//equiv possibly faster version:
	static TMatrix CovarianceB(Eigen::MatrixBase<TPoints>const & points) {
		return CovarianceB(points,MeanPoint(points));
	}
	static TMatrix CovarianceB(Eigen::MatrixBase<TPoints>const & points, TPoint const & mean) {
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
		
		Eigen::SelfAdjointEigenSolver<TMatrix> eigenSolver(covarianceMatrix, true);
		//Eigen::EigenSolver<TMatrix> solver(covarianceMatrix, true); //less efficient that specific function above.


	}

};