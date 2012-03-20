#pragma once
#include <Eigen/Core>
#include <queue>
#include "LvqConstants.h"
#include "GoodBadMatch.h"
#include "LvqModelSettings.h"

#ifdef _MSC_VER
#pragma intrinsic(log)
#pragma intrinsic(exp)
#endif

using namespace Eigen;
struct LvqDataset;
class LvqDatasetStats;
struct LvqModel
{
public:
	double trainIter;
	double totalIter;
	double totalElapsed;
	double totalLR;
	std::vector<double> per_proto_trainIter;

protected:
	LvqModelRuntimeSettings settings;
private:
	double iterationScaleFactor, iterationScalePower;
protected:
	double stepLearningRate(size_t protoIndex) { //starts at 1.0, descending with power -0.75
		double &iters = per_proto_trainIter.size() ? per_proto_trainIter[protoIndex] : trainIter;
		double scaledIter = iters*iterationScaleFactor+1.0;
		++iters;
		double lr= exp(iterationScalePower *  log(scaledIter)); // significantly faster than exp(-0.75*log(scaledIter)) 
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
	double meanUnscaledLearningRate() const {
		double meanIters =per_proto_trainIter.size() ? std::accumulate(per_proto_trainIter.cbegin(), per_proto_trainIter.cend(), 0) / per_proto_trainIter.size() : trainIter;
		double scaledIter = meanIters*iterationScaleFactor+1.0;
		return exp(iterationScalePower *  log(scaledIter)); 
	}

	boost::mt19937 & RngIter() {return *settings.RngIter;}
	LvqModelRuntimeSettings const & ModelSettings() const {return settings;}
	void resetLearningRate() {trainIter=0; }

	std::vector<std::wstring> TrainingStatNames() const;

	double RegisterEpochDone(int itersTrained, double elapsed, int epochs);

	void AddTrainingStat(Statistics& statQueue, LvqDataset const * trainingSet, LvqDataset const * testSet) const;

	virtual int classify(Vector_N const & unknownPoint) const=0; 
	virtual MatchQuality ComputeMatches(Vector_N const & unknownPoint, int pointLabel) const=0;
	virtual MatchQuality learnFrom(Vector_N const & newPoint, int classLabel)=0;
	virtual void DoOptionalNormalization()=0;
	virtual ~LvqModel() { 
	}
	virtual LvqModel* clone() const=0;
	virtual void CopyTo(LvqModel& target) const=0;
	virtual size_t MemAllocEstimate() const=0;
	virtual std::vector<int> GetPrototypeLabels() const=0;
	virtual int Dimensions() const =0;

	int ClassCount() const { return settings.ClassCount;}
};


