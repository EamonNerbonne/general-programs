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
		{ }
		LvqModelSettingsCli^ Copy();
		LvqModelSettings ToNativeSettings(LvqDatasetCli^ dataset, int datasetFold);

		String^ ToShorthand() {
			return ModelType.ToString()
				+ (ModelType == LvqModelType::Lgm ? "[" + Dimensionality + "]" : (TrackProjectionQuality ? "+" : "")) + ","
				+ PrototypesPerClass + ","
				+ "rP" + (RandomInitialProjection ? "+" : "") + ","
				+ (ModelType == LvqModelType::Ggm || ModelType==LvqModelType::G2m ? "rB" + (RandomInitialBorders ? "+" : "") + "," : "")
				+ "nP" + (NormalizeProjection ? "+" : "") + ","
				+ (ModelType == LvqModelType::G2m ? "nB" + (NormalizeBoundaries ? "+" : "") + "," : "")
				+ (ModelType == LvqModelType::G2m && NormalizeBoundaries || NormalizeProjection ? "gn" + (GloballyNormalize ? "+" : "") + "," : "")
				+ (ModelType != LvqModelType::Lgm ? "NG" + (NgUpdateProtos ? "+" : "") + "," : "")
				+ (PrototypesPerClass > 1 ? "NGi" + (NgInitializeProtos ? "+" : "") + "," : "")
				+ (ModelType == LvqModelType::G2m ? "noB" + (UpdatePointsWithoutB ? "+" : "") + "," : "")
				+(LrScaleBad!=1.0? "lrX" + LrScaleBad + ",":"")
				+ (SlowStartLrBad ? "!" : "")
				+ "lr0" + LR0 + ","
				+ "lrP" + LrScaleP + ","
				+ "lrB" + LrScaleB + ","
				+ "[" + ParamsSeed.ToString("x") + "," + InstanceSeed.ToString("x") + "]";
		}
	};
}