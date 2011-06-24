#pragma once
extern "C" {

	typedef double LvqStat;
	typedef double LvqFloat;

	struct LvqDataset;
	struct LvqModel;
	struct DataShape { int pointCount, dimCount, classCount; };
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
		int ParallelModels;
	};

	LvqDataset* CreateDatasetRaw(
		unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, int foldCount,
		LvqFloat* data, int*labels);
	LvqDataset* CreateGaussianClouds(
		unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, int foldCount, 
		double meansep);
	LvqDataset* CreateStarDataset(
		unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, int foldCount, 
		int starDims, int numStarTails, double starMeanSep,	double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma);
	void FreeDataset(LvqDataset* dataset);
	size_t MemAllocEstimateDataset(LvqDataset* dataset);

	void ExtendAndNormalize(LvqDataset * dataset, bool extend, bool normalize);
	double NearestNeighborSplitPcaErrorRate(LvqDataset const * trainingSet, LvqDataset const * testSet);
	double NearestNeighborXvalPcaErrorRate(LvqDataset  const * trainingSet, int fold,int foldCount);
	int GetTrainingSubsetSize(LvqDataset const * trainingSet, int fold,int foldCount);
	int GetTestSubsetSize(LvqDataset const * trainingSet, int fold,int foldCount);

	DataShape GetDataShape(LvqDataset const * dataset);
	void GetPointLabels(LvqDataset const * dataset, int* pointLabels);


	LvqModel* CreateLvqModel(LvqModelSettingsRaw rawSettings, LvqDataset const* dataset, int fold,int foldCount);
	LvqModel* CloneLvqModel(LvqModel const * model);
	void CopyLvqModel(LvqModel const * src,LvqModel * dest);
	size_t MemAllocEstimateModel(LvqModel const * model);
	void FreeModel(LvqModel* model);

	DataShape GetModelShape(LvqModel const * model);
	void ProjectPrototypes(LvqModel const* model, LvqFloat* pointData);
	void ProjectPoints(LvqModel const* model, LvqDataset const * dataset, LvqFloat* pointData);
	void ClassBoundaries(LvqModel const* model, double x0, double x1, double y0, double y1, int xCols, int yRows, unsigned char* imageData);
	void GetPrototypeLabels(LvqModel const* model, int* protoLabels);
	int GetEpochsTrained(LvqModel const* model);
	double GetUnscaledLearningRate(LvqModel const* model);
	bool IsProjectionModel(LvqModel const* model);
	void ResetLearningRate(LvqModel * model); 
	void GetTrainingStatNames(LvqModel const* model, void (*addNames)(void* context, size_t statsCount, wchar_t const **names), void* context);



	void TrainModel(LvqDataset const * trainingset, LvqDataset const * testset, int fold,int foldCount, LvqModel* model, int epochsToDo, void (*addStat)(void* context, size_t statsCount, LvqStat* stats), void* context);
	void ComputeModelStats(LvqDataset const * trainingset, LvqDataset const * testset, int fold,int foldCount, LvqModel const * model, void (*addStat)(void* context, size_t statsCount, LvqStat* stats), void* context);
}