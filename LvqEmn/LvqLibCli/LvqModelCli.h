#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataSet.h"
#include "LvqDataSetCli.h"

namespace LvqLibCli {

	public ref class LvqModelCli
	{
		CAutoNativePtr<AbstractProjectionLvqModel> model;
		CAutoNativePtr<AbstractProjectionLvqModel> modelCopy;
		CAutoNativePtr<boost::mt19937> rnd;
		System::Object^ backupSync;
		System::Object^ mainSync;
		size_t nativeAllocEstimate;
		int dims,classCount,protosPerClass,modelType;
		void BackupModel() {
			AbstractProjectionLvqModel* newCopy = dynamic_cast<AbstractProjectionLvqModel*>(model->clone());
			msclr::lock l(backupSync);
			modelCopy = newCopy;
		}

		!LvqModelCli();

		void ReleaseModels();
		void AddPressure(size_t size);
		void RemovePressure(size_t size);
	public:
		LvqModelCli(Func<unsigned int>^ rngSeed,int dims, int classCount, int protosPerClass,  int modelType);
		void Init(LvqDataSetCli^ trainingSet);

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
