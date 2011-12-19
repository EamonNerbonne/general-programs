#include "StdAfx.h"
#include "LvqModelSettingsCli.h"
#include "LvqDatasetCli.h"
#include "utils.h"
namespace LvqLibCli {
	LvqModelSettingsRaw LvqModelSettingsCli::ToNativeSettings() {

		LvqModelSettingsRaw nativeSettings = { (::LvqModelType)ModelType, Dimensionality, PrototypesPerClass, RandomInitialProjection, RandomInitialBorders, NormalizeProjection, NormalizeBoundaries, GloballyNormalize, NgUpdateProtos, NgInitializeProtos
			, ProjOptimalInit, BLocalInit, UpdatePointsWithoutB, SlowStartLrBad, MuOffset, LR0, LrScaleP, LrScaleB, LrScaleBad, ParamsSeed, InstanceSeed, TrackProjectionQuality, ParallelModels };
		return nativeSettings;
	}

	String^ LvqModelSettingsCli::ToShorthand() {
		return ModelType.ToString()
			+ (ModelType == LvqModelType::Lgm||ModelType == LvqModelType::Lpq || Dimensionality != 2 ? "[" + Dimensionality + "]" : (TrackProjectionQuality ? "+" : "")) + ","
			+ PrototypesPerClass + ","
			+ (RandomInitialProjection != LvqModelSettingsCli().RandomInitialProjection ? "rP" + (RandomInitialProjection ? "+" : "") + ",":"")
			+ (RandomInitialBorders != LvqModelSettingsCli().RandomInitialBorders && (ModelType == LvqModelType::Ggm || ModelType==LvqModelType::G2m) ? "rB" + (RandomInitialBorders ? "+" : "") + "," : "")
			+ (NormalizeProjection != LvqModelSettingsCli().NormalizeProjection ? "nP" + (NormalizeProjection ? "+" : "") + ",":"")
			+ (NormalizeBoundaries != LvqModelSettingsCli().NormalizeBoundaries && ModelType == LvqModelType::G2m ? "nB" + (NormalizeBoundaries ? "+" : "") + "," : "")
			+ (GloballyNormalize !=LvqModelSettingsCli().GloballyNormalize && (ModelType == LvqModelType::G2m && NormalizeBoundaries || ModelType == LvqModelType::Lgm && NormalizeProjection||ModelType == LvqModelType::Lpq && NormalizeProjection) ? "gn" + (GloballyNormalize ? "+" : "") + "," : "")
			+ (NgUpdateProtos != LvqModelSettingsCli().NgUpdateProtos && ModelType != LvqModelType::Lgm && ModelType != LvqModelType::Lpq && PrototypesPerClass > 1 ? "NG" + (NgUpdateProtos ? "+" : "") + "," : "")
			+ (NgInitializeProtos != LvqModelSettingsCli().NgInitializeProtos && PrototypesPerClass > 1 ? "NGi" + (NgInitializeProtos ? "+" : "") + "," : "")
			+ (ProjOptimalInit != LvqModelSettingsCli().ProjOptimalInit && (ModelType != LvqModelType::Lgm && ModelType != LvqModelType::Lpq) ? "Pi" + (ProjOptimalInit ? "+" : "") + "," : "")
			+ (BLocalInit != LvqModelSettingsCli().BLocalInit && (ModelType != LvqModelType::Lpq && ModelType != LvqModelType::Lgm && ModelType != LvqModelType::Gm) ? "Bi" + (BLocalInit ? "+" : "") + "," : "")
			+ (UpdatePointsWithoutB != LvqModelSettingsCli().UpdatePointsWithoutB && ModelType == LvqModelType::G2m ? "noB" + (UpdatePointsWithoutB ? "+" : "") + "," : "")
			+ (MuOffset == 0.0?"": "mu" + MuOffset.ToString("r") + ",")
			+ (LrScaleBad != LvqModelSettingsCli().LrScaleBad ? "lrX" + LrScaleBad.ToString("r") + ",":"")
			+ (SlowStartLrBad ? "!" : "")
			+ (LR0==LVQ_LR0 &&LrScaleP==LVQ_LrScaleP&&LrScaleB==LVQ_LrScaleB?"":
			"lr0" + LR0.ToString("r") + ","
			+ "lrP" + LrScaleP.ToString("r") + ","
			+ "lrB" + LrScaleB.ToString("r") + ",")
			+ ( ParamsSeed != LvqModelSettingsCli().ParamsSeed ||InstanceSeed != LvqModelSettingsCli().InstanceSeed
				? "[" +  ( ParamsSeed != LvqModelSettingsCli().ParamsSeed ? ParamsSeed.ToString("x"):"") 
				+ "," +  (InstanceSeed != LvqModelSettingsCli().InstanceSeed?InstanceSeed.ToString("x"):"") + "]"
			: "" )

			+ (ParallelModels!=LvqModelSettingsCli().ParallelModels?"^" + ParallelModels: "")
			+ (FoldOffset!=LvqModelSettingsCli().FoldOffset?"_" + FoldOffset: "");
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