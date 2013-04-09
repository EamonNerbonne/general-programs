#include "StdAfx.h"
#include "LvqModelSettingsCli.h"
#include "LvqDatasetCli.h"
#include "utils.h"
namespace LvqLibCli {
	LvqModelSettingsRaw LvqModelSettingsCli::ToNativeSettings() {

		LvqModelSettingsRaw nativeSettings = { (::LvqModelType)ModelType, Dimensionality, PrototypesPerClass, Ppca, RandomInitialBorders
			, neiP, scP,noKP, neiB, LocallyNormalize, NGu, NGi, Popt, Bcov, LrRaw, LrPp, wGMu, SlowK, MuOffset, LR0, LrScaleP, LrScaleB, LrScaleBad, decay, iterScaleFactor
			, ParamsSeed, InstanceSeed, NoNnErrorRateTracking, ParallelModels };
		return nativeSettings;
	}
	String^ LvqModelSettingsCli::ToShorthand() { return Canonicalize().toShorthandRaw(); }

	String^ LvqModelSettingsCli::toShorthandRaw() {
		return ModelType.ToString()
			+ (Dimensionality != LvqModelSettingsCli().Dimensionality ? "[" + Dimensionality + "]" : "") + "-" + PrototypesPerClass + ","
			+ (Ppca ? "Ppca,":"") + (RandomInitialBorders ? "RandomInitialBorders,":"") + (neiP ? "neiP," : "") + (scP?"scP,":"")+ (noKP ? "noKP," : "") + (neiB ? "neiB," : "")
			+ (LocallyNormalize ? "LocallyNormalize," : "") + (NGu ? "NGu," : "") + (NGi ? "NGi," : "") + (Popt ? "Popt," : "") + (Bcov ? "Bcov," : "") + (wGMu ? "wGMu," : "") + (SlowK ? "SlowK," : "")
			+ (NoNnErrorRateTracking ? "NoNnErrorRateTracking," : "") + (LrRaw ? "LrRaw," : "") + (LrPp ? "LrPp," : "")
			+ (LR0==0.0 && LrScaleP==0.0 && LrScaleB==0.0 ?"" : "lr" + LR0.ToString("r") + "," + "lrP" + LrScaleP.ToString("r") + "," + (LrScaleB==0.0 ? "" : "lrB" + LrScaleB.ToString("r") + ","))
			+ (LrScaleBad != LvqModelSettingsCli().LrScaleBad ? "lrX" + LrScaleBad.ToString("r") + ",":"") + (MuOffset ==  LvqModelSettingsCli().MuOffset ? "" : "mu" + MuOffset.ToString("r") + ",")
			+ (decay ==  LvqModelSettingsCli().decay ? "" : "d" + decay.ToString("r") + ",")
			+ (iterScaleFactor ==  LvqModelSettingsCli().iterScaleFactor ? "" : "is" + iterScaleFactor.ToString("r") + ",")
			+ (ParamsSeed != LvqModelSettingsCli().ParamsSeed ||InstanceSeed != LvqModelSettingsCli().InstanceSeed
				? "[" +  ( ParamsSeed != LvqModelSettingsCli().ParamsSeed ? ParamsSeed.ToString("x"):"") 
				+ "," +  (InstanceSeed != LvqModelSettingsCli().InstanceSeed?InstanceSeed.ToString("x"):"") + "]"
			: "" )
			+ (ParallelModels!=LvqModelSettingsCli().ParallelModels?"^" + ParallelModels: "")
			+ (FoldOffset!=LvqModelSettingsCli().FoldOffset?"_" + FoldOffset: "");
	}

