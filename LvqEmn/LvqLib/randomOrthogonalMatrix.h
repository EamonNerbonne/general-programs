#pragma once
#include "stdafx.h"

template <typename T> T randomOrthogonalMatrix(std::mt19937& rngParams, int dims) {
    T P(dims, dims);
    double Pdet = 0.0;
    while (!(0.1 < fabs(Pdet) && fabs(Pdet) < 10)) {
        randomMatrixInit(rngParams, P, 0, 1.0);
        Pdet = P.determinant();
        if (fabs(Pdet) <= std::numeric_limits<double>::epsilon()) continue;//exceedingly unlikely.
        Eigen::HouseholderQR<Matrix_NN> qrOfP(P);
        P = qrOfP.householderQ();
        Pdet = P.determinant();
        if (Pdet < 0.0) {
            P.col(0) *= -1;
            Pdet = P.determinant();
        }//Pdet should be 1, but we've lots of doubles and numeric accuracy is thus not perfect.
    }
    return P;
}
