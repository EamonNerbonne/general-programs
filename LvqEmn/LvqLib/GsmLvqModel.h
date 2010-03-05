#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"

class GsmLvqModel : public AbstractProjectionLvqModel
{
	//PMatrix P; //in base class
	std::vector<VectorXd> prototype;
	std::vector<Vector2d, Eigen::aligned_allocator<Vector2d>  > P_prototype;
	VectorXi pLabel;
	double lr_scale_P;
	const int classCount;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK, tmpHelper; //vectors of dimension DIMS
	PMatrix dQdP;

	inline double SqrDistanceTo(int protoIndex, Vector2d const & P_otherPoint) const {
		return (P_prototype[protoIndex] - P_otherPoint).squaredNorm();
	}
	
	struct GoodBadMatch {
		double distGood, distBad;
		int matchGood, matchBad;
		inline GoodBadMatch()
			: distGood(std::numeric_limits<double>::infinity())
			, distBad(std::numeric_limits<double>::infinity())
			, matchGood(-1)
			, matchBad(-1)
		{}
	};
	inline GoodBadMatch findMatches(Vector2d const & P_trainPoint, int trainLabel);

	inline void RecomputeProjection(int protoIndex);

public:
	virtual size_t MemAllocEstimate() const {
		return 
		sizeof(GsmLvqModel) + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		sizeof(double) * (P.size() + dQdP.size()) + //dyn alloc transform + temp transform
		sizeof(double) * (vJ.size()*5) + //various vector temps
		sizeof(VectorXd) *prototype.size() +//dyn alloc prototype base overhead
		sizeof(double) * (prototype.size() * vJ.size()) + //dyn alloc prototype data
		sizeof(Vector2d) * P_prototype.size() + //cache of pretransformed prototypes
		(16/2) * (5+prototype.size()*2);//estimate for alignment mucking.
	}


	GsmLvqModel(boost::mt19937 & rng, bool randInit, std::vector<int> protodistribution, MatrixXd const & means);
	inline int classify(VectorXd const & unknownPoint) const;
	inline int classifyProjected(Vector2d const & unknownProjectedPoint) const;

	void learnFrom(VectorXd const & newPoint, int classLabel);
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const;
	virtual AbstractLvqModel* clone(); 
};

inline GsmLvqModel::GoodBadMatch GsmLvqModel::findMatches(Vector2d const & P_trainPoint, int trainLabel) {
	GoodBadMatch match;

	for(int i=0;i<pLabel.size();i++) {
		double curDist = SqrDistanceTo(i,P_trainPoint);
		if(pLabel(i) == trainLabel) {
			if(curDist < match.distGood) {
				match.matchGood = i;
				match.distGood = curDist;
			}
		} else {
			if(curDist < match.distBad) {
				match.matchBad = i;
				match.distBad = curDist;
			}
		}
	}
	
	assert( match.matchBad >= 0 && match.matchGood >=0 );
	return match;
}

inline int GsmLvqModel::classifyProjected(Vector2d const & P_otherPoint) const{
	using namespace std;
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<pLabel.size();i++) {
		double curDist = SqrDistanceTo(i, P_otherPoint);
		if(curDist < distance) {
			match=i;
			distance = curDist;
		}
	}
	assert( match >= 0 );
	return this->pLabel(match);
}

inline int GsmLvqModel::classify(VectorXd const & unknownPoint) const{
	Vector2d P_otherPoint;
#if EIGEN3
	P_otherPoint.noalias() = P * unknownPoint;
#else
	P_otherPoint = (P * unknownPoint).lazy();
#endif

	using namespace std;
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<pLabel.size();i++) {
		double curDist = SqrDistanceTo(i,P_otherPoint);
		if(curDist < distance) {
			match=i;
			distance = curDist;
		}
	}
	assert( match >= 0 );
	return this->pLabel(match);
}



inline void GsmLvqModel::RecomputeProjection(int protoIndex) {
#if EIGEN3
	P_prototype[protoIndex].noalias() = P * prototype[protoIndex];
#else
	P_prototype[protoIndex] = (P * prototype[protoIndex]).lazy();
#endif
}
