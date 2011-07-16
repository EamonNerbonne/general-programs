#include "stdafx.h"
#include "LvqProjectionModel.h"
#include "LvqDataset.h"
#include "utils.h"
#include "RandomMatrix.h"
#include "GgmLvqModel.h"
void LvqProjectionModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const { 
	LvqModel::AppendTrainingStatNames(retval);
	if(!dynamic_cast<GgmLvqModel const*>(this))
		retval.push_back(L"Projection Norm!norm!Projection Matrix");
	if(settings.TrackProjectionQuality)
		retval.push_back(L"Projected NN Error Rate!error rate!Projection Quality");
}

void LvqProjectionModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const { 
	LvqModel::AppendOtherStats(stats,trainingSet,trainingSubset,testSet,testSubset);
	if(!dynamic_cast<GgmLvqModel const*>(this))
		stats.push_back(projectionSquareNorm(P));
	if(settings.TrackProjectionQuality)
		stats.push_back(trainingSet ? trainingSet->NearestNeighborProjectedErrorRate(trainingSubset,testSet,testSubset,this->P) : 0.0);
}


LvqProjectionModel::LvqProjectionModel(LvqModelSettings & initSettings) 
	: LvqModel(initSettings)
{
}


