#include "stdafx.h"
#include "LvqLib.h"
#include "CreateDataset.h"
#include "LvqDataset.h"
#include "LvqModel.h"
#include "LvqProjectionModel.h"
#include<algorithm>
using boost::mt19937;
using std::vector;
using std::copy;
using std::wstring;
using std::transform;

extern"C" LvqDataset* CreateDatasetRaw(
	unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, int foldCount, //TODO:remove foldCount
	LvqFloat* data, int*labels) {
		mt19937 rngParams(rngParamSeed), rngInst(rngInstSeed);
		Matrix_NN points(dimCount,pointCount);
		points = Map<Matrix_NN>(data,dimCount,pointCount);
		vector<int> vLabels(labels,labels+pointCount);

		LvqDataset* dataset= new LvqDataset(points,vLabels,classCount);
		dataset->shufflePoints(rngInst);
		return dataset;
}
extern"C" LvqDataset* CreateGaussianClouds(
	unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, int foldCount, 
	double meansep) {
		mt19937 rngParams(rngParamSeed), rngInst(rngInstSeed);
		LvqDataset* dataset= CreateDataset::ConstructGaussianClouds(rngParams,rngInst,dimCount,classCount,pointCount/classCount,meansep);
		dataset->shufflePoints(rngInst);
		return dataset;
}
extern"C" LvqDataset* CreateStarDataset(
	unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, int foldCount, 
	int starDims, int numStarTails, double starMeanSep, double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma){
		mt19937 rngParams(rngParamSeed), rngInst(rngInstSeed);
		LvqDataset* dataset= CreateDataset::ConstructStarDataset(rngParams,rngInst,dimCount,starDims,numStarTails,classCount,pointCount/classCount,starMeanSep,starClassRelOffset,randomlyRotate,noiseSigma,globalNoiseMaxSigma);
		dataset->shufflePoints(rngInst);
		return dataset;
}

extern"C"  void CreatePointCloud(unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, double meansep, double detScalePower, LvqFloat* target){
	Map<Matrix_NN> tgt(target,dimCount,pointCount);
	tgt=CreateDataset::MakePointCloud(as_lvalue(mt19937(rngParamSeed)),as_lvalue(mt19937(rngInstSeed)),dimCount,pointCount,meansep,detScalePower);
}

extern"C" void FreeDataset(LvqDataset* dataset) {delete dataset;}
extern"C" size_t MemAllocEstimateDataset(LvqDataset* dataset) {return dataset->MemAllocEstimate();}

extern"C" void ExtendAndNormalize(LvqDataset * dataset, bool extend, bool normalize) {
	if(extend) dataset->ExtendByCorrelations();
	if(normalize) dataset->NormalizeDimensions();
}

extern"C" double NearestNeighborSplitPcaErrorRate(LvqDataset const * trainingSet, LvqDataset const * testSet) {
	return trainingSet->NearestNeighborPcaErrorRate(trainingSet->GetEverythingSubset(), testSet, testSet->GetEverythingSubset());
}
extern"C" double NearestNeighborSplitRawErrorRate(LvqDataset const * trainingSet, LvqDataset const * testSet) {
	return trainingSet->NearestNeighborErrorRate(trainingSet->GetEverythingSubset(), testSet, testSet->GetEverythingSubset());
}


extern"C" double NearestNeighborXvalPcaErrorRate(LvqDataset const * trainingSet, int fold,int foldCount){
	return trainingSet->NearestNeighborPcaErrorRate(trainingSet->GetTrainingSubset(fold,foldCount), trainingSet, trainingSet->GetTestSubset(fold, foldCount));
}
extern"C" double NearestNeighborXvalRawErrorRate(LvqDataset const * trainingSet, int fold,int foldCount){
	return trainingSet->NearestNeighborErrorRate(trainingSet->GetTrainingSubset(fold,foldCount), trainingSet, trainingSet->GetTestSubset(fold, foldCount));
}

extern"C" int GetTrainingSubsetSize(LvqDataset const * trainingSet, int fold, int foldCount) { return trainingSet->GetTrainingSubsetSize(fold,foldCount); }
extern"C" int GetTestSubsetSize(LvqDataset const * trainingSet, int fold, int foldCount){ return trainingSet->GetTestSubsetSize(fold,foldCount); }

