#pragma once
#include "LvqModelSettings.h"
#include "LvqConstants.h"
using namespace System;
namespace LvqLibCli {

	public enum class LvqModelType {
		Lgm = LvqModelSettings::LgmModelType,
		Gm = LvqModelSettings::GmModelType,
		G2m = LvqModelSettings::G2mModelType,
		Ggm = LvqModelSettings::GgmModelType,
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
		bool NgUpdateProtos, NgInitializeProtos, UpdatePointsWithoutB;
		bool SlowStartLrBad;
		double  LR0, LrScaleP, LrScaleB, LrScaleBad;
		unsigned ParamsSeed, InstanceSeed;
		bool TrackProjectionQuality;
		int ParallelModels;

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
			, UpdatePointsWithoutB(false)

			, SlowStartLrBad(false)
			, LR0(LVQ_LR0)
			, LrScaleP(LVQ_LrScaleP)
			, LrScaleB(LVQ_LrScaleB)
			, LrScaleBad(LVQ_LrScaleBad)

			, ParamsSeed(37)
			, InstanceSeed(42)
			, TrackProjectionQuality(true)

			, ParallelModels(10) //only used in C#!
		{ }
		LvqModelSettingsCli^ Copy();
		LvqModelSettingsCli^ WithChanges(LvqModelType type, int protos, unsigned rngParams, unsigned rngIter);
		LvqModelSettings ToNativeSettings(LvqDatasetCli^ dataset, int datasetFold);
		String^ ToShorthand();
	};
}