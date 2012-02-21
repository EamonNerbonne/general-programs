#pragma once
#include "LvqConstants.h"
extern "C" {

	typedef double LvqStat;
	typedef double LvqFloat;

	struct LvqDataset;
	struct LvqModel;
	struct DataShape { int pointCount, dimCount, classCount; };
	struct CostAndErrorRate { double meanCost, errorRate;};
	enum LvqModelType { AutoModelType, LgmModelType, GmModelType, G2mModelType, GgmModelType, GpqModelType, LpqModelType };

	struct LvqModelSettingsRaw {
		LvqModelType ModelType;
		int Dimensionality;
		int PrototypesPerClass;
		bool Ppca;
		bool RandomInitialBorders;
		bool neiP,scP, noKP, neiB, LocallyNormalize;
		bool NGu, NGi, Popt, Bcov, LrRaw, wGMu;
		bool SlowK;
		double MuOffset, LR0, LrScaleP, LrScaleB, LrScaleBad, decay, iterScaleFactor;
		unsigned ParamsSeed, InstanceSeed;
		bool NoNnErrorRateTracking;
		int ParallelModels; //only used in C#!
	};

	const LvqModelSettingsRaw defaultLvqModelSettings = { AutoModelType, 2, 1, false, false, false, false, false, false, false, false, false, false,false,false, false, false, 0.0, LVQ_LR0, LVQ_LrScaleP, LVQ_LrScaleB, LVQ_LrScaleBad, 1.,LVQ_ITERFACTOR_PERPROTO, 37, 42, false, 10	};

	__declspec(dllexport) LvqDataset* CreateDatasetRaw(
										    unsigned rngInstSeed, int dimCount, int pointCount, int classCount,
		LvqFloat* data, int*labels);
	__declspec(dllexport) LvqDataset* CreateGaussianClouds(
		unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount,
		double meansep);
	__declspec(dllexport) LvqDataset* CreateStarDataset(
		unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount,
		int starDims, int numStarTails, double starMeanSep,	double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma);

	__declspec(dllexport) LvqDataset* CreateDatasetFold(LvqDataset* underlying, int fold, int foldCount, bool isTestFold);

	__declspec(dllexport) void CreateExtendedDataset(LvqDataset const * dataset, LvqDataset const * testdataset, LvqModel const * model, LvqDataset** newTraining, LvqDataset** newTest);
	


	__declspec(dllexport) void CreatePointCloud(unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, double meansep, LvqFloat* target);
	__declspec(dllexport) void FreeDataset(LvqDataset* dataset);
	__declspec(dllexport) size_t MemAllocEstimateDataset(LvqDataset* dataset); 
	__declspec(dllexport) void ExtendAndNormalize(LvqDataset * dataset, LvqDataset * testdataset, bool extend, bool normalize, bool normalizeByScaling);
	__declspec(dllexport) double NearestNeighborSplitPcaErrorRate(LvqDataset const * trainingSet, LvqDataset const * testSet);
	__declspec(dllexport) double NearestNeighborSplitRawErrorRate(LvqDataset const * trainingSet, LvqDataset const * testSet);
	__declspec(dllexport) DataShape GetDataShape(LvqDataset const * dataset);
	__declspec(dllexport) void GetPointLabels(LvqDataset const * dataset, int* pointLabels);

	__declspec(dllexport) LvqModel* CreateLvqModel(LvqModelSettingsRaw rawSettings, LvqDataset const* dataset, int modelFold);
	__declspec(dllexport) LvqModel* CloneLvqModel(LvqModel const * model);
	__declspec(dllexport) void CopyLvqModel(LvqModel const * src,LvqModel * dest);
	__declspec(dllexport) size_t MemAllocEstimateModel(LvqModel const * model);
	__declspec(dllexport) void FreeModel(LvqModel* model);
	__declspec(dllexport) DataShape GetModelShape(LvqModel const * model);
	__declspec(dllexport) void ProjectPrototypes(LvqModel const* model, LvqFloat* pointData);
	__declspec(dllexport) void ProjectPoints(LvqModel const* model, LvqDataset const * dataset, LvqFloat* pointData);
	__declspec(dllexport) void GetProjectionMatrix(LvqModel const* model, LvqFloat* matrixDataTgt);//2 * dimCount
	__declspec(dllexport) void ClassBoundaries(LvqModel const * model, double x0, double x1, double y0, double y1, int xCols, int yRows, unsigned char* imageData);
	__declspec(dllexport) void NormalizeProjectionRotation(LvqModel * model);
	__declspec(dllexport) void GetPrototypeLabels(LvqModel const* model, int* protoLabels);
	__declspec(dllexport) int GetEpochsTrained(LvqModel const* model);
	__declspec(dllexport) double GetUnscaledLearningRate(LvqModel const* model);
	__declspec(dllexport) bool IsProjectionModel(LvqModel const* model);
	__declspec(dllexport) void ResetLearningRate(LvqModel * model); 
	__declspec(dllexport) void GetTrainingStatNames(LvqModel const* model, 
		void (*addNames)(void* context, size_t statsCount, wchar_t const **names), void* context);
	__declspec(dllexport) void TrainModel(LvqDataset const * trainingset, LvqDataset const * testset, LvqModel* model, int epochsToDo,
		void (*addStat)(void* context, size_t statsCount, LvqStat* stats), void* context, int* labelOrderSink, bool sortedTrain);
	__declspec(dllexport) void ComputeModelStats(LvqDataset const * trainingset, LvqDataset const * testset, LvqModel const * model,
		void (*addStat)(void* context, size_t statsCount, LvqStat* stats), void* context);
	__declspec(dllexport) CostAndErrorRate ComputeCostAndErrorRate(LvqDataset const * dataset, LvqModel const * model);
}