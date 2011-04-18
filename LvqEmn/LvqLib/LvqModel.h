#pragma once
#include <Eigen/Core>
#include <queue>
#include "LvqConstants.h"
#include "GoodBadMatch.h"
#include "LvqModelSettings.h"

#pragma intrinsic(pow)

using namespace Eigen;
class LvqDataset;
class LvqDatasetStats;

class LvqModel
{
	double trainIter;
	double totalIter;
	double totalElapsed;
	
protected:
	LvqModelRuntimeSettings settings;
	double iterationScaleFactor;//TODO:make private;
	double stepLearningRate() { //starts at 1.0, descending with power -0.75
		double scaledIter = trainIter*iterationScaleFactor+1.0;
		++trainIter;
		return 1.0 / sqrt(scaledIter*sqrt(scaledIter)); // significantly faster than exp(-0.75*log(scaledIter)) 
	}
	//subclasses must append the stats they intend to collect and call their base-classes AppendTrainingStatNames
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	//subclasses must append the stats and the base-classe implementation in the same order as they did for AppendTrainingStatNames
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const;
	LvqModel(LvqModelSettings & initSettings);
	virtual bool IdenticalMu()const {return false;}

public:
	typedef std::queue<std::vector<double> > Statistics;
	int epochsTrained;
	double unscaledLearningRate() const { 
		double scaledIter = trainIter*iterationScaleFactor+1.0;
		return 1.0 / sqrt(scaledIter*sqrt(scaledIter)); 
	}

	boost::mt19937 & RngIter() {return *settings.RngIter;}//TODO:remove.
	void resetLearningRate() {trainIter=0; }

	std::vector<std::wstring> TrainingStatNames();

	double RegisterEpochDone(int itersTrained, double elapsed, int epochs);

	void AddTrainingStat(Statistics& statQueue, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset, LvqDatasetStats const & trainingstats) const;
	void AddTrainingStat(Statistics& statQueue, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const;

	virtual int classify(Vector_N const & unknownPoint) const=0; 
	virtual MatchQuality ComputeMatches(Vector_N const & unknownPoint, int pointLabel) const=0;
	virtual MatchQuality learnFrom(Vector_N const & newPoint, int classLabel)=0;
	virtual void DoOptionalNormalization()=0;
	virtual ~LvqModel() {	}
	virtual LvqModel* clone() const=0;
	virtual void CopyTo(LvqModel& target) const=0;
	virtual size_t MemAllocEstimate() const=0;
	virtual std::vector<int> GetPrototypeLabels() const=0;
	virtual int Dimensions() const =0;
	
	int ClassCount() const { return settings.ClassCount;}
};
