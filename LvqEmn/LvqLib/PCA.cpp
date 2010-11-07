#include "stdafx.h"
#include "PCA.h"

using namespace Eigen;

PMatrix Pca2dFromCov(Eigen::MatrixXd const & covarianceMatrix) {
	SelfAdjointEigenSolver<MatrixXd> eigenSolver(covarianceMatrix, ComputeEigenvectors);
	VectorXd eigenvaluesUnsorted = eigenSolver.eigenvalues();
	MatrixXd eigVecUnsorted = eigenSolver.eigenvectors();
	assert(eigenvaluesUnsorted.size() >=2);

	std::vector<int> v;
	for(int i=0;i<eigenvaluesUnsorted.size();++i)
		v.push_back(i);
	std::sort(v.begin(),v.end(), PcaHighDim:: EigenValueSortHelper(eigenvaluesUnsorted));

	PMatrix eigVec(2,covarianceMatrix.rows());

	for(int i=0;i<2;++i) 
		eigVec.row(i).noalias() = eigVecUnsorted.col(v[i]);
		
	return eigVec;
}