#include "stdafx.h"
#include "PCA.h"

using namespace Eigen;

	 PMatrix PcaProjectInto2d(Eigen::MatrixBase<Eigen::MatrixXd>const & points) {
		MatrixXd covarianceMatrix = Covariance::Compute( points);
		
		SelfAdjointEigenSolver<MatrixXd> eigenSolver(covarianceMatrix, ComputeEigenvectors);
		VectorXd eigenvaluesUnsorted = eigenSolver.eigenvalues();
		MatrixXd eigVecUnsorted = eigenSolver.eigenvectors();


		std::vector<int> v;
		for(int i=0;i<eigenvaluesUnsorted.size();++i)
			v.push_back(i);
		std::sort(v.begin(),v.end(), PcaHighDim:: EigenValueSortHelper(eigenvaluesUnsorted));

		PMatrix eigVec(2,points.rows());

		for(int i=0;i<2;++i) {
			eigVec.row(i).noalias() = eigVecUnsorted.col(v[i]);
		}

		return eigVec;

		//now eigVecSorted.transpose() is an orthonormal projection matrix from data space to PCA space
		//eigenvaluesSorted tells you how important the various dimensions are, we care mostly about the first 2...
		//and then we could transform the data too ...
	}
