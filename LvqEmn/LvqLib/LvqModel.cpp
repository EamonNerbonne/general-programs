#include "stdafx.h"
#include "LvqModel.h"
#include "utils.h"
#include "LvqDataset.h"

using namespace std;
using namespace Eigen;

LvqModel::LvqModel(boost::mt19937 & rngIter,int classCount) : trainIter(0), totalIter(0), totalElapsed(0.0), rngIter(rngIter), iterationScaleFactor(LVQ_PERCLASSITERFACTOR/classCount),classCount(classCount){ }


void LvqModel::AddTrainingStat(LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, double trainingMeanCost,double trainingErrorRate, LvqDataset const * testSet,  vector<int>const & testSubset, int iterInc, double elapsedInc) {
	this->totalIter+=iterInc;
	this->totalElapsed+=elapsedInc;

	LvqTrainingStat trainingStat;
	trainingStat.trainingIter=totalIter;
	trainingStat.values = this->otherStats(trainingSet,trainingSubset,testSet,testSubset);
	trainingStat.values(LvqTrainingStats::ElapsedSeconds) = totalElapsed;
	trainingStat.values(LvqTrainingStats::PNorm) = this->meanProjectionNorm();

	trainingStat.values(LvqTrainingStats::TrainingError) =trainingErrorRate;
	trainingStat.values(LvqTrainingStats::TrainingCost) = trainingMeanCost;
	if(testSet) {
		double meanCost=0,errorRate=0;
		testSet->ComputeCostAndErrorRate(testSubset,this,meanCost,errorRate);
		trainingStat.values(LvqTrainingStats::TestError) = errorRate;
		trainingStat.values(LvqTrainingStats::TestCost) = meanCost;
	} 
	this->trainingStats.push_back(trainingStat);
}

void LvqModel::AddTrainingStat(LvqDataset const * trainingSet,  vector<int>const & trainingSubset, LvqDataset const * testSet,  vector<int>const & testSubset, int iterInc, double elapsedInc) {
	double meanCost=0,errorRate=0;
	if(trainingSet) 
		trainingSet->ComputeCostAndErrorRate(trainingSubset,this,meanCost,errorRate);
	this->AddTrainingStat(trainingSet,trainingSubset,meanCost,errorRate,testSet,testSubset,iterInc,elapsedInc);
}