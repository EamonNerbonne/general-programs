#include "StdAfx.h"
#include "LvqModelSettingsCli.h"
#include "LvqDatasetCli.h"
#include "LvqDataset.h"
#include "utils.h"
namespace LvqLibCli {
	LvqModelSettings LvqModelSettingsCli::ToNativeSettings(LvqDatasetCli^ trainingSet, int modelFold) {
		using boost::mt19937;
		vector<int> protoDistrib;
		for(int i=0;i<trainingSet->ClassCount;++i)
			protoDistrib.push_back(PrototypesPerClass);

		LvqModelSettings initSettings(
			LvqModelSettings::LvqModelType(ModelType), 
			as_lvalue(mt19937(ParamsSeed+modelFold)), 
			as_lvalue(mt19937(InstanceSeed+modelFold)), 
			protoDistrib, 
			trainingSet->GetTrainingDataset(),
			trainingSet->GetTrainingSubset(modelFold)
		);
		initSettings.RandomInitialProjection = RandomInitialProjection;
		initSettings.RandomInitialBorders = RandomInitialBorders;
		initSettings.RuntimeSettings.TrackProjectionQuality = TrackProjectionQuality;
		initSettings.RuntimeSettings.NormalizeProjection = NormalizeProjection;
		initSettings.RuntimeSettings.NormalizeBoundaries = NormalizeBoundaries;
		initSettings.RuntimeSettings.GloballyNormalize = GloballyNormalize;
		initSettings.NgUpdateProtos = NgUpdateProtos;
		initSettings.NgInitializeProtos = NgInitializeProtos;
		initSettings.RuntimeSettings.UpdatePointsWithoutB = UpdatePointsWithoutB;
		initSettings.RuntimeSettings.SlowStartLrBad = SlowStartLrBad;
		initSettings.Dimensionality = Dimensionality;
		initSettings.RuntimeSettings.LrScaleP = LrScaleP;
		initSettings.RuntimeSettings.LrScaleB = LrScaleB;
		initSettings.RuntimeSettings.LR0 = LR0;
		initSettings.RuntimeSettings.LrScaleBad = LrScaleBad;
		return initSettings;
	}
	LvqModelSettingsCli^ LvqModelSettingsCli::Copy() {
		return (LvqModelSettingsCli^)this->MemberwiseClone();
	}

	String^ LvqModelSettingsCli::ToShorthand() {
		return ModelType.ToString()
			+ (ModelType == LvqModelType::Lgm ? "[" + Dimensionality + "]" : (TrackProjectionQuality ? "+" : "")) + ","
			+ PrototypesPerClass + ","
			+ (RandomInitialProjection != defaults->RandomInitialProjection ? "rP" + (RandomInitialProjection ? "+" : "") + ",":"")
			+ (RandomInitialBorders != defaults->RandomInitialBorders && (ModelType == LvqModelType::Ggm || ModelType==LvqModelType::G2m) ? "rB" + (RandomInitialBorders ? "+" : "") + "," : "")
			+ (NormalizeProjection != defaults->NormalizeProjection ? "nP" + (NormalizeProjection ? "+" : "") + ",":"")
			+ (NormalizeBoundaries != defaults->NormalizeBoundaries && ModelType == LvqModelType::G2m ? "nB" + (NormalizeBoundaries ? "+" : "") + "," : "")
			+ (GloballyNormalize !=defaults->GloballyNormalize && (ModelType == LvqModelType::G2m && NormalizeBoundaries || NormalizeProjection) ? "gn" + (GloballyNormalize ? "+" : "") + "," : "")
			+ (NgUpdateProtos != defaults->NgUpdateProtos && ModelType != LvqModelType::Lgm && PrototypesPerClass > 1 ? "NG" + (NgUpdateProtos ? "+" : "") + "," : "")
			+ (NgInitializeProtos != defaults->NgInitializeProtos && PrototypesPerClass > 1 ? "NGi" + (NgInitializeProtos ? "+" : "") + "," : "")
			+ (UpdatePointsWithoutB != defaults->UpdatePointsWithoutB && ModelType == LvqModelType::G2m ? "noB" + (UpdatePointsWithoutB ? "+" : "") + "," : "")
			+ (LrScaleBad != defaults->LrScaleBad ? "lrX" + LrScaleBad + ",":"")
			+ (SlowStartLrBad ? "!" : "")
			+ (LR0==LVQ_LR0 &&LrScaleP==LVQ_LrScaleP&&LrScaleB==LVQ_LrScaleB?"":
				"lr0" + LR0 + ","
				+ "lrP" + LrScaleP + ","
				+ "lrB" + LrScaleB + ",")
			+ "[" + ParamsSeed.ToString("x") + "," + InstanceSeed.ToString("x") + "]"
			+ (ParallelModels!=10?"^" + ParallelModels: "");
	}
	LvqModelSettingsCli^ LvqModelSettingsCli::WithChanges(LvqModelType type, int protos, unsigned rngParams, unsigned rngIter){
		auto retval = Copy();
		retval->ModelType = type;
		retval->PrototypesPerClass = protos;
		retval->ParamsSeed = rngParams;
		retval->InstanceSeed = rngIter;
		return retval;
	}
}