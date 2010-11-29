#pragma once
#include "PointSet.h"
#include "LvqTrainingStatCli.h"


// array of model, modelCopy's.
//on training, train all models (parallel for)
//on projecting project first?
//on stats:
class LvqModel;

namespace LvqLibCli {
	using namespace System;
	using namespace System::Collections::Generic;

	ref class LvqModelSettingsCli;
	ref class LvqDatasetCli;

	public ref class LvqModelCli {
		typedef GcAutoPtr<LvqModel> WrappedModel;
		String^ label;
		WrappedModel^ model;
		WrappedModel^ modelCopy;
		LvqDatasetCli^ initSet;
		int useDataFold;
		Object^trainSync;
		Object^copySync;

	public:
		property Object^ ReadSync {Object^ get(){return copySync;}}
		property int ClassCount {int get();}
		property int Dimensions {int get();}
		property double CurrentLearningRate {double get();}
		property bool IsProjectionModel {bool get();}
		bool FitsDataShape(LvqDatasetCli^ dataset);

		property String^ ModelLabel {String^ get(){return label;}}
		property int InitDataFold {int get(){return useDataFold;}}
		property LvqDatasetCli^ InitDataset {LvqDatasetCli^ get(){return initSet;}}

		LvqModelCli(String^ label, LvqDatasetCli^ trainingSet,int datafold, LvqModelSettingsCli^ modelSettings);

		array<LvqTrainingStatCli>^ GetTrainingStatsAfter(int statI);
		LvqTrainingStatCli GetTrainingStat(int statI);
		property int TrainingStatCount {int get();}

		property array<String^>^ TrainingStatNames { array<String^>^ get();}

		void ResetLearningRate();

		array<int,2>^ ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows);

		ModelProjection CurrentProjectionAndPrototypes(LvqDatasetCli^ dataset);

		void Train(int epochsToDo,LvqDatasetCli^ trainingSet, int datafold); 
		void TrainUpto(int epochsToReach,LvqDatasetCli^ trainingSet, int datafold); 
	};
}
