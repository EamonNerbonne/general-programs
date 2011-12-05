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

	public value class LvqModelSettingsCli
	{
		int _Dimensionality;
		int _PrototypesPerClass;
		int _ParallelModels, _FoldOffset;
		unsigned _ParamsSeed;
		bool _NonRandomInitialProjection;
		bool _NonNormalizeProjection, _NonNormalizeBoundaries, _NonGloballyNormalize;
		bool _NonTrackProjectionQuality;
		Nullable<double> _LR0, _LrScaleP, _LrScaleB, _LrScaleBad;
		Nullable<LvqModelType> _ModelType;
	public:
		property LvqModelType ModelType { LvqModelType get() { return _ModelType.HasValue?_ModelType.Value:LvqModelType::Ggm; } void set(LvqModelType val) { _ModelType = val==LvqModelType::Ggm?Nullable<LvqModelType>():Nullable<LvqModelType>(val); } }

		property int Dimensionality { int get() {return _Dimensionality + 2; } void set(int val) { _Dimensionality = val - 2; } }
		property int PrototypesPerClass  { int get() {return _PrototypesPerClass + 1; } void set(int val) { _PrototypesPerClass = val - 1; } }
		property unsigned ParamsSeed  { unsigned get() {return _ParamsSeed + 1; } void set(unsigned val) { _ParamsSeed = val - 1; } }
		property int ParallelModels  { int get() {return _ParallelModels + 10; } void set(int val) { _ParallelModels = val - 10; } }
		property int FoldOffset  { int get() {return _FoldOffset ; } void set(int val) { _FoldOffset = val; } }
		property bool RandomInitialProjection { bool get() {return !_NonRandomInitialProjection; } void set(bool val) { _NonRandomInitialProjection = !val; } }
		property bool NormalizeProjection { bool get() {return !_NonNormalizeProjection; } void set(bool val) { _NonNormalizeProjection = !val; } }
		property bool NormalizeBoundaries { bool get() {return !_NonNormalizeBoundaries; } void set(bool val) { _NonNormalizeBoundaries = !val; } }
		property bool GloballyNormalize { bool get() {return !_NonGloballyNormalize; } void set(bool val) { _NonGloballyNormalize = !val; } }
		property bool TrackProjectionQuality { bool get() {return !_NonTrackProjectionQuality; } void set(bool val) { _NonTrackProjectionQuality = !val; } }

		property double LR0 { double get() { return _LR0.HasValue?_LR0.Value:LVQ_LR0; } void set(double val) { _LR0 = val==LVQ_LR0?Nullable<double>(): Nullable<double>(val); } }
		property double LrScaleP { double get() { return _LrScaleP.HasValue?_LrScaleP.Value:LVQ_LrScaleP; } void set(double val) { _LrScaleP = val==LVQ_LrScaleP?Nullable<double>(): Nullable<double>(val); } }
		property double LrScaleB { double get() { return _LrScaleB.HasValue?_LrScaleB.Value:LVQ_LrScaleB; } void set(double val) { _LrScaleB =  val==LVQ_LrScaleB?Nullable<double>():Nullable<double>(val); } }
		property double LrScaleBad { double get() { return _LrScaleBad.HasValue?_LrScaleBad.Value:LVQ_LrScaleBad; } void set(double val) { _LrScaleBad = val==LVQ_LrScaleBad?Nullable<double>(): Nullable<double>(val); } }


		bool RandomInitialBorders;

		bool NgUpdateProtos, NgInitializeProtos, ProjOptimalInit,BLocalInit, UpdatePointsWithoutB;
		bool SlowStartLrBad;
		unsigned  InstanceSeed;

		static initonly LvqModelSettingsCli defaults;


//		LvqModelSettingsCli^ Copy();
		LvqModelSettingsCli WithChanges(LvqModelType type, int protos, unsigned rngParams, unsigned rngIter);
		LvqModelSettingsCli WithDefaultLr();
		LvqModelSettingsCli WithDefaultNnTracking();
		LvqModelSettingsRaw ToNativeSettings();
		String^ ToShorthand();

	private:
		static LvqModelSettingsCli() {defaults = LvqModelSettingsCli(); }
	};
}