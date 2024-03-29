#include "stdafx.h"
#include "LvqLib.h"
#include "CreateDataset.h"
#include "LvqDataset.h"
#include "LvqModel.h"
#include "LvqProjectionModel.h"
#include<algorithm>
using std::mt19937;
using std::vector;
using std::copy;
using std::wstring;
using std::transform;
#define DBG(X) (std::cout<< #X <<": "<<(X)<<"\n")

extern "C" LvqDataset * CreateDatasetRaw(
    unsigned rngInstSeed, int dimCount, int pointCount, int classCount, LvqFloat * data, int* labels) {
    mt19937  rngInst(rngInstSeed);
    Matrix_NN points(dimCount, pointCount);
    points = Map<Matrix_NN>(data, dimCount, pointCount);
    //std::cout<<"pc:"<<pointCount<<std::endl;
    VectorXi labelVec((VectorXi::Index)pointCount);
    for (int i = 0;i < pointCount;++i) labelVec(i) = labels[i];//VS11 workaround
    //VectorXi::Map(labels,pointCount);
    //vector<int> vLabels(labels,labels+pointCount);
    //DBG(labelVec);
    LvqDataset* dataset = new LvqDataset(points, labelVec, classCount);
    dataset->shufflePoints(rngInst);
    return dataset;
}

extern "C" LvqDataset * CreateGaussianClouds(
    unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount, double meansep) {
    mt19937 rngParams(rngParamSeed), rngInst(rngInstSeed);
    LvqDataset* dataset = CreateDataset::ConstructGaussianClouds(rngParams, rngInst, dimCount, classCount, pointCount / classCount, meansep);
    dataset->shufflePoints(rngInst);
    return dataset;
}

extern "C" LvqDataset * CreateStarDataset(
    unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, int classCount,
    int starDims, int numStarTails, double starMeanSep, double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma) {
    mt19937 rngParams(rngParamSeed), rngInst(rngInstSeed);
    LvqDataset* dataset = CreateDataset::ConstructStarDataset(rngParams, rngInst, dimCount, starDims, numStarTails, classCount, pointCount / classCount, starMeanSep, starClassRelOffset, randomlyRotate, noiseSigma, globalNoiseMaxSigma);
    dataset->shufflePoints(rngInst);
    return dataset;
}

extern "C" LvqDataset * CreateDatasetFold(LvqDataset * underlying, int fold, int foldCount, bool isTestFold) {
    //std::cout<< "Creating dataset "<< (isTestFold?"test":"") <<" fold #"<<fold<<"/"<<foldCount<<"\n";
    return underlying->Extract(isTestFold ? underlying->GetTestSubset(fold, foldCount) : underlying->GetTrainingSubset(fold, foldCount));
}

extern "C"  void CreatePointCloud(unsigned rngParamSeed, unsigned rngInstSeed, int dimCount, int pointCount, double meansep, LvqFloat * target) {
    Map<Matrix_NN> tgt(target, dimCount, pointCount);
    tgt = CreateDataset::MakePointCloud(as_lvalue(mt19937(rngParamSeed)), as_lvalue(mt19937(rngInstSeed)), dimCount, pointCount, meansep);
}

extern "C" void FreeDataset(LvqDataset * dataset) { delete dataset; }
extern "C" size_t MemAllocEstimateDataset(LvqDataset * dataset) { return dataset->MemAllocEstimate(); }

extern "C" void ExtendAndNormalize(LvqDataset * dataset, LvqDataset * testdataset, bool extend, bool normalize, bool normalizeByScaling) {
    if (extend) {
        dataset->ExtendByCorrelations();
        testdataset->ExtendByCorrelations();
    }
    if (normalize) {
        auto parameters = dataset->NormalizationParameters();
        dataset->ApplyNormalization(parameters, normalizeByScaling);
        testdataset->ApplyNormalization(parameters, normalizeByScaling);
    }
}

extern "C" double NearestNeighborSplitPcaErrorRate(LvqDataset const* trainingSet, LvqDataset const* testSet) {
    return trainingSet->NearestNeighborPcaErrorRate(*testSet);
}
extern "C" double NearestNeighborSplitRawErrorRate(LvqDataset const* trainingSet, LvqDataset const* testSet) {
    return trainingSet->NearestNeighborErrorRate(*testSet);
}

extern "C" DataShape GetDataShape(LvqDataset const* dataset) {
    DataShape shape;
    shape.classCount = dataset->classCount();
    shape.dimCount = (int)dataset->dimCount();
    shape.pointCount = (int)dataset->pointCount();
    return shape;
}

