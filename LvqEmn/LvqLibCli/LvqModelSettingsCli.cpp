#include "StdAfx.h"
#include "LvqModelSettingsCli.h"
#include "LvqDatasetCli.h"
#include "utils.h"
namespace LvqLibCli {
	LvqModelSettingsRaw LvqModelSettingsCli::ToNativeSettings() {

		LvqModelSettingsRaw nativeSettings = { (::LvqModelType)ModelType, Dimensionality, PrototypesPerClass, Ppca, RandomInitialBorders
			, unnormedP, noKP, unnormedB, LocallyNormalize, NGu, NGi, Popt, Bcov, wGMu, SlowK, MuOffset, LR0, LrScaleP, LrScaleB, LrScaleBad
			, ParamsSeed, InstanceSeed, NoNnErrorRateTracking, ParallelModels };
		return nativeSettings;
	}

	String^ LvqModelSettingsCli::ToShorthand() {
		bool isG2mVariant = ModelType == LvqModelType::G2m ||  ModelType == LvqModelType::Gpq;
		bool isLgmVariant = ModelType == LvqModelType::Lgm ||  ModelType == LvqModelType::Lpq;
		bool hasB = isG2mVariant || ModelType == LvqModelType::Ggm;
		bool hasGlobalP = hasB || ModelType == LvqModelType::Gm;
		return ModelType.ToString()
			+ (Dimensionality != LvqModelSettingsCli().Dimensionality ? "[" + Dimensionality + "]" : "") + "-" + PrototypesPerClass + ","
			+ (Ppca && hasGlobalP ? "Ppca,":"")
			+ (RandomInitialBorders && hasB ? "RandomInitialBorders,":"")
			+ ( unnormedP ? "unnormedP," : "")
			+ (noKP ? "noKP," : "")
			+ (unnormedB && isG2mVariant ? "unnormedB," : "")
			+ (LocallyNormalize && (isLgmVariant || isG2mVariant) ? "LocallyNormalize," : "")
			+ (NGu && PrototypesPerClass > 1 && hasGlobalP ? "NGu," : "")
			+ (NGi && PrototypesPerClass > 1 ? "NGi," : "")
			+ (Popt && hasGlobalP ? "Popt," : "")
			+ (Bcov && hasB ? "Bcov," : "")
			+ (wGMu && isG2mVariant ? "wGMu," : "")
			+ (SlowK ? "SlowK," : "")
			+ (NoNnErrorRateTracking ? "NoNnErrorRateTracking," : "")
			+ (LR0==0.0 && LrScaleP==0.0 && (LrScaleB==0.0||!hasB) ?"" :
				( "lr" + LR0.ToString("r") + ","	)
				+ ("lrP" + LrScaleP.ToString("r") + ","	)
				+ (!hasB ?  "": "lrB" + LrScaleB.ToString("r") + ",")
			)
			+ (LrScaleBad != LvqModelSettingsCli().LrScaleBad ? "lrX" + LrScaleBad.ToString("r") + ",":"")
			+ (MuOffset ==  LvqModelSettingsCli().MuOffset ?"": "mu" + MuOffset.ToString("r") + ",")
			+ (ParamsSeed != LvqModelSettingsCli().ParamsSeed ||InstanceSeed != LvqModelSettingsCli().InstanceSeed
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