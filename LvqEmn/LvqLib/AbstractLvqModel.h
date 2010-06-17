#pragma once
#include "stdafx.h"
#include "utils.h"
#include "LvqTrainingStat.h"
#pragma intrinsic(pow)

class LvqDataset;
class AbstractLvqModel
{
	int trainIter;
	int totalIter;
	double totalElapsed;
	boost::mt19937 rngIter;

protected:
	double iterationScaleFactor;
	inline double stepLearningRate() {
		double scaledIter = trainIter*iterationScaleFactor + 1.0;
		++trainIter;
		return 0.2/ sqrt(scaledIter*sqrt(scaledIter)); // significantly faster than exp(-0.75*log(scaledIter)) due to fewer cache misses;  
	}

	const int classCount;
	
public:
	boost::mt19937 & RngIter() {return rngIter;}
	std::vector<LvqTrainingStat> trainingStats;
	void resetLearningRate() {trainIter=0;}
	virtual int classify(VectorXd const & unknownPoint) const=0; 
	virtual void computeCostAndError(VectorXd const & unknownPoint, int pointLabel,bool&err,double&cost) const=0;

	virtual double meanProjectionNorm() const=0; 
	virtual VectorXd otherStats() const { return VectorXd::Zero((int)LvqTrainingStats::Extra); }

	virtual void learnFrom(VectorXd const & newPoint, int classLabel, bool *wasError, double* hadCost)=0;
	AbstractLvqModel(boost::mt19937 & rngIter,int classCount) : trainIter(0), totalIter(0), totalElapsed(0.0), rngIter(rngIter), iterationScaleFactor(0.005/classCount),classCount(classCount){ }
	virtual ~AbstractLvqModel() {	}
	void AddTrainingStat(LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset, int iterInc, double elapsedInc);
	void AddTrainingStatFast(double trainingMeanCost,double trainingErrorRate, LvqDataset const * testSet,  std::vector<int>const & testSubset, int iterInc, double elapsedInc);
	virtual AbstractLvqModel* clone()=0;
	virtual size_t MemAllocEstimate() const=0;
	int ClassCount() const { return classCount; }
	virtual int Dimensions() const =0;
};

class AbstractProjectionLvqModel : public AbstractLvqModel {
protected:
	AbstractProjectionLvqModel(boost::mt19937 & rngIter,int input_dims, int classCount) :AbstractLvqModel(rngIter,classCount), P(LVQ_LOW_DIM_SPACE, input_dims)  {	P.setIdentity(); }
	PMatrix P;
public:
	virtual ~AbstractProjectionLvqModel() { }
	PMatrix const & projectionMatrix() const {return P;}
	double projectionNorm() const { return projectionSquareNorm(P);  }
	virtual double meanProjectionNorm() const {return projectionNorm();}
	void normalizeProjection() { normalizeMatrix(P); }
	virtual int Dimensions() const {return static_cast<int>(P.cols());}

	virtual MatrixXd GetProjectedPrototypes() const=0;
	virtual std::vector<int> GetPrototypeLabels() const=0;
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const=0;
};