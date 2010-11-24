#include "stdafx.h"
#include "LvqModel.h"
#include "utils.h"
#include "LvqDataset.h"

using namespace std;
using namespace Eigen;

LvqModel::LvqModel(LvqModelSettings & initSettings)
	: settings(initSettings.RuntimeSettings)
	, trainIter(0)
	, totalIter(0)
	, totalElapsed(0.0)
	, epochsTrained(0)
	{
		int protoCount = accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0);
		iterationScaleFactor = LVQ_ITERFACTOR_PERPROTO/protoCount;
	}

static VectorXd fromStlVector(vector<double> const & vec) {
	VectorXd retval(vec.size());
	for(size_t i=0;i<vec.size();++i)
		retval(i) = vec[i];
	return retval;
}

void LvqModel::AddTrainingStat(LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, double trainingMeanCost,double trainingErrorRate, LvqDataset const * testSet, vector<int>const & testSubset, int iterInc, double elapsedInc) {
	this->totalIter+=iterInc;
	this->totalElapsed+=elapsedInc;

	vector<double> stats;

	stats.push_back(double(totalIter));
	stats.push_back(totalElapsed);
	stats.push_back(trainingErrorRate);
	stats.push_back(trainingMeanCost);
	double meanCost=0,errorRate=0;
	if(testSet && testSubset.size() >0) 
		testSet->ComputeCostAndErrorRate(testSubset,this,meanCost,errorRate);
	stats.push_back(errorRate);
	stats.push_back(meanCost);
	this->AppendOtherStats(stats, trainingSet,trainingSubset,testSet,testSubset);
	
	this->trainingStats.push_back(fromStlVector(stats) );
}

void LvqModel::AddTrainingStat(LvqDataset const * trainingSet, vector<int>const & trainingSubset, LvqDataset const * testSet, vector<int>const & testSubset, int iterInc, double elapsedInc) {
	double meanCost=0,errorRate=0;
	if(trainingSet && trainingSubset.size() >0) 
		trainingSet->ComputeCostAndErrorRate(trainingSubset,this,meanCost,errorRate);
	this->AddTrainingStat(trainingSet,trainingSubset,meanCost,errorRate,testSet,testSubset,iterInc,elapsedInc);
}

std::vector<std::wstring> LvqModel::TrainingStatNames() {
	std::vector<std::wstring> retval;
	retval.push_back(L"Training Iterations|iterations");
	retval.push_back(L"Elapsed Seconds|seconds");
	retval.push_back(L"Training Error|error rate|Error Rates");
	retval.push_back(L"Training Cost|cost function|Cost Function");
	retval.push_back(L"Test Error|error rate|Error Rates");
	retval.push_back(L"Test Cost|cost function|Cost Function");
	AppendTrainingStatNames(retval); 
	return retval;
}

void LvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const { }
void LvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const { }
