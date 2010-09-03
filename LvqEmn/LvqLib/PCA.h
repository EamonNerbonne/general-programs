#pragma once
#include "stdafx.h"
#include "Eigen/EigenValues"

#include "CovarianceAndMean.h"

template <typename TPoints>
struct PrincipalComponentAnalysisTemplate {

	typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1> TPoint;
	typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> TMatrix;
	
   struct EigenValueSortHelper {
	   TPoint const & eigenvalues;
   public:
	   EigenValueSortHelper(TPoint const & eigenvalues) : eigenvalues(eigenvalues) {}
	   bool operator()(int a, int b) {return eigenvalues(a) > eigenvalues(b); }
   };

	static void DoPca(Eigen::MatrixBase<TPoints>const & points, TMatrix & transform, TPoint & eigenvalues ) {
		TMatrix covarianceMatrix = Covariance::Compute( points);
		
		Eigen::SelfAdjointEigenSolver<TMatrix> eigenSolver(covarianceMatrix, Eigen::ComputeEigenvectors);
		TPoint eigenvaluesUnsorted = eigenSolver.eigenvalues();
		TMatrix eigVecUnsorted = eigenSolver.eigenvectors();
#ifndef NDEBUG
		//for a little less friendly testing!
		if(eigenvaluesUnsorted.size() > 4) {
			std::swap( eigenvaluesUnsorted.coeffRef(1),eigenvaluesUnsorted.coeffRef(3));
			eigVecUnsorted.col(1).swap(eigVecUnsorted.col(3));
		}
#endif

		std::vector<int> v;
		for(int i=0;i<eigenvaluesUnsorted.size();++i)
			v.push_back(i);
		std::sort(v.begin(),v.end(), EigenValueSortHelper(eigenvaluesUnsorted));

		assert(eigVecUnsorted.cols() ==eigVecUnsorted.rows());
		transform.resize(eigVecUnsorted.cols(),eigVecUnsorted.rows());
		eigenvalues.resize(eigenvaluesUnsorted.size());

		for(int i=0;i<eigenvalues.size();++i) {
			transform.row(i).noalias() = eigVecUnsorted.col(v[i]);
			eigenvalues(i) = eigenvaluesUnsorted(v[i]);
		}
		//now eigVecSorted.transpose() is an orthonormal projection matrix from data space to PCA space
		//eigenvaluesSorted tells you how important the various dimensions are, we care mostly about the first 2...
		//and then we could transform the data too ...
	}
};


typedef PrincipalComponentAnalysisTemplate<Eigen::MatrixXd> PcaHighDim;
typedef PrincipalComponentAnalysisTemplate<PMatrix> PcaLowDim;

 PMatrix PcaProjectInto2d(Eigen::MatrixBase<Eigen::MatrixXd>const & points) ;