#pragma once
#include "LvqLib.h"
#include "LvqConstants.h"
using namespace System;
namespace LvqLibCli {

	public enum class LvqModelType {
		Lgm = LgmModelType,
		Gm = GmModelType,
		G2m = G2mModelType,
		Ggm = GgmModelType,
		Gpq = GpqModelType,
		Lpq = LpqModelType,
	};

	ref class LvqDatasetCli;

	public value class LvqModelSettingsCli : public IEquatable<LvqModelSettingsCli>
	{
		int _Dimensionality;
		int _PrototypesPerClass;
		int _ParallelModels, _FoldOffset;
		unsigned _ParamsSeed;
		Nullable<double> _LrScaleBad;
		Nullable<double> _decay;
		Nullable<double> _iterScaleFactor;
		Nullable<LvqModelType> _ModelType;
	public:
		property LvqModelType ModelType { LvqModelType get() { return _ModelType.HasValue?_ModelType.Value:LvqModelType::Ggm; } void set(LvqModelType val) { _ModelType = val==LvqModelType::Ggm?Nullable<LvqModelType>():Nullable<LvqModelType>(val); } }

		property int Dimensionality { int get() {return _Dimensionality + 2; } void set(int val) { _Dimensionality = val - 2; } }
		property int PrototypesPerClass  { int get() {return _PrototypesPerClass + 1; } void set(int val) { _PrototypesPerClass = val - 1; } }
		property unsigned ParamsSeed  { unsigned get() {return _ParamsSeed + 1; } void set(unsigned val) { _ParamsSeed = val - 1; } }
		property int ParallelModels  { int get() {return _ParallelModels + 10; } void set(int val) { _ParallelModels = val - 10; } }
		property int FoldOffset  { int get() {return _FoldOffset ; } void set(int val) { _FoldOffset = val; } }

		property double LrScaleBad { double get() { return _LrScaleBad.HasValue?_LrScaleBad.Value:LVQ_LrScaleBad; } void set(double val) { _LrScaleBad = val==LVQ_LrScaleBad?Nullable<double>(): Nullable<double>(val); } }
		property double decay { double get() { return _decay.HasValue?_decay.Value:1.0; } void set(double val) { _decay = val==1.0?Nullable<double>() : Nullable<double>(val); } }
		property double iterScaleFactor { double get() { return _iterScaleFactor.HasValue?_iterScaleFactor.Value: LVQ_ITERFACTOR_PERPROTO; } void set(double val) { _iterScaleFactor = val==LVQ_ITERFACTOR_PERPROTO?Nullable<double>() : Nullable<double>(val); } }

		double LR0, LrScaleP, LrScaleB;
		bool RandomInitialBorders, NGu, NGi, Ppca, Popt, Bcov, LrRaw, LrPp, wGMu, NoNnErrorRateTracking;
		bool SlowK, neiP, scP, noKP, neiB, LocallyNormalize;

		int ActiveRefinementCount();
		int LikelyRefinementRanking();

		unsigned  InstanceSeed;
		double MuOffset;

		static initonly LvqModelSettingsCli defaults;

		LvqModelSettingsCli WithSeeds(unsigned rngParams, unsigned rngIter);
		LvqModelSettingsCli WithChanges(LvqModelType type, int protos);
		LvqModelSettingsCli WithIterScale(double newIterScale);
		LvqModelSettingsCli WithDecay(double newDecay);
		LvqModelSettingsCli WithLr(double lr0, double lrP, double lrB);

		LvqModelSettingsCli WithCanonicalizedDefaults();
		LvqModelSettingsCli WithLrAndDecay(double lr0, double lrP, double lrB, double decay, double iterScaleFactor);

		LvqModelSettingsCli WithDefaultNnTracking();
		LvqModelSettingsRaw ToNativeSettings();
		double EstimateCost(int classes, int dataDims);
		String^ ToShorthand();
		LvqModelSettingsCli Canonicalize();

		static bool operator==(LvqModelSettingsCli a, LvqModelSettingsCli b) { return a.Equals(b); }
		static bool operator!=(LvqModelSettingsCli a, LvqModelSettingsCli b) { return !a.Equals(b); }
		virtual bool Equals(LvqModelSettingsCli other) { return Object::Equals(*this, other); }

	private:
		String^ toShorthandRaw();
		static LvqModelSettingsCli() {defaults = LvqModelSettingsCli(); }
	};
}