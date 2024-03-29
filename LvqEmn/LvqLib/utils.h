#pragma once

#include <Eigen/Core>
#include <Eigen/QR> 
#include <Eigen/SVD> 

#include <random>
#include <random>
#include <math.h>
#include <sstream>

#include "LvqTypedefs.h"

#define DBG(X) (std::cout<< #X <<": "<<(X)<<"\n")


template <typename T> T& as_lvalue(T&& temporary_value) { return temporary_value; }

template <typename T> T sqr(T val) { return val * val; }

template <typename T> std::wstring to_wstring(const T& obj) { std::wstringstream sink; sink << obj; return sink.str(); }

template <typename T> EIGEN_STRONG_INLINE LvqFloat normalizeProjection(T& projectionMatrix) {
    LvqFloat scale = LvqFloat(LvqFloat(1.0) / projectionMatrix.norm());
    projectionMatrix *= scale;
    return scale;
}


inline bool almostEqual(double x, double y, double leeway = 1.0) {
    const double diff = fabs(x - y);
    const double sum = fabs(x + y);
    return diff <= leeway * std::numeric_limits<double>::epsilon() * sum;
}

template <typename T> void projectionRandomizeUniformScaled(std::mt19937& randGen, T& projectionMatrix) { //initializes all coefficients randomly to -1..1, then normalizes.
    auto uniform01_rand = std::bind(std::uniform_real_distribution(0.0, 1.0), randGen);

    for (int col = 0; col < projectionMatrix.cols(); col++)
        for (int row = 0; row < projectionMatrix.rows(); row++) //column-major storage
            projectionMatrix(row, col) = uniform01_rand() * 2.0 - 1.0;

    normalizeProjection(projectionMatrix);
}

template <typename TMat>
inline TMat MakeUpperTriangular(TMat const& fullMat) {
    TMat square(fullMat.transpose() * fullMat);
    //auto decomposition = square.llt();
    TMat retval = square.llt().matrixL();
    return retval.transpose();
}
//inline Matrix_NN MakeUpperTriangular(Matrix_NN const & fullMat) {
//    Matrix_NN square = fullMat.transpose()*fullMat;
//    Matrix_NN retval = square.llt().matrixL();
//
//    Matrix_NN topR = retval.transpose().topRows(fullMat.rows());
//    return topR;
//}


void makeRandomOrder(std::mt19937& randGen, int* const toFill, int count);
Matrix_NN shuffleMatrixCols(std::mt19937& randGen, Matrix_NN const& src);

#ifdef _MSC_VER
#define FOREACH(RANGEVAR_DECL, ITERATOR) for each(RANGEVAR_DECL in ITERATOR)
#else
#define FOREACH(RANGEVAR_DECL, ITERATOR) for(RANGEVAR_DECL : ITERATOR)
#endif

#ifdef _MSC_VER
inline bool isfinite_emn(double x) { return _finite(x) != 0; }
#else
inline bool isfinite_emn(double x) { return !(std::isinf(x) || std::isnan(x)); }
#endif

#ifdef _MSC_VER
inline bool isnan(double d) { return _isnan(d) != 0; }
#endif