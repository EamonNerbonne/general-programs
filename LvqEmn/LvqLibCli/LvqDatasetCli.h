#pragma once
using namespace System;
using namespace System::Collections::Generic;
#include <random>
//#include "LvqTypedefs.h"
#include "LvqLib.h"
class LvqDataset;

namespace LvqLibCli {
    ref class LvqModelCli;
    public ref class LvqDatasetCli
    {
        typedef array<System::Windows::Media::Color> ColorArray;
        initonly LvqDatasetCli^ original;
        array<GcManualPtr<LvqDataset>^ >^ datasets;
        GcAutoPtr<vector<DataShape> >^ datashape;
        array<String^>^ classNames;
        String^ label;
        LvqModelCli^ lastModel;
        initonly LvqDatasetCli ^testSet;
        ColorArray^ colors;
        LvqDatasetCli(String^label, ColorArray^ colors,array<String^>^ classes, array<GcManualPtr<LvqDataset>^ >^ newDatasets, array<GcManualPtr<LvqDataset>^ >^ newTestDatasets, LvqDatasetCli^ parent);
        static LvqDatasetCli^ Unfolder(String^label, int folds,bool extend, bool normalizeDims, bool normalizeByScaling, ColorArray^ colors, array<String^>^ classes, LvqDataset * newDataset);
        DataShape FoldShape(int fold) { return (*datashape->get())[fold%datasets->Length]; }
    public:
        bool IsFolded() {return datasets->Length>1;}
        int Folds() {return datasets->Length;}
        bool HasTestSet() {return testSet != nullptr;}
        int PointCount(int fold);
        LvqDataset const * GetTrainingDataset(int fold) {return *(datasets[fold%datasets->Length]);}
        LvqDataset const * GetTestDataset(int fold) {return testSet==nullptr?nullptr:testSet->GetTrainingDataset(fold);} 
        array<int>^ ClassLabels(int fold);
        //array<LvqFloat,2>^ RawPoints();
        property ColorArray^ ClassColors { ColorArray^ get(){return colors;} void set(ColorArray^ newcolors){colors=newcolors;} }
        property array<String^>^  ClassNames { array<String^>^  get(){return classNames;} }
        property int ClassCount {int get();}
        property int Dimensions {int get();}
        property String^ DatasetLabel {String^ get(){return label;}}
        property LvqModelCli^ LastModel { LvqModelCli^ get(){return lastModel;} void set(LvqModelCli^ newval){lastModel = newval;} }

        property LvqDatasetCli^ TestSet { LvqDatasetCli^ get(){return testSet;} }

        Tuple<double,double> ^ GetPcaNnErrorRate();

        static LvqDatasetCli^ ConstructFromArray(String^ label,int folds, bool extend, bool normalizeDims, bool normalizeByScaling,ColorArray^ colors, array<String^>^ classes ,unsigned rngInstSeed, array<LvqFloat,2>^ points, array<int>^ pointLabels, array<LvqFloat,2>^ testpoints, array<int>^ testpointLabels);
        static LvqDatasetCli^ ConstructGaussianClouds(String^ label,int folds, bool extend, bool normalizeDims, bool normalizeByScaling,ColorArray^ colors, array<String^>^ classes, unsigned rngParamsSeed, unsigned rngInstSeed, int dims,  int pointsPerClass, double meansep);
        static LvqDatasetCli^ ConstructStarDataset(String^ label,int folds, bool extend, bool normalizeDims, bool normalizeByScaling,ColorArray^ colors, array<String^>^ classes, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform, double noiseSigma, double globalNoiseMaxSigma);

        LvqDatasetCli^ ConstructByModelExtension(array<LvqModelCli^>^ models);
    };
};