extern "C" void GetPointLabels(LvqDataset const* dataset, int* pointLabels) {
    VectorXi::Map(pointLabels, dataset->pointCount()) = dataset->getPointLabels();
}

extern "C" LvqModel * CreateLvqModel(LvqModelSettingsRaw rawSettings, LvqDataset const* dataset, int modelFold) {
    vector<int> protoDistrib;
    for (int i = 0;i < dataset->classCount();++i)
        protoDistrib.push_back(rawSettings.PrototypesPerClass);

    LvqModelSettings initSettings(
        LvqModelSettings::LvqModelType(rawSettings.ModelType),
        as_lvalue(mt19937(rawSettings.ParamsSeed + modelFold)),
        as_lvalue(mt19937(rawSettings.InstanceSeed + modelFold)),
        protoDistrib,
        dataset
    );
    initSettings.Ppca = rawSettings.Ppca;
    initSettings.RandomInitialBorders = rawSettings.RandomInitialBorders;
    initSettings.RuntimeSettings.NoNnErrorRateTracking = rawSettings.NoNnErrorRateTracking;
    initSettings.RuntimeSettings.neiP = rawSettings.neiP;
    initSettings.RuntimeSettings.scP = rawSettings.scP;
    initSettings.RuntimeSettings.noKP = rawSettings.noKP;
    initSettings.RuntimeSettings.neiB = rawSettings.neiB;
    initSettings.RuntimeSettings.LocallyNormalize = rawSettings.LocallyNormalize;
    initSettings.NGu = rawSettings.NGu;
    initSettings.NGi = rawSettings.NGi;
    initSettings.Popt = rawSettings.Popt;
    initSettings.Bcov = rawSettings.Bcov;
    initSettings.RuntimeSettings.wGMu = rawSettings.wGMu;
    initSettings.RuntimeSettings.SlowK = rawSettings.SlowK;
    initSettings.Dimensionality = rawSettings.Dimensionality;
    initSettings.RuntimeSettings.MuOffset = rawSettings.MuOffset;
    initSettings.RuntimeSettings.LrScaleP = rawSettings.LrRaw ? rawSettings.LrScaleP / rawSettings.LR0 : rawSettings.LrScaleP;
    initSettings.RuntimeSettings.LrScaleB = rawSettings.LrRaw ? rawSettings.LrScaleB / rawSettings.LR0 : rawSettings.LrScaleB;
    initSettings.RuntimeSettings.LR0 = rawSettings.LR0;
    initSettings.LrPp = rawSettings.LrPp;
    initSettings.RuntimeSettings.LrScaleBad = rawSettings.LrScaleBad;
    initSettings.decay = rawSettings.decay;
    initSettings.iterScaleFactor = rawSettings.iterScaleFactor;
    return ConstructLvqModel(initSettings);
}

extern "C" LvqModel * CloneLvqModel(LvqModel const* model) { return model->clone(); }
extern "C" void CopyLvqModel(LvqModel const* src, LvqModel * dest) {
    src->CopyTo(*dest);
}
extern "C" size_t MemAllocEstimateModel(LvqModel const* model) { return model->MemAllocEstimate(); }
void FreeModel(LvqModel* model) { delete model; }

extern "C" DataShape GetModelShape(LvqModel const* model) {
    DataShape shape;
    shape.classCount = model->ClassCount();
    shape.dimCount = model->Dimensions();
    shape.pointCount = (int)model->GetPrototypeLabels().size();
    return shape;
}

extern "C" void ProjectPrototypes(LvqModel const* model, LvqFloat * pointData) {
    auto projModel = dynamic_cast<LvqProjectionModel const*>(model);
    Matrix_2N pProtos = projModel->GetProjectedPrototypes();
    Map<Matrix_2N> mappedData(pointData, LVQ_LOW_DIM_SPACE, pProtos.cols());
    mappedData = pProtos;//this is perhaps unnecessary copying
}

extern "C" void ProjectPoints(LvqModel const* model, LvqDataset const* dataset, LvqFloat * pointData) {
    auto projModel = dynamic_cast<LvqProjectionModel const*>(model);
    Map<Matrix_2N> mappedData(pointData, LVQ_LOW_DIM_SPACE, dataset->pointCount());
    mappedData = dataset->ProjectPoints(*projModel);//this is perhaps unnecessary copying
}

extern "C" void GetProjectionMatrix(LvqModel const* model, LvqFloat * matrixDataTgt) {//2 * dimCount
    Matrix_P const& mat = dynamic_cast<LvqProjectionModel const*>(model)->projectionMatrix();
    copy(mat.data(), mat.data() + mat.size(), matrixDataTgt);
}

