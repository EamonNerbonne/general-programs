#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

class AbstractLvqModel
{
	int trainIter;

public:
	int getLearningIterationCount() {return trainIter;}
	void incLearningIterationCount() {trainIter++;}
	virtual int classify(VectorXd const & unknownPoint) const=0; 
	virtual void learnFrom(VectorXd const & newPoint, int classLabel)=0;
	virtual double iterationScaleFactor() const=0;//says how the # of iterations must be scaled before computing the learning rate.  More complex models (with more prototypes) need to learn more slowly.
	AbstractLvqModel() : trainIter(0) { }
	virtual ~AbstractLvqModel() {	}
	
	double getLearningRate() {
		return 0.3*std::pow(trainIter*iterationScaleFactor()*0.01 + 1.0, - 0.75); 
	}

	virtual AbstractLvqModel* clone()=0;
	virtual size_t MemAllocEstimate() const=0;
	//virtual void normalizeEverything()=0;
};

class AbstractProjectionLvqModel : public AbstractLvqModel {
public:
	virtual PMatrix const & getProjection() const = 0;
	virtual ~AbstractProjectionLvqModel() { }
	virtual double projectionNorm() const=0;
//	virtual void normalizeProjection()=0;

	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const=0;
	virtual int classifyProjected(Vector2d const & unknownProjectedPoint) const=0;
};