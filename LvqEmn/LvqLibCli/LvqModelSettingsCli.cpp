#include "StdAfx.h"
#include "LvqModelSettingsCli.h"
#include "LvqDatasetCli.h"
namespace LvqLibCli {
	LvqModelSettings LvqModelSettingsCli::ToNativeSettings(LvqDatasetCli^ trainingSet, int modelFold) {
		using boost::mt19937;
		vector<int> protoDistrib;
		for(int i=0;i<trainingSet->ClassCount;++i)
			protoDistrib.push_back(PrototypesPerClass);

		LvqModelSettings initSettings(
			LvqModelSettings::LvqModelType(ModelType), 
			as_lvalue(mt19937(RngParamsSeed+modelFold)), 
			as_lvalue(mt19937(RngIterSeed+modelFold)), 
			protoDistrib, 
			trainingSet->GetDataset()->ComputeClassMeans(trainingSet->GetTrainingSubset(modelFold))
		);
		initSettings.RandomInitialProjection = RandomInitialProjection;
		initSettings.RandomInitialBorders = RandomInitialBorders;
		initSettings.RuntimeSettings.TrackProjectionQuality = TrackProjectionQuality;
		return initSettings;
	}
}