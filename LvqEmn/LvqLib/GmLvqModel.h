#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"

using boost::scoped_array;
using std::vector;
class GmLvqModel : public AbstractLvqModel
{
	vector<MatrixXd > P; //<double,Eigen::Dynamic,Eigen::Dynamic,Eigen::ColMajor,Eigen::Dynamic,Eigen::Dynamic>
	vector<VectorXd> prototype;
	VectorXi pLabel;
	double lr_scale_P;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK;
	mutable VectorXd tmpHelper1, tmpHelper2; //vectors of dimension DIMS

	EIGEN_STRONG_INLINE double SqrDistanceTo(int protoIndex, VectorXd const & otherPoint) const {
#if EIGEN3
		tmpHelper1.noalias() = prototype[protoIndex] - otherPoint;
		tmpHelper2.noalias() = P[protoIndex] * tmpHelper1;
		return tmpHelper2.squaredNorm();
#else
		tmpHelper1 = prototype[protoIndex] - otherPoint;
		tmpHelper2 = (P[protoIndex] * tmpHelper1).lazy();
		return tmpHelper2.squaredNorm();
#endif
		
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
	EIGEN_STRONG_INLINE GoodBadMatch findMatches(VectorXd const & trainPoint, int trainLabel) const {
	GoodBadMatch match;

	for(int i=0;i<pLabel.size();i++) {
		double curDist = SqrDistanceTo(i, trainPoint);
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

public:
	virtual size_t MemAllocEstimate() const;
	virtual int Dimensions() const {return static_cast<int>(P[0].cols());}


	GmLvqModel(boost::mt19937 & rng,  bool randInit, std::vector<int> protodistribution, MatrixXd const & means);
	double costFunction(VectorXd const & unknownPoint, int pointLabel) const; 
	virtual int classify(VectorXd const & unknownPoint) const; //tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	virtual void learnFrom(VectorXd const & newPoint, int classLabel);//tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	virtual AbstractLvqModel* clone() { return new GmLvqModel(*this); }
};

inline int GmLvqModel::classify(VectorXd const & unknownPoint) const{
	using namespace std;
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<pLabel.size();++i) {
		double curDist = SqrDistanceTo(i, unknownPoint);
		if(curDist < distance) {
			match=i;
			distance = curDist;
		}
	}
	assert( match >= 0 );
	return this->pLabel(match);
}
