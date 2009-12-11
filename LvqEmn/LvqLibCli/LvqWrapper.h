#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataSet.h"

namespace LVQCppCli {

	public ref class LvqWrapper
	{
		CAutoNativePtr<LvqDataSet> dataset;
		CAutoNativePtr<AbstractProjectionLvqModel> model;
		CAutoNativePtr<AbstractProjectionLvqModel> modelCopy;
		CAutoNativePtr<boost::mt19937> rnd;
		System::Object^ backupSync;
		System::Object^ mainSync;

		void BackupModel() {
			AbstractProjectionLvqModel* newCopy = dynamic_cast<AbstractProjectionLvqModel*>(model->clone());
			msclr::lock l(backupSync);
			modelCopy = newCopy;
		}

	public:
		LvqWrapper(array<double,2>^ points, array<int>^ pointLabels, int classCount, int protosPerDistrib, bool useGsm);
		property Object^ UpdateSyncObject { Object ^ get(){return mainSync;} }
		double ErrorRate();
		array<double,2>^ CurrentProjection();
		array<int,2>^ ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows);
		void TrainEpoch(int epochsToDo); 
	};
}
