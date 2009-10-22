#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"

class GsmLvqModel : public AbstractProjectionLvqModel
{
	PMatrix P;
	//MatrixXd prototype;
	boost::scoped_array<VectorXd> prototype;
	VectorXi pLabel;
	double lr_scale_P;
	const int classCount;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK; //vectors of dimension DIMS
	PMatrix dQdP;

	inline double SqrDistanceTo(int protoIndex, VectorXd const & otherPoint, VectorXd & tmp ) const {
		//return ((*B)*(P*(point - otherPoint))).squaredNorm(); 
		tmp = prototype[protoIndex] - otherPoint;
		Vector2d proj = P * tmp;
		return proj.squaredNorm();
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
	GoodBadMatch findMatches(VectorXd const & trainPoint, int trainLabel, VectorXd & tmp); 

public:

	PMatrix const & getProjection() const {return P; }

	GsmLvqModel(std::vector<int> protodistribution, MatrixXd const & means);
	int classify(VectorXd const & unknownPoint, VectorXd & tmp) const; //tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	void learnFrom(VectorXd const & newPoint, int classLabel, double learningRate, VectorXd & tmp);//tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
};

