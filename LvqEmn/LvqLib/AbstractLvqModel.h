#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

class AbstractLvqModel
{
public:
	int trainIter;
	virtual int classify(VectorXd const & unknownPoint) const=0; //tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	virtual void learnFrom(VectorXd const & newPoint, int classLabel, double learningRate)=0;//tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	virtual double iterationScaleFactor() const=0;//says how the # of iterations must be scaled before computing the learning rate.  More complex models (with more prototypes) need to learn more slowly.
	AbstractLvqModel() : trainIter(0) { }
	virtual ~AbstractLvqModel() {	}
};

class AbstractProjectionLvqModel : public AbstractLvqModel {
public:
	virtual PMatrix const & getProjection() const = 0;
	virtual ~AbstractProjectionLvqModel() { }
	virtual double projectionNorm() const=0;

	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram)=0;
};