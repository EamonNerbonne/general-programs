#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

class G2mLvqPrototype
{
	friend class G2mLvqModel;
	Matrix2d B;
	VectorXd point;
	int classLabel; //only set during initialization.
	//tmps:
	Vector2d P_point;
	EIGEN_STRONG_INLINE void ComputePP( PMatrix const & P) {
#if EIGEN3
		P_point.noalias() = P  * point;
#else
		P_point = (P  * point).lazy();
#endif
	}

public:
	inline int ClassLabel() const {return classLabel;}
	inline Matrix2d const & matB() const {return B;}
	inline VectorXd const & position() const{return point;}

	inline G2mLvqPrototype() : classLabel(-1) {}

	inline G2mLvqPrototype(boost::mt19937 & rng, bool randInit, int protoLabel, VectorXd const & initialVal) 
		: point(initialVal) 
		, classLabel(protoLabel)
	{
		if(randInit)
			projectionRandomizeUniformScaled(rng, B);	
		else 
			B.setIdentity();
	}

	double SqrDistanceTo(Vector2d const & P_testPoint) const {
		Vector2d P_Diff;
#if EIGEN3
		P_Diff.noalias() = P_testPoint - P_point;
		return (B * P_Diff).squaredNorm();//waslazy
#else
		P_Diff = (P_testPoint - P_point).lazy();
		return (B * P_Diff).lazy().squaredNorm();
#endif
	}

	EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};
