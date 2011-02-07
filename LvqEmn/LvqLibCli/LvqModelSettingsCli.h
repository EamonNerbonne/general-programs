#pragma once
#include "LvqModelSettings.h"
#include "LvqConstants.h"
using namespace System;
namespace LvqLibCli {

	public enum class LvqModelType {
		GmModelType = LvqModelSettings::GmModelType,
		GsmModelType = LvqModelSettings::GsmModelType,
		G2mModelType = LvqModelSettings::G2mModelType,
		GmmModelType = LvqModelSettings::GmmModelType,
	};

	ref class LvqDatasetCli;

	public ref class LvqModelSettingsCli
	{
	public:
		LvqModelType ModelType;
		unsigned RngParamsSeed, RngIterSeed;
		int PrototypesPerClass;
		bool RandomInitialProjection;
		bool RandomInitialBorders;
		bool TrackProjectionQuality;
		bool NormalizeProjection, NormalizeBoundaries, GloballyNormalize;
		bool NgUpdateProtos, UpdatePointsWithoutB;
		int Dimensionality;
		double LrScaleP, LrScaleB, LR0, LrScaleBad;

		LvqModelSettingsCli()
			: ModelType(LvqModelType::GmmModelType)
			, RngParamsSeed(37)
			, RngIterSeed(42)
			, PrototypesPerClass(1)
			, RandomInitialProjection(true)
			, RandomInitialBorders(false)
			, TrackProjectionQuality(false)
			, NormalizeProjection(true)
			, NormalizeBoundaries(false)
			, GloballyNormalize(true)
			, NgUpdateProtos(false)
			, UpdatePointsWithoutB(false)
			, Dimensionality(0)
			, LrScaleP(LVQ_LrScaleP)
			, LrScaleB(LVQ_LrScaleB)
			, LR0(LVQ_LR0)
			, LrScaleBad(LVQ_LrScaleBad)
		{ }

		LvqModelSettings ToNativeSettings(LvqDatasetCli^ dataset, int datasetFold);
	};
}