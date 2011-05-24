#pragma once
#include "LvqModelSettings.h"
#include "LvqConstants.h"
using namespace System;
namespace LvqLibCli {

	public enum class LvqModelType {
		LgmModelType = LvqModelSettings::LgmModelType,
		GmModelType = LvqModelSettings::GmModelType,
		G2mModelType = LvqModelSettings::G2mModelType,
		GgmModelType = LvqModelSettings::GgmModelType,
	};

	ref class LvqDatasetCli;

	public ref class LvqModelSettingsCli
	{
	public:
		LvqModelType ModelType;
		unsigned ParamsSeed, InstanceSeed;
		int PrototypesPerClass;
		bool RandomInitialProjection;
		bool RandomInitialBorders;
		bool TrackProjectionQuality;
		bool NormalizeProjection, NormalizeBoundaries, GloballyNormalize;
		bool NgUpdateProtos, NgInitializeProtos, UpdatePointsWithoutB;
		bool SlowStartLrBad;
		int Dimensionality;
		double LrScaleP, LrScaleB, LR0, LrScaleBad;

		LvqModelSettingsCli()
			: ModelType(LvqModelType::GgmModelType)
			, ParamsSeed(37)
			, InstanceSeed(42)
			, PrototypesPerClass(1)
			, RandomInitialProjection(true)
			, RandomInitialBorders(false)
			, TrackProjectionQuality(true)
			, NormalizeProjection(true)
			, NormalizeBoundaries(true)
			, GloballyNormalize(true)
			, NgUpdateProtos(false)
			, NgInitializeProtos(true)
			, UpdatePointsWithoutB(false)
			, SlowStartLrBad(false)
			, Dimensionality(0)
			, LrScaleP(LVQ_LrScaleP)
			, LrScaleB(LVQ_LrScaleB)
			, LR0(LVQ_LR0)
			, LrScaleBad(LVQ_LrScaleBad)
		{ }
		LvqModelSettingsCli^ Copy();
		LvqModelSettings ToNativeSettings(LvqDatasetCli^ dataset, int datasetFold);

		String^ ToShorthand() {
			return ModelType.ToString()
				+ (ModelType == LvqModelType::LgmModelType ? "[" + Dimensionality + "]" : "") + ","
				+ PrototypesPerClass + ","
				+ "rP" + (RandomInitialProjection ? "+" : "") + ","
				+ (ModelType == LvqModelType::GgmModelType || ModelType==LvqModelType::G2mModelType ? "rB" + (RandomInitialBorders ? "+" : "") + "," : "")
				+ "nP" + (NormalizeProjection ? "+" : "") + ","
				+ (ModelType == LvqModelType::G2mModelType ? "nB" + (NormalizeBoundaries ? "+" : "") + "," : "")
				+ (ModelType == LvqModelType::G2mModelType && NormalizeBoundaries || NormalizeProjection ? "gn" + (GloballyNormalize ? "+" : "") + "," : "")
				+ (ModelType != LvqModelType::LgmModelType ? "NG" + (NgUpdateProtos ? "+" : "") + "," : "")
				+ (PrototypesPerClass > 1 ? "NGi" + (NgInitializeProtos ? "+" : "") + "," : "")
				+ (ModelType == LvqModelType::G2mModelType ? "noB" + (UpdatePointsWithoutB ? "+" : "") + "," : "")
				+ "lr0" + LR0 + ","
				+ "lrP" + LrScaleP + ","
				+ "lrB" + LrScaleB + ","
				+ "lrX" + LrScaleBad + ","
				+ (SlowStartLrBad ? "!" : "")
				+ "[" + ParamsSeed + ":" + InstanceSeed + "]";
		}
	};
}