	LvqModelSettingsCli LvqModelSettingsCli::Canonicalize() {
		bool isG2mVariant = ModelType == LvqModelType::G2m ||  ModelType == LvqModelType::Gpq;
		bool isGgmVariant = ModelType == LvqModelType::Ggm ||  ModelType == LvqModelType::Fgm;
		bool isLgmVariant = ModelType == LvqModelType::Lgm ||  ModelType == LvqModelType::Lpq;
		bool hasB = isG2mVariant || isGgmVariant;
		bool hasGlobalP = hasB || ModelType == LvqModelType::Gm;
		LvqModelSettingsCli retval = *this;

		
		retval.noKP = retval.noKP && hasGlobalP;
		//retval.Popt =  retval.Popt && hasGlobalP;
		//retval.Ppca = retval.Ppca && (hasGlobalP || );
		retval.NoNnErrorRateTracking = retval.NoNnErrorRateTracking && hasGlobalP;
		retval.NGu = retval.NGu && PrototypesPerClass > 1 && hasGlobalP;
		retval.NGi =retval.NGi && PrototypesPerClass > 1;
		retval.neiB = retval.neiB && isG2mVariant;
		retval.neiP = retval.neiP && !isGgmVariant;
		retval.scP = retval.scP && hasGlobalP;
		retval.wGMu = retval.wGMu && isG2mVariant;
		retval.LocallyNormalize = retval.LocallyNormalize && (isLgmVariant || isG2mVariant);
		retval.RandomInitialBorders = retval.RandomInitialBorders && hasB;
		retval.Bcov = retval.Bcov && hasB;
		if(!hasB) retval.LrScaleB=0.0;
		if(!isGgmVariant && ModelType != LvqModelType::Normal) retval.MuOffset = 0.0;
		
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

	LvqModelSettingsCli LvqModelSettingsCli::WithIterScale(double newIterScaleFactor) {
		LvqModelSettingsCli retval = *this;
		retval.iterScaleFactor = newIterScaleFactor;
		return retval;
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithDecay(double newDecay) {
		LvqModelSettingsCli retval = *this;
		retval.decay = newDecay;
		return retval;
	}

	LvqModelSettingsCli LvqModelSettingsCli::WithCanonicalizedDefaults(){
		return WithLr(LvqModelSettingsCli().LR0, LvqModelSettingsCli().LrScaleP, LvqModelSettingsCli().LrScaleB)
			.WithSeeds(LvqModelSettingsCli().ParamsSeed, LvqModelSettingsCli().InstanceSeed)
			.WithIterScale(LvqModelSettingsCli().iterScaleFactor) 
			.WithDecay(LvqModelSettingsCli().decay)
			.Canonicalize();
	}
	LvqModelSettingsCli LvqModelSettingsCli::WithLrAndDecay(double lr0, double lrP, double lrB, double decay, double iterScaleFactor){
		return WithLr(lr0, lrP, lrB)
			.WithIterScale(iterScaleFactor) 
			.WithDecay(decay);
	}

	LvqModelSettingsCli LvqModelSettingsCli::WithDefaultNnTracking(){
		LvqModelSettingsCli retval = *this;
		retval.NoNnErrorRateTracking = LvqModelSettingsCli().NoNnErrorRateTracking;
		return retval;
	}

	static double costfactors[9][4] = 
		{	
			{9.9, 10.2, 2.11, 16.9},//g2m
			{7,8,2.54,700},//g2m ngu
			{10.7,9.1,2.18,441},//ggm
			{20,12.5,2.19,777},//ggm ngu
			{5.3,7.1,2.37,5},//gm
			{6,6.5,2.57,500},//gm ngu
			{104,210,0.062,-1010},//gpq
			{48.7,2090.101,-515},//gpq ngu
			{7.4,14.8,8,-40},//lgm
		};

	double LvqModelSettingsCli::EstimateCost(int classes, int dataDims) {
		bool ngu = NGu && PrototypesPerClass >1;
		double* currfactors	=
			ModelType==LvqModelType::G2m && !ngu ? costfactors[0]
		:	ModelType==LvqModelType::G2m && ngu ? costfactors[1]
		:	ModelType==LvqModelType::Ggm && !ngu ? costfactors[2] 
		:	ModelType==LvqModelType::Ggm && ngu || ModelType==LvqModelType::Fgm ? costfactors[3] //unreasonable for Fgm, but whatever
		:	ModelType==LvqModelType::Gm && !ngu ? costfactors[4]
		:	ModelType==LvqModelType::Gm && ngu ? costfactors[5]
		:	ModelType==LvqModelType::Gpq && !ngu ? costfactors[6]
		:	ModelType==LvqModelType::Gpq && ngu ? costfactors[7]
		:	costfactors[8]//Lgm, Lpq
			;

		return ((classes*PrototypesPerClass + currfactors[0])*(dataDims + currfactors[1])*currfactors[2] + currfactors[3])*0.001;
	}

	 int LvqModelSettingsCli::ActiveRefinementCount() { return (int)RandomInitialBorders  + (int)NGu  + (int)NGi  + (int)Ppca  + (int)Popt  + (int)Bcov + (int)LrRaw + (int)LrPp + (int)wGMu  + (int)NoNnErrorRateTracking + (int)SlowK  + (int)neiP  + (int)scP  + (int)noKP  + (int)neiB  + (int)LocallyNormalize;}
	 int LvqModelSettingsCli::LikelyRefinementRanking() { 
		 LvqModelSettingsCli copy = *this;
		 if(ModelType == LvqModelType::Lgm || ModelType == LvqModelType::Lpq)
			 copy.Popt = !copy.Popt;
		 else
			copy.Ppca = !copy.Ppca;
		 if(ModelType == LvqModelType::Ggm)
			 copy.SlowK = !copy.SlowK;
		 if(ModelType == LvqModelType::Gpq)
			 copy.scP = !copy.scP;
		 if(PrototypesPerClass >1)
			 copy.NGi = !copy.NGi;
		 //maybe: GM: noKP, GM SlowK?
		 return copy.ActiveRefinementCount();

	 }
}