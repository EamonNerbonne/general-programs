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
		typedef array<WrappedModel^> WrappedModelArray;
		String^ label;
		WrappedModelArray^ model;
		WrappedModelArray^ modelCopy;
		Object^ mainSync;
		LvqDatasetCli^ initSet;
		void BackupModel(); 
		List<LvqTrainingStatCli>^ cachedTrainingStats;
	public:

		property int ClassCount {int get();}
		property int Dimensions {int get();}
		property int ModelCount {int get();}
		property bool IsMultiModel {bool get();}

		property String^ ModelLabel {String^ get(){return label;}}

		property LvqDatasetCli^ InitSet {LvqDatasetCli^ get(){return initSet;}}
		property IEnumerable<LvqTrainingStatCli>^ TrainingStats {	IEnumerable<LvqTrainingStatCli>^ get();}
		property array<String^>^ TrainingStatNames { array<String^>^ get();}

		void ResetLearningRate();
		
		LvqModelCli(String^ label, int parallelModels, LvqDatasetCli^ trainingSet, LvqModelSettingsCli^ modelSettings);

		bool FitsDataShape(LvqDatasetCli^ dataset);

		property Object^ UpdateSyncObject { Object ^ get(){return mainSync;} }

		array<double,2>^ CurrentProjectionOf(int modelIdx,LvqDatasetCli^ dataset);
		Tuple<array<double,2>^,array<int>^>^ PrototypePositions(int modelIdx);
		array<int,2>^ ClassBoundaries(int modelIdx, double x0, double x1, double y0, double y1,int xCols, int yRows);

		ModelProjection CurrentProjectionAndPrototypes(int modelIdx, LvqDatasetCli^ dataset);

		void Train(int epochsToDo,LvqDatasetCli^ trainingSet); 

	};
}
