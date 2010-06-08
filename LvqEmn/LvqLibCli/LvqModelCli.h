#pragma once
#include "stdafx.h"
#include "PointSet.h"
#include "LvqDataset.h"
#include "LvqDatasetCli.h"
#include "LvqTrainingStatCli.h"


using namespace System;
namespace LvqLibCli {

	public ref class LvqModelCli {
		typedef GcAutoPtr<AbstractLvqModel> WrappedModel;
		int protosPerClass,modelType;
		String^ label;
		WrappedModel^ model;
		WrappedModel^ modelCopy;
		GcPlainPtr<boost::mt19937> rngParam, rngIter;
		Object^ mainSync;
		void BackupModel() {
			WrappedModel^ newBackup = GcPtr::Create(model->get()->clone());
			msclr::lock l(newBackup);
			modelCopy = newBackup;
		}
		void Init(LvqDatasetCli^ trainingSet);
		LvqDatasetCli^ initSet;
	public:

		void ResetLearningRate() {msclr::lock l2(mainSync); model->get()->resetLearningRate();}
		property int ClassCount {int get(){return model->get()->ClassCount();}}
		property int Dimensions {int get(){return model->get()->Dimensions();}}
		property String^ ModelLabel {String^ get(){return label;}}
		property LvqDatasetCli^ InitSet {LvqDatasetCli^ get(){return initSet;}}
		property array<LvqTrainingStatCli>^ TrainingStats {
			array<LvqTrainingStatCli>^ get(){
				WrappedModel^ currentBackup = modelCopy;
				msclr::lock l2(currentBackup);
				array<LvqTrainingStatCli>^retval;
				cppToCli(currentBackup->get()->trainingStats,retval);
				return retval;
			}
		}

		int OtherStatCount() {WrappedModel^ backupCopy=modelCopy;msclr::lock l(backupCopy); return static_cast<int>( backupCopy->get()->otherStats().size());}

		LvqModelCli(String^ label, unsigned rngParamsSeed, unsigned rngInstSeed, int protosPerClass, int modelType,LvqDatasetCli^ trainingSet);

		bool FitsDataShape(LvqDatasetCli^ dataset) {return dataset!=nullptr && dataset->ClassCount == this->ClassCount && dataset->Dimensions == this->Dimensions;}

		property Object^ UpdateSyncObject { Object ^ get(){return mainSync;} }
		double ErrorRate(LvqDatasetCli^testSet);

		array<double,2>^ CurrentProjectionOf(LvqDatasetCli^ dataset);
		Tuple<array<double,2>^,array<int>^>^ PrototypePositions();
		array<int,2>^ ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows);

		ModelProjection CurrentProjectionAndPrototypes(LvqDatasetCli^ dataset);


		void Train(int epochsToDo,LvqDatasetCli^ trainingSet); 

		static const int G2M_TYPE =0;
		static const int GSM_TYPE =1;
		static const int GM_TYPE =2;
	};
}
