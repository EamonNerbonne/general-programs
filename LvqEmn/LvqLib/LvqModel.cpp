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

void LvqModel::AddTrainingStat(LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, vector<int>const & testSubset, int iterInc, double elapsedInc, LvqDatasetStats const & trainingstats) {
	this->totalIter+=iterInc;
	this->totalElapsed+=elapsedInc;

	vector<double> stats;

	stats.push_back(double(totalIter));
	stats.push_back(totalElapsed);
	stats.push_back(trainingstats.errorRate);
	stats.push_back(trainingstats.meanCost);
	LvqDatasetStats teststats;
	if(testSet && testSubset.size() >0) 
		teststats = testSet->ComputeCostAndErrorRate(testSubset,this);
	stats.push_back(teststats.errorRate);
	stats.push_back(teststats.meanCost);

	stats.push_back(trainingstats.distGoodMean);
	stats.push_back(trainingstats.distBadMean);

	stats.push_back(trainingstats.distGoodVar);
	stats.push_back(trainingstats.distBadVar);

	stats.push_back(trainingstats.muJmean);
	stats.push_back(trainingstats.muKmean);

	this->AppendOtherStats(stats, trainingSet,trainingSubset,testSet,testSubset);


	this->trainingStats.push_back(fromStlVector(stats) );
}

void LvqModel::AddTrainingStat(LvqDataset const * trainingSet, vector<int>const & trainingSubset, LvqDataset const * testSet, vector<int>const & testSubset, int iterInc, double elapsedInc) {
	LvqDatasetStats trainingstats;
	if(trainingSet && trainingSubset.size() >0) 
		trainingstats=trainingSet->ComputeCostAndErrorRate(trainingSubset,this);
	this->AddTrainingStat(trainingSet,trainingSubset,testSet,testSubset,iterInc,elapsedInc,trainingstats);
}

std::vector<std::wstring> LvqModel::TrainingStatNames() {
	std::vector<std::wstring> retval;
	retval.push_back(L"Training Iterations|iterations");
	retval.push_back(L"Elapsed Seconds|seconds");
	retval.push_back(L"Training Error|error rate|Error Rates");
	retval.push_back(L"Training Cost|cost function|Cost Function");
	retval.push_back(L"Test Error|error rate|Error Rates");
	retval.push_back(L"Test Cost|cost function|Cost Function");
	retval.push_back(L"Nearest Correct Prototype Distance|distance|Prototype Distance");
	retval.push_back(L"Nearest Incorrect Prototype Distance|distance|Prototype Distance");
	retval.push_back(L"Nearest Correct Prototype Distance Variance|distance variance|Prototype Distance Variance");
	retval.push_back(L"Nearest Incorrect Prototype Distance Variance|distance variance|Prototype Distance Variance");
	retval.push_back(L"MuJ mean|mu-ratio|Mu J and K");
	retval.push_back(L"MuK mean|mu-ratio|Mu J and K");
	AppendTrainingStatNames(retval); 
	return retval;
}

void LvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const { }
void LvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const { }
