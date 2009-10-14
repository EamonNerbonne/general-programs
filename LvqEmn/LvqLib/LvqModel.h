#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

#include "LvqPrototype.h"

class LvqModel
{
	PMatrix P;
	boost::scoped_array<LvqPrototype> prototype;
	int protoCount;
public:
	const int classCount;
	PMatrix const & getP() const {return P;}

	LvqModel(std::vector<int> protodistribution, MatrixXd const & means);
	int classify(VectorXd const & unknownPoint) const;
	void learnFrom(VectorXd const & newPoint, int classLabel, double lr_P, double lr_B, double lr_point);
};

