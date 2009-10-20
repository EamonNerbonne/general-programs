#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

	typedef Matrix<double,2, Eigen::Dynamic> PMatrix;

class LvqPrototype
{
	friend class LvqModel;
	boost::shared_ptr<Matrix2d> B;
	VectorXd point;
	int classLabel; //only set during initialization.
	int protoIndex;
	//tmps:
	VectorXd tmpDiff;

public:
	int ClassLabel() const {return classLabel;}
	Matrix2d const & matB() const {return *B;}
	VectorXd const & position() const{return point;}

	LvqPrototype() :protoIndex(-1),classLabel(-1) {}
	LvqPrototype(int protoLabel, int thisIndex, VectorXd const & initialVal);
	inline double SqrDistanceTo(VectorXd const & otherPoint, PMatrix const & P ) const {
		//return ((*B)*(P*(point - otherPoint))).squaredNorm(); 
		VectorXd & tmp = const_cast<LvqPrototype*>(this)->tmpDiff;
		tmp = (point - otherPoint).lazy();
		Vector2d projectedDiff  = (P * tmp).lazy();
		Vector2d finalDiff = ((*B) * projectedDiff).lazy();
		return finalDiff.squaredNorm();
	}

};
