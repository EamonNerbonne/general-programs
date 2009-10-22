#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataSet.h"

namespace LVQCppCli {

	public ref class LvqWrapper
	{
		CAutoNativePtr<LvqDataSet> dataset;
		CAutoNativePtr<G2mLvqModel> model;
		CAutoNativePtr<boost::mt19937> rnd;
	public:
		LvqWrapper(array<double,2>^ points, array<int>^ pointLabels, int classCount, int protosPerDistrib);

		double ErrorRate();
		array<double,2>^ CurrentProjection();
		void TrainEpoch(int epochsToDo) {
			dataset->TrainModel(epochsToDo,  *rnd, *model);
		}
	};
}
