#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

#include "LvqPrototype.h"

class LvqModel
{
	PMatrix P;
	std::vector<LvqPrototype> prototype;
public:
	const int classCount;

	LvqModel(std::vector<int> protodistribution, MatrixXd const & means);
	int classify(VectorXd const & unknownPoint) const;
	void learnFrom(VectorXd const & newPoint, int classLabel, double lr_P, double lr_B, double lr_point);
};

