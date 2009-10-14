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
	
	LvqModel(std::vector<int> protodistribution);
	int classify(VectorXd unknownPoint) const;
	void learnFrom(VectorXd newPoint, int classLabel, double lr_P, double lr_B, double lr_point);
};

