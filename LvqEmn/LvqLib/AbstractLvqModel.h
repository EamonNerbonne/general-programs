#pragma once
#include "stdafx.h"
#include "utils.h"
USING_PART_OF_NAMESPACE_EIGEN

#pragma intrinsic(pow)


class AbstractLvqModel
{
	int trainIter;
protected:
	double iterationScaleFactor;
	inline double stepLearningRate() {
		double scaledIter = trainIter*iterationScaleFactor + 1.0;
		trainIter++;
		return 0.5/ sqrt(scaledIter*sqrt(scaledIter)); //*exp(-0.65*log(scaledIter));  
	}


public:

	virtual int classify(VectorXd const & unknownPoint) const=0; 
	virtual void learnFrom(VectorXd const & newPoint, int classLabel)=0;
	//virtual double iterationScaleFactor() const=0;//says how the # of iterations must be scaled before computing the learning rate.  More complex models (with more prototypes) need to learn more slowly.
	AbstractLvqModel() : trainIter(0), iterationScaleFactor(0.01){ }
	virtual ~AbstractLvqModel() {	}
	
	//virtual normalizeProject
	virtual AbstractLvqModel* clone()=0;
	virtual size_t MemAllocEstimate() const=0;
};

class AbstractProjectionLvqModel : public AbstractLvqModel {
protected:
	AbstractProjectionLvqModel(int input_dims) : P(LVQ_LOW_DIM_SPACE, input_dims)  {	P.setIdentity(); }
	PMatrix P;
public:
	virtual ~AbstractProjectionLvqModel() { }
	PMatrix const & projectionMatrix() const {return P;}
	double projectionNorm() const { return projectionSquareNorm(P);  }
	void normalizeProjection() { normalizeMatrix(P); }
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const=0;
	//virtual int classifyProjected(Vector2d const & unknownProjectedPoint) const=0;
};