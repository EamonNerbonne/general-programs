#include "stdafx.h"
#include "AbstractLvqModel.h"
#include "utils.h"
#include "LvqDataset.h"

using namespace std;
using namespace Eigen;

void AbstractLvqModel::AddTrainingStat(LvqDataset const * trainingSet, vector<int>const & trainingSubset, LvqDataset const * testSet,  vector<int>const & testSubset, int iterInc, double elapsedInc) {
	this->totalIter+=iterInc;
	this->totalElapsed+=elapsedInc;

	LvqTrainingStat trainingStat;
	trainingStat.trainingIter=totalIter;
	trainingStat.values = this->otherStats();
	trainingStat.values(LvqTrainingStats::ElapsedSeconds) = totalElapsed;
	trainingStat.values(LvqTrainingStats::PNorm) = this->meanProjectionNorm();

	if(trainingSet) {
		trainingStat.values(LvqTrainingStats::TrainingError) = trainingSet->ErrorRate(trainingSubset,this);
		trainingStat.values(LvqTrainingStats::TrainingCost) = trainingSet->CostFunction(trainingSubset,this);
	} 
	if(testSet) {
		trainingStat.values(LvqTrainingStats::TestError) = testSet->ErrorRate(testSubset,this);
		trainingStat.values(LvqTrainingStats::TestCost) = testSet->CostFunction(testSubset,this);
	} 
	this->trainingStats.push_back(trainingStat);
}