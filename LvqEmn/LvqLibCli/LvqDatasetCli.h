#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataSet.h"
#include "WrappingUtils.h"

namespace LVQCppCli {

	public ref class LvqDataSetCli
	{
		CAutoNativePtr<LvqDataSet> dataset;
		size_t nativeAllocEstimate;
		!LvqDataSetCli();

		LvqDataSetCli(LvqDataSet * newDataset);
public:
		LvqDataSet const * GetDataSet() {return dataset;}

		static LvqDataSetCli ^ ConstructFromArray(array<double,2>^ points, array<int>^ pointLabels, int classCount);
		static LvqDataSetCli^ MakeGaussianClouds(Func<unsigned int>^ rng,int dims, int pointCount, int classCount, double meansep);
	};
}

