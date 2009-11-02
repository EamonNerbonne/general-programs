#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

//#define BPROJ

class G2mLvqPrototype
{
	friend class G2mLvqModel;
	Matrix2d B;
	VectorXd point;
	int classLabel; //only set during initialization.
	//tmps:

	Vector2d P_point;
	void ComputePP( PMatrix const & P) {
#ifdef BPROJ
		Vector2d tmp = (P  * point).lazy();
		P_point = (B * tmp).lazy();
#else
		P_point = (P  * point).lazy();
#endif
	}

public:
	int ClassLabel() const {return classLabel;}
	Matrix2d const & matB() const {return B;}
	VectorXd const & position() const{return point;}

	G2mLvqPrototype() : classLabel(-1) {}
	
	G2mLvqPrototype(int protoLabel, int thisIndex, VectorXd const & initialVal) 
		: classLabel(protoLabel)
		, point(initialVal) 
	{
		B.setIdentity();
	}

	inline double SqrDistanceTo(Vector2d const & P_testPoint) const {
#ifdef BPROJ
		Vector2d B_P_testPoint = (B * P_testPoint).lazy();
		return (B_P_testPoint - P_point).squaredNorm();
#else
		Vector2d P_Diff = (P_testPoint - P_point).lazy();
		return (B * P_Diff).lazy().squaredNorm();
#endif
	}

	EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};
