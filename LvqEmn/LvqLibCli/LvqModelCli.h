#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataSet.h"
#include "LvqDataSetCli.h"

namespace LvqLibCli {

	public ref class LvqModelCli
	{
		int protosPerClass,modelType;
		String^ label;
		GcAutoPtr<AbstractProjectionLvqModel>^ model;
		GcAutoPtr<AbstractProjectionLvqModel>^ modelCopy;
		GcPlainPtr<boost::mt19937> rngParam, rngIter;
		Object^ backupSync;
		Object^ mainSync;
		void BackupModel() {
			AbstractProjectionLvqModel* newCopy = dynamic_cast<AbstractProjectionLvqModel*>(model->get()->clone());
			msclr::lock l(backupSync);
			modelCopy = GcPtr::Create(newCopy);
		}
		void Init(LvqDataSetCli^ trainingSet);
		LvqDataSetCli^ initSet;
	public:

		property int ClassCount {int get(){return model->get()->ClassCount();}}
		property int Dimensions {int get(){return model->get()->Dimensions();}}
		property String^ ModelLabel {String^ get(){return label;}}
		property LvqDataSetCli^ InitSet {LvqDataSetCli^ get(){return initSet;}}

		LvqModelCli(String^ label, unsigned rngParamsSeed, unsigned rngInstSeed, int protosPerClass, int modelType,LvqDataSetCli^ trainingSet);

		bool FitsDataShape(LvqDataSetCli^ dataset) {return dataset!=nullptr && dataset->ClassCount == this->ClassCount && dataset->Dimensions == this->Dimensions;}

		property Object^ UpdateSyncObject { Object ^ get(){return mainSync;} }
		double ErrorRate(LvqDataSetCli^testSet);
		array<double,2>^ CurrentProjectionOf(LvqDataSetCli^ dataset);
		
		Tuple<array<double,2>^,array<int>^>^ PrototypePositions();

		array<int,2>^ ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows);
		void Train(int epochsToDo,LvqDataSetCli^ trainingSet); 

		static const int G2M_TYPE =0;
		static const int GSM_TYPE =1;
		static const int GM_TYPE =2;
	};
}
