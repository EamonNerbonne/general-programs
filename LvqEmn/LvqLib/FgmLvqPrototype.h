#pragma once
#include <Eigen/Core>
#include "LvqTypedefs.h"
using namespace Eigen;
#include "utils.h"

#define AUTO_BIAS

class FgmLvqPrototype
{
    friend class FgmLvqModel;
    Matrix_22 B;
    Vector_2 P_point;

    int classLabel; //only set during initialization.
    Vector_N point;
    double bias;//-ln(det(B)^2)

    EIGEN_STRONG_INLINE void ComputePP(Matrix_P const& P) {
        P_point.noalias() = P * point;
    }

    EIGEN_STRONG_INLINE void RecomputeBias() {
        bias = -log(sqr(B.determinant()));
        assert(isfinite_emn(bias));
    }

public:
    inline int label() const { return classLabel; }
    inline Matrix_22 const& matB() const { return B; }
    inline Vector_N const& position() const { return point; }
    inline Vector_2 const& projectedPosition() const { return P_point; }

    FgmLvqPrototype();

    FgmLvqPrototype(std::mt19937& rng, bool randInit, int protoLabel, Vector_N const& initialVal, Matrix_P const& P, Matrix_22 const& scaleB);

    inline double SqrDistanceTo(Vector_2 const& P_testPoint) const {
        Vector_2 P_Diff = P_testPoint - P_point;
        return (B.triangularView<Eigen::Upper>() * P_Diff).squaredNorm() + bias;
    }

    EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};