extern"C" DataShape GetDataShape(LvqDataset const * dataset){
	DataShape shape;
	shape.classCount = dataset->getClassCount();
	shape.dimCount = dataset->dimensions();
	shape.pointCount = dataset->getPointCount();
	return shape;
}

extern"C" void GetPointLabels(LvqDataset const * dataset, int* pointLabels) {
	vector<int> labels = dataset->getPointLabels();
	copy(labels.begin(),labels.end(),pointLabels);
}

extern"C" LvqModel* CreateLvqModel(LvqModelSettingsRaw rawSettings, LvqDataset const* dataset, int modelFold, int foldCount){
	vector<int> protoDistrib;
	for(int i=0;i<dataset->getClassCount();++i)
		protoDistrib.push_back(rawSettings.PrototypesPerClass);

	LvqModelSettings initSettings(
		LvqModelSettings::LvqModelType(rawSettings.ModelType), 
		as_lvalue(mt19937(rawSettings.ParamsSeed+modelFold)), 
		as_lvalue(mt19937(rawSettings.InstanceSeed+modelFold)), 
		protoDistrib, 
		dataset,
		dataset->GetTrainingSubset(modelFold, foldCount)
		);
	initSettings.RandomInitialProjection = rawSettings.RandomInitialProjection;
	initSettings.RandomInitialBorders =rawSettings. RandomInitialBorders;
	initSettings.RuntimeSettings.TrackProjectionQuality = rawSettings.TrackProjectionQuality;
	initSettings.RuntimeSettings.NormalizeProjection = rawSettings.NormalizeProjection;
	initSettings.RuntimeSettings.NormalizeBoundaries =rawSettings. NormalizeBoundaries;
	initSettings.RuntimeSettings.GloballyNormalize =rawSettings. GloballyNormalize;
	initSettings.NgUpdateProtos =rawSettings. NgUpdateProtos;
	initSettings.NgInitializeProtos =rawSettings. NgInitializeProtos;
	initSettings.ProjOptimalInit =rawSettings. ProjOptimalInit;
	initSettings.BLocalInit =rawSettings. BLocalInit;
	initSettings.RuntimeSettings.UpdatePointsWithoutB = rawSettings.UpdatePointsWithoutB;
	initSettings.RuntimeSettings.SlowStartLrBad = rawSettings.SlowStartLrBad;
	initSettings.Dimensionality =rawSettings. Dimensionality;
	initSettings.RuntimeSettings.LrScaleP = rawSettings.LrScaleP;
	initSettings.RuntimeSettings.LrScaleB = rawSettings.LrScaleB;
	initSettings.RuntimeSettings.LR0 =rawSettings. LR0;
	initSettings.RuntimeSettings.LrScaleBad =rawSettings. LrScaleBad;

	return ConstructLvqModel(initSettings);
}

extern"C" LvqModel* CloneLvqModel(LvqModel const * model){ return model->clone(); }
extern"C" void CopyLvqModel(LvqModel const * src,LvqModel * dest){
	src->CopyTo(*dest);
}
extern"C" size_t MemAllocEstimateModel(LvqModel const * model){ return model->MemAllocEstimate();}
void FreeModel(LvqModel* model){delete model;}

extern "C" DataShape GetModelShape(LvqModel const * model) {
	DataShape shape;
	shape.classCount = model->ClassCount();
	shape.dimCount = model->Dimensions();
	shape.pointCount = (int)model->GetPrototypeLabels().size();//TODO:efficiency
	return shape;
}

extern "C"void ProjectPrototypes(LvqModel const* model, LvqFloat* pointData){
	auto projModel=dynamic_cast<LvqProjectionModel const *>(model);
	Matrix_2N pProtos = projModel->GetProjectedPrototypes();
	Map<Matrix_2N> mappedData(pointData,LVQ_LOW_DIM_SPACE,pProtos.cols());
	mappedData = pProtos;//TODO:copying
}

