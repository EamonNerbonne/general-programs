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
	trainingStat.elapsedSeconds = totalElapsed;
	trainingStat.pNorm = this->meanProjectionNorm();
	trainingStat.otherStats = this->otherStats();
	if(trainingSet) {
		trainingStat.trainingError = trainingSet->ErrorRate(trainingSubset,this);
		trainingStat.trainingCost = trainingSet->CostFunction(trainingSubset,this);
	} else {
		trainingStat.trainingError = 0;
		trainingStat.trainingCost = 0;
	}
	if(testSet) {
		trainingStat.testError = testSet->ErrorRate(testSubset,this);
		trainingStat.testCost = testSet->CostFunction(testSubset,this);
	} else {
		trainingStat.testError = 0;
		trainingStat.testCost = 0;
	}
	this->trainingStats.push_back(trainingStat);
}