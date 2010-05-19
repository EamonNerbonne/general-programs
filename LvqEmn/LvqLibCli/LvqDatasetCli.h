#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataSet.h"
#include "WrappingUtils.h"

namespace LvqLibCli {
	ref class LvqModelCli;
	public ref class LvqDataSetCli
	{
		GcAutoPtr<LvqDataSet> dataset;
		String^ label;
		LvqModelCli^ lastModel;

		LvqDataSetCli(String^label,LvqDataSet * newDataset);
public:
		LvqDataSet const * GetDataSet() {return dataset;}
		array<int>^ ClassLabels(){return cppToCli(dataset->trainPointLabels);}
		property int ClassCount {int get(){return dataset->classCount;}}
		property int Dimensions {int get(){return dataset->dimensions();}}
		property String^ DataSetLabel {String^ get(){return label;}}
		property LvqModelCli^ LastModel { LvqModelCli^ get(){return lastModel;} void set(LvqModelCli^ newval){lastModel = newval;}}

		static LvqDataSetCli^ ConstructFromArray(String^ label,array<double,2>^ points, array<int>^ pointLabels, int classCount);
		static LvqDataSetCli^ ConstructGaussianClouds(String^ label, Func<unsigned int>^ rngParamsSeed, Func<unsigned int>^ rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep);
		static LvqDataSetCli^ ConstructStarDataset(String^ label, Func<unsigned int>^  rngParamsSeed, Func<unsigned int>^ rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset);
	};
}

