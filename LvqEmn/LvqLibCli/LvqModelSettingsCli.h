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
	};

	ref class LvqDatasetCli;

	public ref class LvqModelSettingsCli
	{
	public:
		LvqModelType ModelType;
		int Dimensionality;
		int PrototypesPerClass;
		bool RandomInitialProjection;
		bool RandomInitialBorders;
		bool NormalizeProjection, NormalizeBoundaries, GloballyNormalize;
		bool NgUpdateProtos, NgInitializeProtos, ProjOptimalInit,BLocalInit, UpdatePointsWithoutB;
		bool SlowStartLrBad;
		double  LR0, LrScaleP, LrScaleB, LrScaleBad;
		unsigned ParamsSeed, InstanceSeed;
		bool TrackProjectionQuality;
		int ParallelModels;

		static initonly LvqModelSettingsCli^ defaults;

		LvqModelSettingsCli()
			: ModelType(LvqModelType::Ggm)
			, Dimensionality(2)
			, PrototypesPerClass(1)
			, RandomInitialProjection(true)
			, RandomInitialBorders(false)

			, NormalizeProjection(true)
			, NormalizeBoundaries(true)
			, GloballyNormalize(true)

			, NgUpdateProtos(false)
			, NgInitializeProtos(false)
			, ProjOptimalInit(false)
			, BLocalInit(false)
			, UpdatePointsWithoutB(false)

			, SlowStartLrBad(false)
			, LR0(LVQ_LR0)
			, LrScaleP(LVQ_LrScaleP)
			, LrScaleB(LVQ_LrScaleB)
			, LrScaleBad(LVQ_LrScaleBad)

			, ParamsSeed(1)
			, InstanceSeed(0)
			, TrackProjectionQuality(true)

			, ParallelModels(10) //only used in C#!
		{ }

		LvqModelSettingsCli^ Copy();
		LvqModelSettingsCli^ WithChanges(LvqModelType type, int protos, unsigned rngParams, unsigned rngIter);
		LvqModelSettingsRaw ToNativeSettings();
		String^ ToShorthand();
	private:
		static LvqModelSettingsCli() {defaults = gcnew LvqModelSettingsCli();}

	};
}