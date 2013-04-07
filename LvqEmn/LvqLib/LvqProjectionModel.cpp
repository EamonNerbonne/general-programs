#include "stdafx.h"
#include "LvqProjectionModel.h"
#include "LvqDataset.h"
#include "utils.h"
#include "GgmLvqModel.h"
void LvqProjectionModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const { 
	LvqModel::AppendTrainingStatNames(retval);
	if(!dynamic_cast<GgmLvqModel const*>(this))
		retval.push_back(L"Projection Norm!norm!$Projection Norm");
	if(!settings.NoNnErrorRateTracking)
		retval.push_back(L"Projected NN Error Rate!error rate!NN Error");
}

void LvqProjectionModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const { 
	LvqModel::AppendOtherStats(stats,trainingSet,testSet);
	if(!dynamic_cast<GgmLvqModel const*>(this))
		stats.push_back(P.squaredNorm());
	if(!settings.NoNnErrorRateTracking)
		stats.push_back(trainingSet ? trainingSet->NearestNeighborProjectedErrorRate(*testSet,this->P) : 0.0);
}


LvqProjectionModel::LvqProjectionModel(LvqModelSettings & initSettings) 
	: LvqModel(initSettings)
	, P(initSettings.OutputDimensions(), initSettings.InputDimensions())
{
}


void LvqProjectionModel::normalizeProjectionRotation() {
	JacobiSVD<Matrix_NN> svd(P, ComputeThinU | ComputeThinV);
	//now P == U * S * V^T; with U&V unitary.
	auto Vt = svd.matrixV().transpose();
	auto U = svd.matrixU();
	auto S = svd.singularValues();

	P = S.asDiagonal() * Vt;
	double scale=1.0;
	if(!settings.neiP && !settings.scP)
		scale = normalizeProjection(P);

	compensateProjectionUpdate(U,scale);
}