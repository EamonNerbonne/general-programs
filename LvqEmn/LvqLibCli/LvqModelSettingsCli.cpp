#include "StdAfx.h"
#include "LvqModelSettingsCli.h"
#include "LvqDatasetCli.h"
#include "utils.h"
namespace LvqLibCli {
	//TODO:ABI:LvqModelSettings
	LvqModelSettingsRaw LvqModelSettingsCli::ToNativeSettings() {

		LvqModelSettingsRaw nativeSettings = { (::LvqModelType)ModelType, Dimensionality, PrototypesPerClass, RandomInitialProjection, RandomInitialBorders, NormalizeProjection, NormalizeBoundaries, GloballyNormalize, NgUpdateProtos, NgInitializeProtos, ProjOptimalInit, BLocalInit, UpdatePointsWithoutB, SlowStartLrBad, LR0, LrScaleP, LrScaleB, LrScaleBad, ParamsSeed, InstanceSeed, TrackProjectionQuality, ParallelModels };
		return nativeSettings;
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
			+ (GloballyNormalize !=defaults->GloballyNormalize && (ModelType == LvqModelType::G2m && NormalizeBoundaries ||ModelType == LvqModelType::Lgm && NormalizeProjection) ? "gn" + (GloballyNormalize ? "+" : "") + "," : "")
			+ (NgUpdateProtos != defaults->NgUpdateProtos && ModelType != LvqModelType::Lgm && PrototypesPerClass > 1 ? "NG" + (NgUpdateProtos ? "+" : "") + "," : "")
			+ (NgInitializeProtos != defaults->NgInitializeProtos && PrototypesPerClass > 1 ? "NGi" + (NgInitializeProtos ? "+" : "") + "," : "")
			+ (ProjOptimalInit != defaults->ProjOptimalInit && (ModelType != LvqModelType::Lgm) ? "Pi" + (ProjOptimalInit ? "+" : "") + "," : "")
			+ (BLocalInit != defaults->BLocalInit && (ModelType != LvqModelType::Lgm && ModelType != LvqModelType::Gm) ? "Bi" + (BLocalInit ? "+" : "") + "," : "")
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