#include "StdAfx.h"
#include "LvqModelSettingsCli.h"
#include "LvqDatasetCli.h"
#include "utils.h"
namespace LvqLibCli {
	LvqModelSettingsRaw LvqModelSettingsCli::ToNativeSettings() {

		LvqModelSettingsRaw nativeSettings = { (::LvqModelType)ModelType, Dimensionality, PrototypesPerClass, Ppca, RandomInitialBorders
			, unnormedP, unnormedB, LocallyNormalize, NGu, NGi, Popt, Bcov, wGMu, SlowK, MuOffset, LR0, LrScaleP, LrScaleB, LrScaleBad
			, ParamsSeed, InstanceSeed, NoNnErrorRateTracking, ParallelModels };
		return nativeSettings;
	}

	String^ LvqModelSettingsCli::ToShorthand() {
		return ModelType.ToString()
			+ (Dimensionality != LvqModelSettingsCli().Dimensionality ? "[" + Dimensionality + "]" : "") + "-" + PrototypesPerClass + ","
			
			+ (Ppca && ModelType != LvqModelType::Lgm && ModelType != LvqModelType::Lgm ? "Ppca,":"")
			+ (RandomInitialBorders && (ModelType == LvqModelType::G2m || ModelType == LvqModelType::Gpq || ModelType == LvqModelType::Ggm) ? "RandomInitialBorders,":"")
			+ ( unnormedP?"unnormedP,":"")
			+ (unnormedB && (ModelType == LvqModelType::G2m || ModelType == LvqModelType::Gpq) ? "unnormedB," : "")
			+ (LocallyNormalize && (ModelType == LvqModelType::G2m || ModelType == LvqModelType::Lgm || ModelType == LvqModelType::Lpq || ModelType == LvqModelType::Gpq) ? "LocallyNormalize," : "")
			+ (NGu && PrototypesPerClass > 1 && (ModelType == LvqModelType::G2m ||  ModelType == LvqModelType::Gpq || ModelType == LvqModelType::Ggm || ModelType == LvqModelType::Gm) ? "NGu," : "")
			+ (NGi && PrototypesPerClass > 1 ? "NGi," : "")
			+ (Popt && (ModelType == LvqModelType::G2m ||  ModelType == LvqModelType::Gpq || ModelType == LvqModelType::Ggm || ModelType == LvqModelType::Gm) ? "Popt," : "")
			+ (Bcov && (ModelType == LvqModelType::G2m ||  ModelType == LvqModelType::Gpq || ModelType == LvqModelType::Ggm) ? "Bcov," : "")
			+ (wGMu && (ModelType == LvqModelType::G2m ||  ModelType == LvqModelType::Gpq) ? "wGMu," : "")
			+ (SlowK ? "SlowK," : "")
			+ (NoNnErrorRateTracking ? "NoNnErrorRateTracking," : "")
			+ (LR0==LVQ_LR0  ?  "": "lr0" + LR0.ToString("r") + ","	)
			+ (LrScaleP==LVQ_LrScaleP  ?  ""  :   "lrP" + LrScaleP.ToString("r") + ","	)
			+ (LrScaleB==LVQ_LrScaleB  || ModelType ==  LvqModelType::Lgm || ModelType == LvqModelType::Lpq || ModelType == LvqModelType::Gm ?  "": "lrB" + LrScaleB.ToString("r") + ",")
			+ (LrScaleBad != LvqModelSettingsCli().LrScaleBad ? "lrX" + LrScaleBad.ToString("r") + ",":"")
			+ (MuOffset == 0.0?"": "mu" + MuOffset.ToString("r") + ",")
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
		retval.NoNnErrorRateTracking = LvqModelSettingsCli().NoNnErrorRateTracking;
		return retval;
	}
}