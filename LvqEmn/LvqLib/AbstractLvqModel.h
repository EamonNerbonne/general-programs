#pragma once
#include "stdafx.h"
#include "utils.h"

#pragma intrinsic(pow)


class AbstractLvqModel
{
	int trainIter;
protected:
	double iterationScaleFactor;
	inline double stepLearningRate() {
		double scaledIter = trainIter*iterationScaleFactor + 1.0;
		++trainIter;
		return 0.5/ sqrt(scaledIter*sqrt(sqrt(scaledIter))); // faster than exp(-0.625*log(scaledIter));  
	}

	const int classCount;

public:
	void resetLearningRate() {trainIter=0;}
	virtual int classify(VectorXd const & unknownPoint) const=0; 
	virtual void learnFrom(VectorXd const & newPoint, int classLabel)=0;
	AbstractLvqModel(int classCount) : trainIter(0), iterationScaleFactor(0.01),classCount(classCount){ }
	virtual ~AbstractLvqModel() {	}
	
	virtual AbstractLvqModel* clone()=0;
	virtual size_t MemAllocEstimate() const=0;
	int ClassCount() const { return classCount; }
	virtual int Dimensions() const =0;
};

class AbstractProjectionLvqModel : public AbstractLvqModel {
protected:
	AbstractProjectionLvqModel(int input_dims, int classCount) :AbstractLvqModel(classCount), P(LVQ_LOW_DIM_SPACE, input_dims)  {	P.setIdentity(); }
	PMatrix P;
public:
	virtual ~AbstractProjectionLvqModel() { }
	PMatrix const & projectionMatrix() const {return P;}
	double projectionNorm() const { return projectionSquareNorm(P);  }
	void normalizeProjection() { normalizeMatrix(P); }
	virtual int Dimensions() const {return static_cast<int>(P.cols());}

	virtual MatrixXd GetProjectedPrototypes() const=0;
	virtual std::vector<int> GetPrototypeLabels() const=0;
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const=0;
};