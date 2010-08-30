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

   struct EigenValueSortHelper {
	   TPoint const & eigenvalues;
   public:
	   EigenValueSortHelper(TPoint const & eigenvalues) : eigenvalues(eigenvalues) {}
	   bool operator()(int a, int b) {return eigenvalues(a) > eigenvalues(b); }
   };

public:
	static TPoint MeanPoint(Eigen::MatrixBase<TPoints>const & points) {
		return points.rowwise().sum() * (1.0/points.cols());
	}

	static TMatrix Covariance(Eigen::MatrixBase<TPoints>const & points) {
		return Covariance(points,MeanPoint(points));
	}
	static TMatrix Covariance(Eigen::MatrixBase<TPoints> const & points, TPoint const & mean) {
		return (points.colwise() - mean) * (1.0/(points.cols()-1.0)) * (points.colwise() - mean).transpose()   ;
	}


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



	static void DoPca(Eigen::MatrixBase<TPoints>const & points ) {
		TPoint mean = MeanPoint(points);
		TPoints meanCenteredPoints = points.colwise() - mean;
		TMatrix covarianceMatrix =  (1.0/(points.cols()-1.0)) * meanCenteredPoints *  meanCenteredPoints.transpose() ;
		
		Eigen::SelfAdjointEigenSolver<TMatrix> eigenSolver(covarianceMatrix, true);
		TPoint eigenvalues = eigenSolver.eigenvalues();
		std::vector<int> v;
		for(int i=0;i<eigenvalues.size();++i)
			v.push_back(i);

		std::sort(v.begin(),v.end(), EigenValueSortHelper(eigenvalues));
		TMatrix eigVec = eigenSolver.eigenvectors();
		TMatrix eigVecSorted = eigVec;
		TPoint eigenvaluesSorted = eigenvalues;

		for(int i=0;i<eigenvalues.size();++i) {
			eigVecSorted.col(v[i]).noalias() = eigVec.col(i);
			eigenvaluesSorted(v[i]) = eigenvalues(i);
		}

		//now eigVecSorted.transpose() is an orthonormal projection matrix from data space to PCA space
		//eigenvaluesSorted tells you how important the various dimensions are, we care mostly about the first 2...
		//and then we could transform the data too ...

	}
};