extern "C" void ProjectPoints(LvqModel const* model, LvqDataset const * dataset, LvqFloat* pointData) {
	auto projModel=dynamic_cast<LvqProjectionModel const *>(model);
	Matrix_2N pPoints = dataset->ProjectPoints(projModel);
	Map<Matrix_2N> mappedData(pointData,LVQ_LOW_DIM_SPACE,pPoints.cols());
	mappedData = pPoints;//TODO:copying
}

extern "C" void GetProjectionMatrix(LvqModel const* model, LvqFloat* matrixDataTgt){//2 * dimCount
	Matrix_P const & mat = dynamic_cast<LvqProjectionModel const*>(model)->projectionMatrix();
	copy(mat.data(), mat.data()+mat.size(), matrixDataTgt);
}

extern "C" void ClassBoundaries(LvqModel const* model, double x0, double x1, double y0, double y1, int xCols, int yRows, unsigned char* imageData) {
	auto projModel=dynamic_cast<LvqProjectionModel const *>(model);
	LvqProjectionModel::ClassDiagramT image(yRows,xCols);
	projModel->ClassBoundaryDiagram(x0,x1,y0,y1,image);
	Map<LvqProjectionModel::ClassDiagramT> mappedImage(imageData,image.rows(),image.cols());
	mappedImage = image;//TODO:copying
}

extern "C" void GetPrototypeLabels(LvqModel const* model, int* protoLabels){
	vector<int> labels = model->GetPrototypeLabels();
	copy(labels.begin(),labels.end(),protoLabels);
}
extern "C" int GetEpochsTrained(LvqModel const* model){
	return model->epochsTrained;
}
extern "C" double GetUnscaledLearningRate(LvqModel const* model){
	return model->unscaledLearningRate();
}
extern "C" bool IsProjectionModel(LvqModel const* model) {
	return nullptr != dynamic_cast<LvqProjectionModel const*>(model); 
}
extern "C" void ResetLearningRate(LvqModel * model) {
	model->resetLearningRate(); 
}
extern "C" void GetTrainingStatNames(LvqModel const* model, void (*addNames)(void* context, size_t statsCount, wchar_t const **names), void* context) {
	vector<wstring> names = model->TrainingStatNames();
	vector<wchar_t const *> mappedNames(names.size());
	transform(names.begin(),names.end(), mappedNames.begin(), [](wstring const &str) -> wchar_t const* {return &str[0];});
	addNames(context,names.size(),&mappedNames[0]);
}

extern "C" void TrainModel(LvqDataset const * trainingset, LvqDataset const * testset, int fold, int foldCount, LvqModel* model, int epochsToDo, void (*addStat)(void* context, size_t statsCount, LvqStat* stats), void* context, int* labelOrderSink, bool sortedTrain){
	LvqModel::Statistics stats;
	bool isSplitSet = foldCount==0;
	trainingset->TrainModel(epochsToDo,model,addStat?&stats:nullptr,trainingset->GetTrainingSubset(fold,foldCount),testset,isSplitSet?testset->GetEverythingSubset(): testset->GetTestSubset(fold,foldCount), labelOrderSink,sortedTrain);
	if(addStat)
		while(!stats.empty()){
			addStat(context,stats.front().size(),& stats.front()[0]);
			stats.pop();
		}
}

extern "C" void ComputeModelStats(LvqDataset const * trainingset, LvqDataset const * testset, int fold, int foldCount, LvqModel const * model, void (*addStat)(void* context, size_t statsCount, LvqStat* stats), void* context) {
	LvqModel::Statistics stats;
	bool isSplitSet = foldCount==0;
	model->AddTrainingStat(stats,trainingset,trainingset->GetTrainingSubset(fold,foldCount), testset, isSplitSet ? testset->GetEverythingSubset() : testset->GetTestSubset(fold,foldCount));
	while(!stats.empty()){
		addStat(context,stats.front().size(),& stats.front()[0]);
		stats.pop();
	}
}

extern "C"  CostAndErrorRate ComputeCostAndErrorRate(LvqDataset const * dataset, int fold,int foldCount, LvqModel const * model){
	auto stats = dataset->ComputeCostAndErrorRate(dataset->GetTrainingSubset(fold,foldCount),model);

	CostAndErrorRate result = { stats.meanCost(), stats.errorRate()};
	return result;
}
