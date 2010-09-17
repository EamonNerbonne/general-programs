#pragma once
#include "stdafx.h"
#include "PointSet.h"
#include "LvqDataset.h"
#include "LvqDatasetCli.h"
#include "LvqTrainingStatCli.h"
#include "SmartSum.h"

// array of model, modelCopy's.
//on training, train all models (parallel for)
//on projecting project first?
//on stats:

using namespace System;
namespace LvqLibCli {

	public ref class LvqModelCli {
		typedef GcAutoPtr<LvqModel> WrappedModel;
		typedef array<WrappedModel^> WrappedModelArray;
		int protosPerClass,modelType;
		String^ label;
		WrappedModelArray^ model;
		WrappedModelArray^ modelCopy;
		Object^ mainSync;
		LvqDatasetCli^ initSet;
		void BackupModel(); 
	public:

		property int ClassCount {int get(){return model[0]->get()->ClassCount();}}
		property int Dimensions {int get(){return model[0]->get()->Dimensions();}}
		property bool IsMultiModel {bool get(){return model->Length > 1;}}

		property String^ ModelLabel {String^ get(){return label;}}

		property LvqDatasetCli^ InitSet {LvqDatasetCli^ get(){return initSet;}}
		property array<LvqTrainingStatCli>^ TrainingStats {	array<LvqTrainingStatCli>^ get();}
		property array<String^>^ TrainingStatNames { array<String^>^ get();}

		void ResetLearningRate();
		

		LvqModelCli(String^ label, unsigned rngParamsSeed, unsigned rngInstSeed, int protosPerClass, int modelType,int parallelModels,LvqDatasetCli^ trainingSet);

		bool FitsDataShape(LvqDatasetCli^ dataset) {return dataset!=nullptr && dataset->ClassCount == this->ClassCount && dataset->Dimensions == this->Dimensions;}

		property Object^ UpdateSyncObject { Object ^ get(){return mainSync;} }
		//double ErrorRate(LvqDatasetCli^testSet);

		array<double,2>^ CurrentProjectionOf(LvqDatasetCli^ dataset);
		Tuple<array<double,2>^,array<int>^>^ PrototypePositions();
		array<int,2>^ ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows);

		ModelProjection CurrentProjectionAndPrototypes(LvqDatasetCli^ dataset);

		void Train(int epochsToDo,LvqDatasetCli^ trainingSet); 

		literal int G2M_TYPE = LvqModelInitSettings::G2mModelType;
		literal int GSM_TYPE = LvqModelInitSettings::GsmModelType;
		literal int GM_TYPE = LvqModelInitSettings::GmModelType;
	};
}
