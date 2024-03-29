#include "stdafx.h"

bool rowFromCol() {
    Matrix_NN matA, matB;

    matA = Matrix_NN::Random(12, 15);
    matB.resize(matA.cols(), matA.rows());
    for (ptrdiff_t i = 0;i < matA.cols();++i)
        matB.row(i) = matA.col(i);
    return matB.isApprox(matA.transpose());
}

bool colFromRow() {
    Matrix_NN matA, matB;

    matA = Matrix_NN::Random(12, 15);
    matB.resize(matA.cols(), matA.rows());
    for (ptrdiff_t i = 0;i < matA.rows();++i)
        matB.col(i) = matA.row(i);
    return matB.isApprox(matA.transpose());
}

bool rowFromColNoalias() {
    Matrix_NN matA, matB;

    matA = Matrix_NN::Random(12, 15);
    matB.resize(matA.cols(), matA.rows());
    for (ptrdiff_t i = 0;i < matA.cols();++i)
        matB.row(i).noalias() = matA.col(i);
    return matB.isApprox(matA.transpose());
}

bool colFromRowNoalias() {
    Matrix_NN matA, matB;

    matA = Matrix_NN::Random(12, 15);
    matB.resize(matA.cols(), matA.rows());
    for (ptrdiff_t i = 0;i < matA.rows();++i)
        matB.col(i).noalias() = matA.row(i);
    return matB.isApprox(matA.transpose());
}


#ifndef STANDALONE

BOOST_AUTO_TEST_CASE(rowColMixedTest)
{
    BOOST_CHECK(rowFromCol());
    BOOST_CHECK(rowFromColNoalias());
    BOOST_CHECK(colFromRow());
    BOOST_CHECK(colFromRowNoalias());
}
#else
#include <iostream>

int main(int argc, char* argv[]) {
    VERIFY(rowFromCol());
    VERIFY(rowFromColNoalias());
    VERIFY(colFromRow());
    VERIFY(colFromRowNoalias());

    return failed;
}


#endif