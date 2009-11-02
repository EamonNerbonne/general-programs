#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"

using boost::scoped_array;

class GmLvqModel : public AbstractLvqModel
{
	scoped_array<MatrixXd> P;
	//MatrixXd prototype;
	scoped_array<VectorXd> prototype;
	scoped_array<VectorXd> P_prototype;
	VectorXi pLabel;
	double lr_scale_P;
	const int classCount;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK, tmpHelper1, tmpHelper2; //vectors of dimension DIMS
	MatrixXd dQdPj, dQdPk;

	inline double SqrDistanceTo(int protoIndex, VectorXd const & otherPoint, VectorXd & tmp, VectorXd tmp2) const {
		tmp = prototype[protoIndex] - otherPoint;
		tmp2 = P[protoIndex] * tmp;
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

	void RecomputeProjection(int protoIndex) {
		P_prototype[protoIndex] = (P[protoIndex] * prototype[protoIndex]).lazy();
	}

public:
	virtual double iterationScaleFactor() const {return 0.1/pLabel.size();}
	GmLvqModel(std::vector<int> protodistribution, MatrixXd const & means);
	int classify(VectorXd const & unknownPoint) const; //tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	void learnFrom(VectorXd const & newPoint, int classLabel, double learningRate);//tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
};