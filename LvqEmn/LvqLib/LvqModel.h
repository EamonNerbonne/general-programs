#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

#include "LvqPrototype.h"

class LvqModel
{
	PMatrix P;
	boost::scoped_array<LvqPrototype> prototype;
	int protoCount;
	double lr_scale_P, lr_scale_B;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK; //vectors of dimension DIMS
	PMatrix dQdP;

	
public:
	const int classCount;
	PMatrix const & getP() const {return P;}
	LvqPrototype const * Prototypes() const {return prototype.get();}

	LvqModel(std::vector<int> protodistribution, MatrixXd const & means);
	int classify(VectorXd const & unknownPoint, VectorXd & tmp) const; //tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	void learnFrom(VectorXd const & newPoint, int classLabel, double learningRate, VectorXd & tmp);//tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
};

