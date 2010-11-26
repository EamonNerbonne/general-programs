#pragma once
#include <Eigen/Core>
#include "LvqTypedefs.h"
using namespace Eigen;
#include "utils.h"
#include <cmath>
class GmmLvqPrototype
{
	friend class GmmLvqModel;
	Matrix2d B;
	Vector2d P_point;

	int classLabel; //only set during initialization.
	VectorXd point;
	double bias;//-2ln(det(B))-2ln(det(P))  --but I ignore the global factor P.

	EIGEN_STRONG_INLINE void ComputePP(PMatrix const & P) {
		P_point.noalias() = P * point;
	}
	EIGEN_STRONG_INLINE void RecomputeBias() {
		bias = -2 * log(B.determinant());
	}

public:
	inline int label() const {return classLabel;}
	inline Matrix2d const & matB() const {return B;}
	inline VectorXd const & position() const{return point;}
	inline Vector2d const & projectedPosition() const{return P_point;}

	inline GmmLvqPrototype() : classLabel(-1) {}

	inline GmmLvqPrototype(boost::mt19937 & rng, bool randInit, int protoLabel, VectorXd const & initialVal,PMatrix const & P) 
		: point(initialVal) 
		, classLabel(protoLabel)
	{
		if(randInit)
			projectionRandomizeUniformScaled(rng, B);	
		else 
			B.setIdentity();
		ComputePP(P);
		RecomputeBias();
	}

	inline double SqrDistanceTo(Vector2d const & P_testPoint) const {
		Vector2d P_Diff = P_testPoint - P_point;
		return (B * P_Diff).squaredNorm() + bias;//waslazy
	}

	//inline double SqrRawDistanceTo(Vector2d const & P_testPoint) const {
	//	return (P_testPoint - P_point).squaredNorm();
	//}

	EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};
