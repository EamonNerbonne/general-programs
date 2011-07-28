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

	String^ LvqModelSettingsCli::ToShorthand() {
		return ModelType.ToString()
			+ (ModelType == LvqModelType::Lgm ? "[" + Dimensionality + "]" : (TrackProjectionQuality ? "+" : "")) + ","
			+ PrototypesPerClass + ","
			+ (RandomInitialProjection != LvqModelSettingsCli().RandomInitialProjection ? "rP" + (RandomInitialProjection ? "+" : "") + ",":"")
			+ (RandomInitialBorders != LvqModelSettingsCli().RandomInitialBorders && (ModelType == LvqModelType::Ggm || ModelType==LvqModelType::G2m) ? "rB" + (RandomInitialBorders ? "+" : "") + "," : "")
			+ (NormalizeProjection != LvqModelSettingsCli().NormalizeProjection ? "nP" + (NormalizeProjection ? "+" : "") + ",":"")
			+ (NormalizeBoundaries != LvqModelSettingsCli().NormalizeBoundaries && ModelType == LvqModelType::G2m ? "nB" + (NormalizeBoundaries ? "+" : "") + "," : "")
			+ (GloballyNormalize !=LvqModelSettingsCli().GloballyNormalize && (ModelType == LvqModelType::G2m && NormalizeBoundaries ||ModelType == LvqModelType::Lgm && NormalizeProjection) ? "gn" + (GloballyNormalize ? "+" : "") + "," : "")
			+ (NgUpdateProtos != LvqModelSettingsCli().NgUpdateProtos && ModelType != LvqModelType::Lgm && PrototypesPerClass > 1 ? "NG" + (NgUpdateProtos ? "+" : "") + "," : "")
			+ (NgInitializeProtos != LvqModelSettingsCli().NgInitializeProtos && PrototypesPerClass > 1 ? "NGi" + (NgInitializeProtos ? "+" : "") + "," : "")
			+ (ProjOptimalInit != LvqModelSettingsCli().ProjOptimalInit && (ModelType != LvqModelType::Lgm) ? "Pi" + (ProjOptimalInit ? "+" : "") + "," : "")
			+ (BLocalInit != LvqModelSettingsCli().BLocalInit && (ModelType != LvqModelType::Lgm && ModelType != LvqModelType::Gm) ? "Bi" + (BLocalInit ? "+" : "") + "," : "")
			+ (UpdatePointsWithoutB != LvqModelSettingsCli().UpdatePointsWithoutB && ModelType == LvqModelType::G2m ? "noB" + (UpdatePointsWithoutB ? "+" : "") + "," : "")
			+ (LrScaleBad != LvqModelSettingsCli().LrScaleBad ? "lrX" + LrScaleBad + ",":"")
			+ (SlowStartLrBad ? "!" : "")
			+ (LR0==LVQ_LR0 &&LrScaleP==LVQ_LrScaleP&&LrScaleB==LVQ_LrScaleB?"":
			"lr0" + LR0 + ","
			+ "lrP" + LrScaleP + ","
			+ "lrB" + LrScaleB + ",")
			+ "[" + ParamsSeed.ToString("x") + "," + InstanceSeed.ToString("x") + "]"
			+ (ParallelModels!=10?"^" + ParallelModels: "");
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithChanges(LvqModelType type, int protos, unsigned rngParams, unsigned rngIter){
		LvqModelSettingsCli retval = *this;
		retval.ModelType = type;
		retval.PrototypesPerClass = protos;
		retval.ParamsSeed = rngParams;
		retval.InstanceSeed = rngIter;
		return retval;
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithDefaultLr(){
		LvqModelSettingsCli retval = *this;
        retval.LR0 = LvqModelSettingsCli().LR0;
		retval.LrScaleP = LvqModelSettingsCli().LrScaleP;
		retval.LrScaleB = LvqModelSettingsCli().LrScaleB;
		return retval;
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithDefaultNnTracking(){
		LvqModelSettingsCli retval = *this;
		retval.TrackProjectionQuality = LvqModelSettingsCli().TrackProjectionQuality;
		return retval;
	}
}