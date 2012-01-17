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
			+ (ModelType == LvqModelType::Lgm||ModelType == LvqModelType::Lpq || Dimensionality != 2 ? "[" + Dimensionality + "]" : (!NoNnErrorRateTracking ? "+" : "")) + ","
			+ PrototypesPerClass + ","
			+ (Ppca != LvqModelSettingsCli().Ppca ? "rP" + (!Ppca ? "+" : "") + ",":"")
			+ (RandomInitialBorders != LvqModelSettingsCli().RandomInitialBorders && (ModelType == LvqModelType::Ggm || ModelType==LvqModelType::G2m) ? "rB" + (RandomInitialBorders ? "+" : "") + "," : "")
			+ (unnormedP != LvqModelSettingsCli().unnormedP ? "nP" + (!unnormedP ? "+" : "") + ",":"")
			+ (unnormedB != LvqModelSettingsCli().unnormedB && ModelType == LvqModelType::G2m ? "nB" + (!unnormedB ? "+" : "") + "," : "")
			+ (LocallyNormalize !=LvqModelSettingsCli().LocallyNormalize && (ModelType == LvqModelType::G2m && !unnormedB || ModelType == LvqModelType::Lgm && !unnormedP||ModelType == LvqModelType::Lpq && !unnormedP) ? "gn" + (!LocallyNormalize ? "+" : "") + "," : "")
			+ (NGu != LvqModelSettingsCli().NGu && ModelType != LvqModelType::Lgm && ModelType != LvqModelType::Lpq && PrototypesPerClass > 1 ? "NG" + (NGu ? "+" : "") + "," : "")
			+ (NGi != LvqModelSettingsCli().NGi && PrototypesPerClass > 1 ? "NGi" + (NGi ? "+" : "") + "," : "")
			+ (Popt != LvqModelSettingsCli().Popt && (ModelType != LvqModelType::Lgm && ModelType != LvqModelType::Lpq) ? "Pi" + (Popt ? "+" : "") + "," : "")
			+ (Bcov != LvqModelSettingsCli().Bcov && (ModelType != LvqModelType::Lpq && ModelType != LvqModelType::Lgm && ModelType != LvqModelType::Gm) ? "Bi" + (Bcov ? "+" : "") + "," : "")
			+ (wGMu != LvqModelSettingsCli().wGMu && ModelType == LvqModelType::G2m ? "noB" + (wGMu ? "+" : "") + "," : "")
			+ (MuOffset == 0.0?"": "mu" + MuOffset.ToString("r") + ",")
			+ (LrScaleBad != LvqModelSettingsCli().LrScaleBad ? "lrX" + LrScaleBad.ToString("r") + ",":"")
			+ (SlowK ? "!" : "")
			+ (LR0==LVQ_LR0  ?  "": "lr0" + LR0.ToString("r") + ","	)
			+ (LrScaleP==LVQ_LrScaleP  ?  ""  :   "lrP" + LrScaleP.ToString("r") + ","	)
			+ (LrScaleB==LVQ_LrScaleB  || ModelType ==  LvqModelType::Lgm || ModelType == LvqModelType::Lpq || ModelType == LvqModelType::Gm ?  "": "lrB" + LrScaleB.ToString("r") + ",")
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