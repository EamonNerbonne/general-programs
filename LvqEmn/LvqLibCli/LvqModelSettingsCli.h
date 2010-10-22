#pragma once
#include "LvqModelSettings.h"
using namespace System;
namespace LvqLibCli {

	public enum class LvqModelType {
		GmModelType = LvqModelSettings::GmModelType,
		GsmModelType = LvqModelSettings::GsmModelType,
		G2mModelType = LvqModelSettings::G2mModelType,
	};

	ref class LvqDatasetCli;

	public ref class LvqModelSettingsCli
	{
	public:
		//not passed by native constructor:
		bool RandomInitialProjection;
		bool RandomInitialBorders;
		bool TrackProjectionQuality;
		bool NormalizeProjection, NormalizeBoundaries, GloballyNormalize;
		bool NgUpdateProtos;
		//fields set by native constructor:
		LvqModelType ModelType;
		unsigned RngParamsSeed, RngIterSeed;
		int PrototypesPerClass;
		int Dimensionality;

		LvqModelSettingsCli()
			: ModelType(LvqModelType::G2mModelType)
			, RandomInitialProjection(true)
			, RandomInitialBorders(false)
			, TrackProjectionQuality(false)
			, RngParamsSeed(37)
			, RngIterSeed(42)
			, PrototypesPerClass(1)
		{ }

		LvqModelSettings ToNativeSettings(LvqDatasetCli^ dataset, int modelRank);
	};
}