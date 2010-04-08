#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataSet.h"
#include "WrappingUtils.h"

namespace LvqLibCli {

	public ref class LvqDataSetCli
	{
		CAutoNativePtr<LvqDataSet> dataset;
		size_t nativeAllocEstimate;
		!LvqDataSetCli();

		LvqDataSetCli(LvqDataSet * newDataset);
public:
		LvqDataSet const * GetDataSet() {return dataset;}
		array<int>^ ClassLabels(){return cppToCli(dataset->trainPointLabels);}
		property int ClassCount {int get(){return dataset->classCount;}}

		static LvqDataSetCli ^ ConstructFromArray(array<double,2>^ points, array<int>^ pointLabels, int classCount);
		static LvqDataSetCli^ ConstructGaussianClouds(Func<unsigned int>^ rng, int dims, int classCount, int pointsPerClass, double meansep);
		static LvqDataSetCli^ ConstructStarDataset(Func<unsigned int>^ rng, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset);
	};
}

