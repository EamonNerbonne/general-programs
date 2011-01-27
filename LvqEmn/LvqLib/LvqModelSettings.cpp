#include "StdAfx.h"
#include "LvqModelSettings.h"
#include "LvqModel.h"

#include "GmmLvqModel.h"
#include "G2mLvqModel.h"
#include "GmLvqModel.h"
#include "GsmLvqModel.h"

#include "LvqDataset.h"

LvqModel* ConstructLvqModel(LvqModelSettings & initSettings) {
	switch(initSettings.ModelType) {
	case LvqModelSettings::GmModelType:
		return new GmLvqModel(initSettings);
		break;
	case LvqModelSettings::GsmModelType:
		return new GsmLvqModel(initSettings);
		break;
	case LvqModelSettings::G2mModelType:
		return new G2mLvqModel(initSettings);
		break;
	case LvqModelSettings::GmmModelType:
		return new GmmLvqModel(initSettings);
		break;
	default:
		return 0;
		break;
	}
}

LvqModelSettings::LvqModelSettings(LvqModelType modelType, boost::mt19937 & rngParams, boost::mt19937 & rngIter, std::vector<int> protodistrib, LvqDataset const * dataset, std::vector<int> trainingset) 
	: RandomInitialProjection(true)
	, RandomInitialBorders(false) 
	, NgUpdateProtos(false)
	, RngParams(rngParams)
	, PrototypeDistribution(protodistrib)
	, Dataset(dataset)
	, ModelType(modelType)
	, RuntimeSettings(static_cast<int>(dataset->getClassCount()), rngIter)
	, Dimensionality(0)
	, Trainingset(trainingset)
{ }

size_t LvqModelSettings::Dimensions() const {return Dataset->dimensions();}

Eigen::MatrixXd LvqModelSettings::PerClassMeans() const {
	return Dataset->ComputeClassMeans(Trainingset);
}
	
PMatrix LvqModelSettings::pcaTransform() const {
	return Dataset->ComputePcaProjection(Trainingset);
}
