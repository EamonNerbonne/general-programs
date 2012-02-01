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
	String^ LvqModelSettingsCli::ToShorthand() { return Canonicalize().toShorthandRaw(); }

	String^ LvqModelSettingsCli::toShorthandRaw() {
		return ModelType.ToString()
			+ (Dimensionality != LvqModelSettingsCli().Dimensionality ? "[" + Dimensionality + "]" : "") + "-" + PrototypesPerClass + ","
			+ (Ppca ? "Ppca,":"") + (RandomInitialBorders ? "RandomInitialBorders,":"") + (unnormedP ? "unnormedP," : "") + (noKP ? "noKP," : "") + (unnormedB ? "unnormedB," : "")
			+ (LocallyNormalize ? "LocallyNormalize," : "") + (NGu ? "NGu," : "") + (NGi ? "NGi," : "") + (Popt ? "Popt," : "") + (Bcov ? "Bcov," : "") + (wGMu ? "wGMu," : "") + (SlowK ? "SlowK," : "")
			+ (NoNnErrorRateTracking ? "NoNnErrorRateTracking," : "")
			+ (LR0==0.0 && LrScaleP==0.0 && LrScaleB==0.0 ?"" : "lr" + LR0.ToString("r") + "," + "lrP" + LrScaleP.ToString("r") + "," + (LrScaleB==0.0 ? "" : "lrB" + LrScaleB.ToString("r") + ","))
			+ (LrScaleBad != LvqModelSettingsCli().LrScaleBad ? "lrX" + LrScaleBad.ToString("r") + ",":"") + (MuOffset ==  LvqModelSettingsCli().MuOffset ? "" : "mu" + MuOffset.ToString("r") + ",")
			+ (ParamsSeed != LvqModelSettingsCli().ParamsSeed ||InstanceSeed != LvqModelSettingsCli().InstanceSeed
				? "[" +  ( ParamsSeed != LvqModelSettingsCli().ParamsSeed ? ParamsSeed.ToString("x"):"") 
				+ "," +  (InstanceSeed != LvqModelSettingsCli().InstanceSeed?InstanceSeed.ToString("x"):"") + "]"
			: "" )
			+ (ParallelModels!=LvqModelSettingsCli().ParallelModels?"^" + ParallelModels: "")
			+ (FoldOffset!=LvqModelSettingsCli().FoldOffset?"_" + FoldOffset: "");
	}

	LvqModelSettingsCli LvqModelSettingsCli::Canonicalize() {
		bool isG2mVariant = ModelType == LvqModelType::G2m ||  ModelType == LvqModelType::Gpq;
		bool isLgmVariant = ModelType == LvqModelType::Lgm ||  ModelType == LvqModelType::Lpq;
		bool hasB = isG2mVariant || ModelType == LvqModelType::Ggm;
		bool hasGlobalP = hasB || ModelType == LvqModelType::Gm;
		LvqModelSettingsCli retval = *this;

		
		retval.noKP = retval.noKP && hasGlobalP;
		retval.Popt =  retval.Popt && hasGlobalP;
		retval.Ppca = retval.Ppca && hasGlobalP;
		retval.NGu = retval.NGu && PrototypesPerClass > 1 && hasGlobalP;
		retval.NGi =retval.NGi && PrototypesPerClass > 1;
		retval.unnormedB = retval.unnormedB && isG2mVariant;
		retval.wGMu = retval.wGMu && isG2mVariant;
		retval.LocallyNormalize = retval.LocallyNormalize && (isLgmVariant || isG2mVariant);
		retval.RandomInitialBorders = retval.RandomInitialBorders && hasB;
		retval.Bcov = retval.Bcov && hasB;
		if(!hasB) retval.LrScaleB=0.0;
		if(ModelType != LvqModelType::Ggm) retval.MuOffset = 0.0;
		
		return retval;
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithChanges(LvqModelType type, int protos){
		LvqModelSettingsCli retval = *this;
		retval.ModelType = type;
		retval.PrototypesPerClass = protos;
		return retval;
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithSeeds(unsigned rngParams, unsigned rngIter){
		LvqModelSettingsCli retval = *this;
		retval.ParamsSeed = rngParams;
		retval.InstanceSeed = rngIter;
		return retval;
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithLr(double lr0, double lrP, double lrB) {
		LvqModelSettingsCli retval = *this;
		retval.LR0 = lr0;
		retval.LrScaleP = lrP;
		retval.LrScaleB = lrB;
		return retval;
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithDefaultLr(){
		return WithLr(LvqModelSettingsCli().LR0, LvqModelSettingsCli().LrScaleP, LvqModelSettingsCli().LrScaleB);
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithDefaultSeeds(){
		return WithSeeds(LvqModelSettingsCli().ParamsSeed, LvqModelSettingsCli().InstanceSeed);
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithDefaultNnTracking(){
		LvqModelSettingsCli retval = *this;
		retval.NoNnErrorRateTracking = LvqModelSettingsCli().NoNnErrorRateTracking;
		return retval;
	}
}