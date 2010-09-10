#include "stdafx.h"
#include "LvqProjectionModel.h"

void LvqProjectionModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const { 
	LvqModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Projection Norm|norm");
}
void LvqProjectionModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset) const { 
	LvqModel::AppendOtherStats(stats,trainingSet,trainingSubset,testSet,testSubset);
	stats.push_back(projectionSquareNorm(P));
}
