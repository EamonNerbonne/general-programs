#pragma once
#include "stdafx.h"
#include "LvqProjectionModelBase.h"

class GsmLvqModel : public LvqProjectionModelBase<GsmLvqModel>
{
	//PMatrix P; //in base class
	std::vector<VectorXd> prototype;
	std::vector<Vector2d, Eigen::aligned_allocator<Vector2d>  > P_prototype;
	VectorXi pLabel;
	double lr_scale_P;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK; //vectors of dimension DIMS

	EIGEN_STRONG_INLINE void RecomputeProjection(int protoIndex) {
		P_prototype[protoIndex].noalias() = P * prototype[protoIndex];
	}


public:
	//for templates:
	
	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::GsmModelType;
	inline int PrototypeLabel(int protoIndex) const {return pLabel(protoIndex);}
	inline int PrototypeCount() const {return static_cast<int>(pLabel.size());}
	inline double SqrDistanceTo(int protoIndex, Vector2d const & P_otherPoint) const {
		return (P_prototype[protoIndex] - P_otherPoint).squaredNorm();
	}

//end for templates
	virtual size_t MemAllocEstimate() const;

	GsmLvqModel(LvqModelSettings & initSettings);
	virtual int classify(VectorXd const & unknownPoint) const {return classifyProjectedInline(P * unknownPoint);}
	virtual int classifyProjected(Vector2d const & unknownProjectedPoint) const {return classifyProjectedInline(unknownProjectedPoint);}
	inline int classifyProjectedInline(Vector2d const & P_otherPoint) const{
		double distance(std::numeric_limits<double>::infinity());
		int match(-1);

		for(int i=0;i<pLabel.size();i++) {
			double curDist = SqrDistanceTo(i, P_otherPoint);
			if(curDist < distance) { match=i; distance = curDist; }
		}
		assert( match >= 0 );
		return this->pLabel(match);
	}
	
	GoodBadMatch learnFrom(VectorXd const & newPoint, int classLabel);
	LvqModel* clone() const; 

	MatrixXd GetProjectedPrototypes() const;
	std::vector<int> GetPrototypeLabels() const;
};

