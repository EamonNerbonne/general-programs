#include "stdafx.h"
#include "PCA.h"

using namespace Eigen;

PMatrix Pca2dFromCov(Eigen::MatrixXd const & covarianceMatrix) {
	MatrixXd transform;
	VectorXd eigenvalues;
	PcaHighDim::DoPcaFromCov(covarianceMatrix,transform,eigenvalues);
	return transform.topRows<2>();
}