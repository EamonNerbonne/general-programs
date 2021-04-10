#include "stdafx.h"
#include "PCA.h"

using namespace Eigen;

Matrix_NN PcaFromCov(Matrix_NN const& covarianceMatrix) {
    Matrix_NN transform;
    Vector_N eigenvalues;
    PcaHighDim::DoPcaFromCov(covarianceMatrix, transform, eigenvalues);
    return transform;
}