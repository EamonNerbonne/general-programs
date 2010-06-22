#include "stdafx.h"
#include "LvqModel.h"
#include "utils.h"
#include "LvqDataset.h"

using namespace std;
using namespace Eigen;

void LvqModel::AddTrainingStat(double trainingMeanCost,double trainingErrorRate, LvqDataset const * testSet,  vector<int>const & testSubset, int iterInc, double elapsedInc) {
	this->totalIter+=iterInc;
	this->totalElapsed+=elapsedInc;

	LvqTrainingStat trainingStat;
	trainingStat.trainingIter=totalIter;
	trainingStat.values = this->otherStats();
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
	this->totalIter+=iterInc;
	this->totalElapsed+=elapsedInc;

	LvqTrainingStat trainingStat;
	trainingStat.trainingIter=totalIter;
	trainingStat.values = this->otherStats();
	trainingStat.values(LvqTrainingStats::ElapsedSeconds) = totalElapsed;
	trainingStat.values(LvqTrainingStats::PNorm) = this->meanProjectionNorm();

	if(trainingSet) {
		double meanCost=0,errorRate=0;
		trainingSet->ComputeCostAndErrorRate(trainingSubset,this,meanCost,errorRate);
		trainingStat.values(LvqTrainingStats::TrainingError) =errorRate;
		trainingStat.values(LvqTrainingStats::TrainingCost) = meanCost;
	}
	if(testSet) {
		double meanCost=0,errorRate=0;
		testSet->ComputeCostAndErrorRate(testSubset,this,meanCost,errorRate);
		trainingStat.values(LvqTrainingStats::TestError) = errorRate;
		trainingStat.values(LvqTrainingStats::TestCost) = meanCost;
	} 
	this->trainingStats.push_back(trainingStat);
}