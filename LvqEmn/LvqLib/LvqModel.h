#pragma once
#include <Eigen/Core>
#include <queue>
#include "LvqConstants.h"
#include "GoodBadMatch.h"
#include "LvqModelSettings.h"

//#define DEBUGHELP

#ifdef _MSC_VER
#pragma intrinsic(pow)
#endif

using namespace Eigen;
struct LvqDataset;
class LvqDatasetStats;
#ifdef DEBUGHELP
const size_t initSentinal = 0xdeadbeefdeadbeef;
#endif
struct LvqModel
{
#ifdef DEBUGHELP
	size_t sentinal;
#endif
private:
	double trainIter;
	double totalIter;
	double totalElapsed;
	double totalLR;

protected:
	LvqModelRuntimeSettings settings;
private:
	double iterationScaleFactor;
protected:
	double stepLearningRate() { //starts at 1.0, descending with power -0.75
		double scaledIter = trainIter*iterationScaleFactor+1.0;
		++trainIter;
		double lr= 1.0 / sqrt(scaledIter*sqrt(scaledIter)); // significantly faster than exp(-0.75*log(scaledIter)) 
		totalLR+=lr;
		return lr;
	}
	//subclasses must append the stats they intend to collect and call their base-classes AppendTrainingStatNames
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	//subclasses must append the stats and the base-classe implementation in the same order as they did for AppendTrainingStatNames
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const;
	LvqModel(LvqModelSettings & initSettings);
	virtual bool IdenticalMu()const {return false;}

public:
	virtual Matrix_NN PrototypeDistances(Matrix_NN const & points) const=0;
	virtual Matrix_NN GetCombinedTransforms() const=0;

	typedef std::queue<std::vector<double>> Statistics;
	int epochsTrained;
	double unscaledLearningRate() const { 
		double scaledIter = trainIter*iterationScaleFactor+1.0;
		return 1.0 / sqrt(scaledIter*sqrt(scaledIter)); 
	}

	boost::mt19937 & RngIter() {return *settings.RngIter;}
	void resetLearningRate() {trainIter=0; }

	std::vector<std::wstring> TrainingStatNames() const;

	double RegisterEpochDone(int itersTrained, double elapsed, int epochs);

	void AddTrainingStat(Statistics& statQueue, LvqDataset const * trainingSet, LvqDataset const * testSet) const;

	virtual int classify(Vector_N const & unknownPoint) const=0; 
	virtual MatchQuality ComputeMatches(Vector_N const & unknownPoint, int pointLabel) const=0;
	virtual MatchQuality learnFrom(Vector_N const & newPoint, int classLabel)=0;
	virtual void DoOptionalNormalization()=0;
	virtual ~LvqModel() { 
#ifdef DEBUGHELP
		sentinal = 0; 
#endif
	}
	virtual LvqModel* clone() const=0;
	virtual void CopyTo(LvqModel& target) const=0;
	virtual size_t MemAllocEstimate() const=0;
	virtual std::vector<int> GetPrototypeLabels() const=0;
	virtual int Dimensions() const =0;

	int ClassCount() const { return settings.ClassCount;}
};


