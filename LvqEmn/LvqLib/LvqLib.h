#pragma once
#include "LvqConstants.h"
extern "C" {

	typedef double LvqStat;
	typedef double LvqFloat;

	struct LvqDataset;
	struct LvqModel;
	struct DataShape { int pointCount, dimCount, classCount; };
	struct CostAndErrorRate { double meanCost, errorRate;};
	enum LvqModelType { AutoModelType, LgmModelType, GmModelType, G2mModelType, GgmModelType };

	struct LvqModelSettingsRaw {
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
		int ParallelModels; //only used in C#!
	};

	const LvqModelSettingsRaw defaultLvqModelSettings = { AutoModelType, 2, 1, true, false, true, true, true, false, false, false, false, LVQ_LR0, LVQ_LrScaleP, LVQ_LrScaleB, LVQ_LrScaleBad, 37, 42, true, 10	};

	__declspec(dllexport) LvqDataset* CreateDatasetRaw(
		unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, int foldCount,
		LvqFloat* data, int*labels);
	__declspec(dllexport) LvqDataset* CreateGaussianClouds(
		unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, int foldCount, 
		double meansep);
	__declspec(dllexport) LvqDataset* CreateStarDataset(
		unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, int foldCount, 
		int starDims, int numStarTails, double starMeanSep,	double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma);
	__declspec(dllexport) void FreeDataset(LvqDataset* dataset);
	__declspec(dllexport) size_t MemAllocEstimateDataset(LvqDataset* dataset);
	__declspec(dllexport) void ExtendAndNormalize(LvqDataset * dataset, bool extend, bool normalize);
	__declspec(dllexport) double NearestNeighborSplitPcaErrorRate(LvqDataset const * trainingSet, LvqDataset const * testSet);
	__declspec(dllexport) double NearestNeighborXvalPcaErrorRate(LvqDataset  const * trainingSet, int fold,int foldCount);
	__declspec(dllexport) int GetTrainingSubsetSize(LvqDataset const * trainingSet, int fold,int foldCount);
	__declspec(dllexport) int GetTestSubsetSize(LvqDataset const * trainingSet, int fold,int foldCount);
	__declspec(dllexport) DataShape GetDataShape(LvqDataset const * dataset);
	__declspec(dllexport) void GetPointLabels(LvqDataset const * dataset, int* pointLabels);

	__declspec(dllexport) LvqModel* CreateLvqModel(LvqModelSettingsRaw rawSettings, LvqDataset const* dataset, int fold,int foldCount);
	__declspec(dllexport) LvqModel* CloneLvqModel(LvqModel const * model);
	__declspec(dllexport) void CopyLvqModel(LvqModel const * src,LvqModel * dest);
	__declspec(dllexport) size_t MemAllocEstimateModel(LvqModel const * model);
	__declspec(dllexport) void FreeModel(LvqModel* model);
	__declspec(dllexport) DataShape GetModelShape(LvqModel const * model);
	__declspec(dllexport) void ProjectPrototypes(LvqModel const* model, LvqFloat* pointData);
	__declspec(dllexport) void ProjectPoints(LvqModel const* model, LvqDataset const * dataset, LvqFloat* pointData);
	__declspec(dllexport) void GetProjectionMatrix(LvqModel const* model, LvqFloat* matrixDataTgt);//2 * dimCount
	__declspec(dllexport) void ClassBoundaries(LvqModel const* model, double x0, double x1, double y0, double y1, int xCols, int yRows, unsigned char* imageData);
	__declspec(dllexport) void GetPrototypeLabels(LvqModel const* model, int* protoLabels);
	__declspec(dllexport) int GetEpochsTrained(LvqModel const* model);
	__declspec(dllexport) double GetUnscaledLearningRate(LvqModel const* model);
	__declspec(dllexport) bool IsProjectionModel(LvqModel const* model);
	__declspec(dllexport) void ResetLearningRate(LvqModel * model); 
	__declspec(dllexport) void GetTrainingStatNames(LvqModel const* model, 
		void (*addNames)(void* context, size_t statsCount, wchar_t const **names), void* context);
	__declspec(dllexport) void TrainModel(LvqDataset const * trainingset, LvqDataset const * testset, int fold,int foldCount, LvqModel* model, int epochsToDo,
		void (*addStat)(void* context, size_t statsCount, LvqStat* stats), void* context);
	__declspec(dllexport) void ComputeModelStats(LvqDataset const * trainingset, LvqDataset const * testset, int fold,int foldCount, LvqModel const * model,
		void (*addStat)(void* context, size_t statsCount, LvqStat* stats), void* context);
	__declspec(dllexport) CostAndErrorRate ComputeCostAndErrorRate(LvqDataset const * dataset, int fold,int foldCount, LvqModel const * model);
	

}