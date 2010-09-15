#include "stdafx.h"
#include "LvqProjectionModel.h"
#include "LvqDataset.h"
void LvqProjectionModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const { 
	LvqModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Projection Norm|norm|Projection Matrix");
	retval.push_back(L"Projected NN Error Rate|error rate|Projection Quality");
}
void LvqProjectionModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset) const { 
	LvqModel::AppendOtherStats(stats,trainingSet,trainingSubset,testSet,testSubset);
	stats.push_back(projectionSquareNorm(P));
	stats.push_back(
		trainingSet
		?trainingSet->NearestNeighborErrorRate(trainingSubset,testSet,testSubset,this->P)
		:0.0);
}
