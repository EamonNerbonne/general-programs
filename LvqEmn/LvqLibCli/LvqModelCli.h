#pragma once
#include "PointSet.h"
#include "LvqTrainingStatCli.h"
#include "LvqModel.h"


// array of model, modelCopy's.
//on training, train all models (parallel for)
//on projecting project first?
//on stats:

namespace LvqLibCli {
	using namespace System;
	using namespace System::Collections::Generic;
	using namespace System::Collections::ObjectModel;

	ref class LvqModelSettingsCli;
	ref class LvqDatasetCli;

	public ref class LvqModelCli {
		typedef GcAutoPtr<LvqModel> WrappedModel;
		String^ label;
		WrappedModel^ model;
		WrappedModel^ modelCopy;
		List<LvqTrainingStatCli>^ stats;
		LvqDatasetCli^ initSet;
		int initDataFold;
		Object^trainSync;
		Object^copySync;

		void SinkStats(LvqModel::Statistics & nativeStats);
	public:
		property Object^ ReadSync {Object^ get(){return copySync;}}
		property int ClassCount {int get();}
		property int Dimensions {int get();}
		property double UnscaledLearningRate {double get();}
		property bool IsProjectionModel {bool get();}
		bool FitsDataShape(LvqDatasetCli^ dataset);

		property String^ ModelLabel {String^ get(){return label;}}
		property int InitDataFold {int get(){return initDataFold;}}
		property LvqDatasetCli^ InitDataset {LvqDatasetCli^ get(){return initSet;}}

		LvqModelCli(String^ label, LvqDatasetCli^ trainingSet,int datafold, LvqModelSettingsCli^ modelSettings, bool trackStats);

		array<LvqTrainingStatCli>^ GetTrainingStatsAfter(int statI);
		LvqTrainingStatCli EvaluateStats(LvqDatasetCli^ testset, int datafold);
		
		LvqTrainingStatCli GetTrainingStat(int statI);
		property int TrainingStatCount {int get();}
		property array<LvqTrainingStatCli>^ TrainingStats {array<LvqTrainingStatCli>^  get();}

		property array<String^>^ TrainingStatNames { array<String^>^ get();}

		void ResetLearningRate();

		MatrixContainer<unsigned char> ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows);

		ModelProjection CurrentProjectionAndPrototypes(LvqDatasetCli^ dataset);
		property array<int>^ PrototypeLabels {array<int>^ get(); }

		void Train(int epochsToDo,LvqDatasetCli^ trainingSet, int datafold); 
		void TrainUpto(int epochsToReach,LvqDatasetCli^ trainingSet, int datafold); 
	};
}
