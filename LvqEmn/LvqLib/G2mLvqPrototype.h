#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN


class G2mLvqPrototype
{
	friend class G2mLvqModel;
	boost::shared_ptr<Matrix2d> B;
	VectorXd point;
	int classLabel; //only set during initialization.
	int protoIndex;
	//tmps:

public:
	int ClassLabel() const {return classLabel;}
	Matrix2d const & matB() const {return *B;}
	VectorXd const & position() const{return point;}

	G2mLvqPrototype() :protoIndex(-1),classLabel(-1) {}
	G2mLvqPrototype(int protoLabel, int thisIndex, VectorXd const & initialVal);
	inline double SqrDistanceTo(VectorXd const & otherPoint, PMatrix const & P, VectorXd & tmp ) const {
		//return ((*B)*(P*(point - otherPoint))).squaredNorm(); 
		tmp = (point - otherPoint).lazy();
		Vector2d projectedDiff  = (P * tmp).lazy();
		Vector2d finalDiff = ((*B) * projectedDiff).lazy();
		return finalDiff.squaredNorm();
	}
};
