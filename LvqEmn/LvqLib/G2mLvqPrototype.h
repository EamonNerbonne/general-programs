#pragma once
#include <Eigen/Core>
using namespace Eigen;
#include "utils.h"

class G2mLvqPrototype
{
    friend class G2mLvqModel;
    Matrix_22 B;
    Vector_N point;
    int classLabel; //only set during initialization.
    //tmps:
    Vector_2 P_point;

    EIGEN_STRONG_INLINE void ComputePP( Matrix_P const & P) {
        P_point.noalias() = P * point;
    }

public:
    inline int label() const {return classLabel;}
    inline Matrix_22 const & matB() const {return B;}
    inline Vector_N const & position() const{return point;}
    inline Vector_2 const & projectedPosition() const{return P_point;}

    G2mLvqPrototype();
    G2mLvqPrototype(Matrix_22 const & Binit, int protoLabel, Vector_N const & initialVal);
    


    inline LvqFloat SqrDistanceTo(Vector_2 const & P_testPoint) const {
        Vector_2 P_Diff = P_testPoint - P_point;
        return (B * P_Diff).squaredNorm();
    }

    inline LvqFloat SqrRawDistanceTo(Vector_2 const & P_testPoint) const {
        return (P_testPoint - P_point).squaredNorm();
    }

    EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};
