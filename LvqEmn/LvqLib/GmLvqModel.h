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
	const int classCount;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK, tmpHelper1, tmpHelper2; //vectors of dimension DIMS
	MatrixXd dQdPj, dQdPk;

	EIGEN_STRONG_INLINE double SqrDistanceTo(int protoIndex, VectorXd const & otherPoint, VectorXd & tmp, VectorXd tmp2) const {
#if EIGEN3
		tmp.noalias() = prototype[protoIndex] - otherPoint;
		return (P[protoIndex] * tmp).squaredNorm();
#else
		tmp = prototype[protoIndex] - otherPoint;
		tmp2 = P[protoIndex] * tmp;
		return tmp2.squaredNorm();
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
	inline GoodBadMatch findMatches(VectorXd const & trainPoint, int trainLabel, VectorXd & tmp, VectorXd tmp2); 

public:
	virtual size_t MemAllocEstimate() const {
		return 
		sizeof(GmLvqModel) + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		sizeof(double) * (dQdPj.size() + dQdPk.size()) + //dyn alloc temp transforms
		sizeof(double) * (dQdPj.size() * P.size()) + //dyn alloc prototype transforms
		sizeof(double) * (vJ.size()*6) + //various vector temps
		sizeof(VectorXd) *prototype.size() +//dyn alloc prototype base overhead
		sizeof(double) * (prototype.size() * vJ.size()) + //dyn alloc prototype data
		(16/2) * (6+prototype.size()*2);//estimate for alignment mucking.
	}

	GmLvqModel(boost::mt19937 & rng,  bool randInit, std::vector<int> protodistribution, MatrixXd const & means);
	inline int classify(VectorXd const & unknownPoint) const; //tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	void learnFrom(VectorXd const & newPoint, int classLabel);//tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	virtual AbstractLvqModel* clone() { return new GmLvqModel(*this); }
};

inline int GmLvqModel::classify(VectorXd const & unknownPoint) const{
	using namespace std;
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	VectorXd & tmp = const_cast<VectorXd &>(tmpHelper1);
	VectorXd & tmp2 = const_cast<VectorXd &>(tmpHelper2);



	for(int i=0;i<pLabel.size();i++) {
		double curDist = SqrDistanceTo(i, unknownPoint, tmp, tmp2);
		if(curDist < distance) {
			match=i;
			distance = curDist;
		}
	}
	assert( match >= 0 );
	return this->pLabel(match);
}

inline GmLvqModel::GoodBadMatch GmLvqModel::findMatches(VectorXd const & trainPoint, int trainLabel, VectorXd & tmp, VectorXd tmp2) {
	GoodBadMatch match;

	for(int i=0;i<pLabel.size();i++) {
		double curDist = SqrDistanceTo(i, trainPoint, tmp, tmp2);
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
