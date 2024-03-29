#pragma once
#include "PointSet.h"
#include "LvqTrainingStatCli.h"
#include "LvqLib.h"


// array of model, modelCopy's.
//on training, train all models (parallel for)
//on projecting project first?
//on stats:

namespace LvqLibCli {
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Collections::ObjectModel;

    value class LvqModelSettingsCli;
    ref class LvqDatasetCli;

    public ref class LvqModelCli {
    public:
        typedef GcManualPtr<LvqModel> WrappedModel;
    private:
        String^ label;
        WrappedModel^ model;
        WrappedModel^ modelCopy;
        List<LvqTrainingStatCli>^ stats;
        LvqDatasetCli^ trainingSet;
        int dataFold, classCount, dimCount, protoCount;
        Object^ trainSync;
        Object^ copySync;

    public:
        property Object^ ReadSync {Object^ get() { return copySync; }}
        property int ClassCount {int get();}
        property int Dimensions {int get();}
        property double MeanUnscaledLearningRate {double get();}
        property bool IsProjectionModel {bool get();}
        bool FitsDataShape(LvqDatasetCli^ dataset);

        property String^ ModelLabel {String^ get() { return label; }}
        property int DataFold {int get() { return dataFold; }}
        property LvqDatasetCli^ TrainingSet {LvqDatasetCli^ get() { return trainingSet; }}

        LvqModelCli(String^ label, LvqDatasetCli^ trainingSet, int datafold, LvqModelSettingsCli^ modelSettings, bool trackStats);

        array<LvqTrainingStatCli>^ GetTrainingStatsAfter(int statI);
        LvqTrainingStatCli EvaluateStats();

        LvqTrainingStatCli GetTrainingStat(int statI);
        property int TrainingStatCount {int get();}
        property array<LvqTrainingStatCli>^ TrainingStats {array<LvqTrainingStatCli>^ get();}

        property array<String^>^ TrainingStatNames { array<String^>^ get();}

        void ResetLearningRate();

        MatrixContainer<unsigned char> ClassBoundaries(double x0, double x1, double y0, double y1, int xCols, int yRows);

        ModelProjection CurrentProjectionAndPrototypes(bool showTestEmbedding);
        property array<int>^ PrototypeLabels {array<int>^ get(); }

        array<int>^ Train(int epochsToDo, bool getOrder, bool sortedTrain);
        void TrainUpto(int epochsToReach);
    internal:
        Tuple<GcManualPtr<LvqDataset>^, GcManualPtr<LvqDataset>^>^ ExtendDatasetByProjection(LvqDatasetCli^ dataset, LvqDatasetCli^ toInclude, int datafold);
    };
}
