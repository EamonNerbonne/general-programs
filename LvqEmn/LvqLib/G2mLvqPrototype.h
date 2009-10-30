#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN


class G2mLvqPrototype
{
	friend class G2mLvqModel;
	boost::shared_ptr<Matrix2d> B;
	VectorXd point;
	boost::shared_ptr<Vector2d> ppoint;
	int classLabel; //only set during initialization.
	int protoIndex;
	//tmps:

	void ComputePP( PMatrix const & P) {
		Vector2d proj1 = (P*point).lazy();
		*ppoint = ((*B) * proj1).lazy();
	}

public:
	int ClassLabel() const {return classLabel;}
	Matrix2d const & matB() const {return *B;}
	VectorXd const & position() const{return point;}

	G2mLvqPrototype() :protoIndex(-1),classLabel(-1) {}
	
	G2mLvqPrototype(int protoLabel, int thisIndex, VectorXd const & initialVal) 
		: classLabel(protoLabel)
		, protoIndex(thisIndex)
		, point(initialVal) 
		, B(new Matrix2d())
		, ppoint(new Vector2d())
	{ 
		B->setIdentity();
	}

	inline double SqrDistanceTo(VectorXd const & otherPoint, PMatrix const & P, VectorXd & tmp ) const {
		//return ((*B)*(P*(point - otherPoint))).squaredNorm(); 

		//tmp = (point - otherPoint).lazy();
		//Vector2d projectedDiff  = (P * tmp).lazy();
		//Vector2d finalDiff = ((*B) * projectedDiff).lazy();
		//return finalDiff.squaredNorm();


		Vector2d projectedOther  = (P * otherPoint).lazy();
		Vector2d finalDiff = ((*B) * projectedOther).lazy() - (*ppoint);
		return finalDiff.squaredNorm();

	}
};
