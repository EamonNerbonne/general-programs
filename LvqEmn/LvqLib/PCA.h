#pragma once
#include "Eigen/EigenValues"
#include "LvqTypedefs.h"
#include "CovarianceAndMean.h"

template <typename TPoints>
struct PrincipalComponentAnalysisTemplate {

	typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1> TPoint;
	typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> TMatrix;
	typedef typename TPoints::Scalar Scalar;
	typedef typename TPoints::Index Index;

	static void DoPca(Eigen::MatrixBase<TPoints>const & points, TMatrix & transform, TPoint & eigenvalues ) {
		TMatrix covarianceMatrix = Covariance::ComputeWithMean(points);
		DoPcaFromCov(covarianceMatrix,transform,eigenvalues);
	}

	static void DoPcaFromCov(TMatrix const & covarianceMatrix, TMatrix & transform, TPoint & eigenvalues ) {	
		using namespace std;

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

		vector<pair<Scalar, Index> > v;
		for(Index i=0;i<eigenvaluesUnsorted.size();++i)
			v.push_back( make_pair(-eigenvaluesUnsorted(i),i) );
		sort(v.begin(),v.end());

		assert(eigVecUnsorted.cols() == eigVecUnsorted.rows());
		transform.resize(eigVecUnsorted.cols(), eigVecUnsorted.rows());
		eigenvalues.resize(eigenvaluesUnsorted.size());

		for(int i=0;i<eigenvalues.size();++i) {
			transform.row(i).noalias() = eigVecUnsorted.col(v[i].second);
			eigenvalues(i) = eigenvaluesUnsorted(v[i].second);
		}
		//now eigVecSorted.transpose() is an orthonormal projection matrix from data space to PCA space
		//eigenvaluesSorted tells you how important the various dimensions are, we care mostly about the first 2...
		//and then we could transform the data too ...
	}
};


typedef PrincipalComponentAnalysisTemplate<Matrix_NN> PcaHighDim;
typedef PrincipalComponentAnalysisTemplate<Matrix_P> PcaLowDim;

Matrix_P Pca2dFromCov(Matrix_NN const & covarianceMatrix) ;
inline Matrix_P PcaProjectInto2d(Matrix_NN const & points) { return Pca2dFromCov( Covariance::ComputeWithMean(points) ); }
