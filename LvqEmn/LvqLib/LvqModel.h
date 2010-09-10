#pragma once
#include "stdafx.h"
#include "utils.h"
#include "LvqConstants.h"
#include "GoodBadMatch.h"

#pragma intrinsic(pow)

class LvqDataset;
class LvqModel
{
	unsigned long long trainIter;
	unsigned long long totalIter;
	double totalElapsed;
	boost::mt19937 rngIter;
	std::vector<VectorXd> trainingStats;
protected:
	double iterationScaleFactor;
	inline double stepLearningRate() {
		double scaledIter = trainIter*iterationScaleFactor + 1.0;
		++trainIter;
		return LVQ_LR0 /sqrt(scaledIter*sqrt(scaledIter)); // significantly faster than exp(-0.75*log(scaledIter)) due to fewer cache misses;  
	}

	const int classCount;
	
	//subclasses must append the stats they intend to collect and call their base-classes AppendTrainingStatNames
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const { }
	//subclasses must append the stats and the base-classe implementation in the same order as they did for AppendTrainingStatNames
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset) const { }

public:
	boost::mt19937 & RngIter() {return rngIter;}
	void resetLearningRate() {trainIter=0;}
	int ClassCount() const { return classCount; }
	std::vector<VectorXd> const & TrainingStats() {return trainingStats;}
	std::vector<std::wstring> TrainingStatNames();

	LvqModel(boost::mt19937 & rngIter,int classCount);
	void AddTrainingStat(LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, double trainingMeanCost,double trainingErrorRate, LvqDataset const * testSet,  std::vector<int>const & testSubset, int iterInc, double elapsedInc);
	void AddTrainingStat(LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset, int iterInc, double elapsedInc);

	virtual int classify(VectorXd const & unknownPoint) const=0; 
	virtual GoodBadMatch ComputeMatches(VectorXd const & unknownPoint, int pointLabel) const=0;
	virtual GoodBadMatch learnFrom(VectorXd const & newPoint, int classLabel)=0;
	virtual ~LvqModel() {	}
	virtual LvqModel* clone() const=0;
	virtual size_t MemAllocEstimate() const=0;
	virtual int Dimensions() const =0;
};

