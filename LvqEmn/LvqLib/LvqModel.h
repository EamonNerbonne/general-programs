#pragma once
#include "stdafx.h"
#include "utils.h"
#include "LvqTrainingStat.h"
#include "LvqConstants.h"
#pragma intrinsic(pow)

class LvqDataset;
class LvqModel
{
	unsigned long long trainIter;
	unsigned long long totalIter;
	double totalElapsed;
	boost::mt19937 rngIter;
	std::vector<LvqTrainingStat> trainingStats;
protected:
	double iterationScaleFactor;
	inline double stepLearningRate() {
		double scaledIter = trainIter*iterationScaleFactor + 1.0;
		++trainIter;
		return LVQ_LR0 /sqrt(scaledIter*sqrt(scaledIter)); // significantly faster than exp(-0.75*log(scaledIter)) due to fewer cache misses;  
	}

	const int classCount;
public:
	boost::mt19937 & RngIter() {return rngIter;}
	void resetLearningRate() {trainIter=0;}
	int ClassCount() const { return classCount; }
	std::vector<LvqTrainingStat> const & TrainingStats() {return trainingStats;}

	LvqModel(boost::mt19937 & rngIter,int classCount);
	void AddTrainingStat(double trainingMeanCost,double trainingErrorRate, LvqDataset const * testSet,  std::vector<int>const & testSubset, int iterInc, double elapsedInc);
	void AddTrainingStat(LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset, int iterInc, double elapsedInc);

	virtual int classify(VectorXd const & unknownPoint) const=0; 
	virtual void computeCostAndError(VectorXd const & unknownPoint, int pointLabel,bool&err,double&cost) const=0;
	virtual double meanProjectionNorm() const=0; 
	virtual VectorXd otherStats() const { return VectorXd::Zero((int)LvqTrainingStats::Extra); }
	virtual void learnFrom(VectorXd const & newPoint, int classLabel, bool *wasError, double* hadCost)=0;
	virtual ~LvqModel() {	}
	virtual LvqModel* clone() const=0;
	virtual size_t MemAllocEstimate() const=0;
	virtual int Dimensions() const =0;
};