extern "C" void ClassBoundaries(LvqModel const* model, double x0, double x1, double y0, double y1, int xCols, int yRows, unsigned char* imageData) {
    auto projModel = dynamic_cast<LvqProjectionModel const*>(model);
    LvqProjectionModel::ClassDiagramT image(yRows, xCols);
    projModel->ClassBoundaryDiagram(x0, x1, y0, y1, image);
    Map<LvqProjectionModel::ClassDiagramT> mappedImage(imageData, image.rows(), image.cols());
    mappedImage = image;//this is perhaps unnecessary copying
}

extern "C" void NormalizeProjectionRotation(LvqModel * model) {
    auto projModel = dynamic_cast<LvqProjectionModel*>(model);
    if (projModel != nullptr)
        projModel->normalizeProjectionRotation();
}


extern "C" void GetPrototypeLabels(LvqModel const* model, int* protoLabels) {
    vector<int> labels = model->GetPrototypeLabels();
    copy(labels.begin(), labels.end(), protoLabels);
}
extern "C" int GetEpochsTrained(LvqModel const* model) {
    return model->epochsTrained;
}
extern "C" double GetMeanUnscaledLearningRate(LvqModel const* model) {
    return model->meanUnscaledLearningRate();
}
extern "C" bool IsProjectionModel(LvqModel const* model) {
    return nullptr != dynamic_cast<LvqProjectionModel const*>(model);
}
extern "C" void ResetLearningRate(LvqModel * model) {
    model->resetLearningRate();
}
extern "C" void GetTrainingStatNames(LvqModel const* model, void (*addNames)(void* context, size_t statsCount, wchar_t const** names), void* context) {
    vector<wstring> names = model->TrainingStatNames();
    vector<wchar_t const*> mappedNames(names.size());
    transform(names.begin(), names.end(), mappedNames.begin(), [](wstring const& str) -> wchar_t const* {return &str[0];});
    addNames(context, names.size(), &mappedNames[0]);
}

extern "C" void TrainModel(LvqDataset const* trainingset, LvqDataset const* testset, LvqModel * model, int epochsToDo, void (*addStat)(void* context, size_t statsCount, LvqStat * stats), void* context, int* labelOrderSink, bool sortedTrain) {
    LvqModel::Statistics stats;
    trainingset->TrainModel(epochsToDo, *model, addStat ? &stats : nullptr, testset, labelOrderSink, sortedTrain);
    if (addStat)
        while (!stats.empty()) {
            addStat(context, stats.front().size(), &stats.front()[0]);
            stats.pop();
        }
}

extern "C" void ComputeModelStats(LvqDataset const* trainingset, LvqDataset const* testset, LvqModel const* model, void (*addStat)(void* context, size_t statsCount, LvqStat * stats), void* context) {
    LvqModel::Statistics stats;
    model->AddTrainingStat(stats, trainingset, testset);
    while (!stats.empty()) {
        addStat(context, stats.front().size(), &stats.front()[0]);
        stats.pop();
    }
}

extern "C"  CostAndErrorRate ComputeCostAndErrorRate(LvqDataset const* dataset, LvqModel const* model) {
    auto stats = dataset->ComputeCostAndErrorRate(*model);

    CostAndErrorRate result = { stats.meanCost(), stats.errorRate() };
    return result;
}

extern "C" void CreateExtendedDataset(LvqDataset const* dataset, LvqDataset const* testdataset, LvqDataset const* extendDataset, LvqDataset const* extendTestdataset, LvqModel const* model, LvqDataset * *newTraining, LvqDataset * *newTest) {
    auto extendedDataset = dataset->ExtendUsingModel(testdataset, extendDataset, extendTestdataset, *model);
    *newTraining = extendedDataset.first;
    *newTest = extendedDataset.second;
}


extern "C" void ChooseSaneDefaultLr(LvqModelSettingsRaw * settings) {
    if (settings->ModelType == GmModelType) {
        settings->LR0 = 0.002;
        settings->LrScaleP = 2.0;
    }
    else if (settings->ModelType == GgmModelType || settings->ModelType == FgmModelType) {
        settings->LR0 = 0.03;
        settings->LrScaleP = 0.05;
        settings->LrScaleB = 4.0;
    }
    else {//lgm, g2m
        settings->LR0 = 0.01;
        settings->LrScaleP = 0.4;
        settings->LrScaleB = 0.006;
    }
}