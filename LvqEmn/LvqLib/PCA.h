#pragma once
#include "stdafx.h"
#include "Eigen/EigenValues"



template <typename TPoints>
class PrincipalComponentAnalysisTemplate {
public:
		typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1> TPoint;
		typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> TMatrix;
private:
	
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
		return (points.colwise() - mean) *  (points.colwise() - mean).transpose()  *(1.0/(points.cols()-1.0)) ;
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



	static void DoPca(Eigen::MatrixBase<TPoints>const & points, TMatrix & transform, TPoint & eigenvalues ) {
		TPoint mean = MeanPoint(points);
		TPoints meanCenteredPoints = points.colwise() - mean;
		TMatrix covarianceMatrix =  (1.0/(points.cols()-1.0)) * meanCenteredPoints *  meanCenteredPoints.transpose() ;
		
		Eigen::SelfAdjointEigenSolver<TMatrix> eigenSolver(covarianceMatrix, Eigen::ComputeEigenvectors);
		TPoint eigenvaluesUnsorted = eigenSolver.eigenvalues();
		TMatrix eigVecUnsorted = eigenSolver.eigenvectors();


		std::vector<int> v;
		for(int i=0;i<eigenvaluesUnsorted.size();++i)
			v.push_back(i);
		std::sort(v.begin(),v.end(), EigenValueSortHelper(eigenvaluesUnsorted));




		TMatrix eigVec = eigVecUnsorted;
		eigenvalues = eigenvaluesUnsorted;

		for(int i=0;i<eigenvalues.size();++i) {
			eigVec.col(v[i]).noalias() = eigVecUnsorted.col(i);
			eigenvalues(v[i]) = eigenvaluesUnsorted(i);
		}

		transform = eigVec.transpose();

		//now eigVecSorted.transpose() is an orthonormal projection matrix from data space to PCA space
		//eigenvaluesSorted tells you how important the various dimensions are, we care mostly about the first 2...
		//and then we could transform the data too ...

	}
};

typedef PrincipalComponentAnalysisTemplate<Eigen::MatrixXd> PcaHighDim;
typedef PrincipalComponentAnalysisTemplate<PMatrix> PcaLowDim;