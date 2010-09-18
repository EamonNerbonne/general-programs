#pragma once
#include <Eigen/Core>
using namespace Eigen;
#include "utils.h"

class G2mLvqPrototype
{
	friend class G2mLvqModel;
	Matrix2d B;
	VectorXd point;
	int classLabel; //only set during initialization.
	//tmps:
	Vector2d P_point;

	EIGEN_STRONG_INLINE void ComputePP( PMatrix const & P) {
		P_point.noalias() = P  * point;
	}

public:
	inline int label() const {return classLabel;}
	inline Matrix2d const & matB() const {return B;}
	inline VectorXd const & position() const{return point;}
	inline Vector2d const & projectedPosition() const{return P_point;}

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

	inline double SqrDistanceTo(Vector2d const & P_testPoint) const {
		Vector2d P_Diff = P_testPoint - P_point;
		return (B * P_Diff).squaredNorm();//waslazy
	}

	EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};
