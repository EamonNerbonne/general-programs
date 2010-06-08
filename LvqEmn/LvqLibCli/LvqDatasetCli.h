#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataset.h"
#include "WrappingUtils.h"

namespace LvqLibCli {
	ref class LvqModelCli;
	public ref class LvqDatasetCli
	{
		typedef array<System::Windows::Media::Color> ColorArray;

		GcAutoPtr<LvqDataset> dataset;
		String^ label;
		LvqModelCli^ lastModel;
		ColorArray^ colors;

		LvqDatasetCli(String^label, ColorArray^ colors, LvqDataset * newDataset);
public:
		LvqDataset const * GetDataset() {return dataset;}
		array<int>^ ClassLabels(){return cppToCli(dataset->getPointLabels());}
		array<double,2>^ RawPoints() {return cppToCli(dataset->getPoints());}
		property ColorArray^ ClassColors { ColorArray^ get(){return colors;} void set(ColorArray^ newcolors){colors=newcolors;}}
		property int ClassCount {int get(){return dataset->getClassCount();}}
		property int Dimensions {int get(){return dataset->dimensions();}}
		property String^ DatasetLabel {String^ get(){return label;}}
		property LvqModelCli^ LastModel { LvqModelCli^ get(){return lastModel;} void set(LvqModelCli^ newval){lastModel = newval;}}


		static LvqDatasetCli^ ConstructFromArray(String^ label,ColorArray^ colors,array<double,2>^ points, array<int>^ pointLabels, int classCount);
		static LvqDatasetCli^ ConstructGaussianClouds(String^ label,ColorArray^ colors, unsigned rngParamsSeed, unsigned  rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep);
		static LvqDatasetCli^ ConstructStarDataset(String^ label,ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform);
	};
}

