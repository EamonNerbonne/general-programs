#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

class AbstractLvqModel
{
public:
	int trainIter;
	virtual int classify(VectorXd const & unknownPoint) const=0; //tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	virtual void learnFrom(VectorXd const & newPoint, int classLabel, double learningRate)=0;//tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	AbstractLvqModel() : trainIter(0) { }
	virtual ~AbstractLvqModel() {
	//	std::cout <<"~AbstractLvqModel()\n";
	}
};

class AbstractProjectionLvqModel : public AbstractLvqModel {
public:
	virtual PMatrix const & getProjection() const = 0;
	virtual ~AbstractProjectionLvqModel() {
	//	std::cout <<"~AbstractProjectionLvqModel()\n";
	}

};