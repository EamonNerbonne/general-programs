#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

typedef Matrix<double,2, Eigen::Dynamic> PMatrix;

class LvqPrototype
{
	friend class LvqModel;
	Matrix2d B;
	VectorXd point;
	int classLabel; //only set during initialization.
	int protoIndex;
public:
	int ClassLabel() const {return classLabel;}
	Matrix2d const & matB() const {return B;}
	VectorXd const & position() const{return point;}

	LvqPrototype(int protoLabel, int thisIndex, VectorXd const & initialVal);
	~LvqPrototype(void);
	double SqrDistanceTo(VectorXd otherPoint, PMatrix const & P ) const { return (B*(P*(point - otherPoint))).squaredNorm(); }
};
