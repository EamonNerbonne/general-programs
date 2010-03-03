#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"

using boost::scoped_array;
using std::vector;
class GmLvqModel : public AbstractLvqModel
{
	vector<MatrixXd> P;
	vector<VectorXd> prototype;
	VectorXi pLabel;
	double lr_scale_P;
	const int classCount;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK, tmpHelper1, tmpHelper2; //vectors of dimension DIMS
	MatrixXd dQdPj, dQdPk;

	inline double SqrDistanceTo(int protoIndex, VectorXd const & otherPoint, VectorXd & tmp, VectorXd tmp2) const {
#if EIGEN3
		tmp.noalias() = prototype[protoIndex] - otherPoint;
		tmp2.noalias() = P[protoIndex] * tmp;
#else
		tmp = prototype[protoIndex] - otherPoint;
		tmp2 = P[protoIndex] * tmp;
#endif
		return tmp2.squaredNorm();
	}
	
	struct GoodBadMatch {
		double distGood, distBad;
		int matchGood, matchBad;
		GoodBadMatch()
			: distGood(std::numeric_limits<double>::infinity())
			, distBad(std::numeric_limits<double>::infinity())
			, matchGood(-1)
			, matchBad(-1)
		{}
	};
	GoodBadMatch findMatches(VectorXd const & trainPoint, int trainLabel, VectorXd & tmp, VectorXd tmp2); 

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
	int classify(VectorXd const & unknownPoint) const; //tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	void learnFrom(VectorXd const & newPoint, int classLabel);//tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	virtual AbstractLvqModel* clone() { return new GmLvqModel(*this); }
};