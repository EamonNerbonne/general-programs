#pragma once
using namespace System;
#include <boost/random/mersenne_twister.hpp>
#include "LvqTypedefs.h"
class LvqDataset;

namespace LvqLibCli {
	ref class LvqModelCli;
	public ref class LvqDatasetCli
	{
		typedef array<System::Windows::Media::Color> ColorArray;

		GcAutoPtr<LvqDataset> dataset;
		String^ label;
		LvqModelCli^ lastModel;
		LvqDatasetCli ^testSet;
		ColorArray^ colors;
		int folds;
		LvqDatasetCli(String^label,int folds, bool extend,ColorArray^ colors, LvqDataset * newDataset,boost::mt19937& rngOrder);
	public:
		bool IsFolded() {return folds!=0;}
		bool HasTestSet() {return testSet != nullptr;}
		std::vector<int> GetTrainingSubset(int fold);
		std::vector<int> GetTestSubset(int fold);
		LvqDataset const * GetTrainingDataset() {return dataset;}
		LvqDataset const * GetTestDataset() {return testSet==nullptr?dataset:testSet->dataset;}
		array<int>^ ClassLabels();
		array<LvqFloat,2>^ RawPoints();
		property ColorArray^ ClassColors { ColorArray^ get(){return colors;} void set(ColorArray^ newcolors){colors=newcolors;}}
		property int ClassCount {int get();}
		property int PointCount {int get();}
		property int Dimensions {int get();}
		property String^ DatasetLabel {String^ get(){return label;}}
		property LvqModelCli^ LastModel { LvqModelCli^ get(){return lastModel;} void set(LvqModelCli^ newval){lastModel = newval;}}

		property LvqDatasetCli^ TestSet { LvqDatasetCli^ get(){return testSet;} void set(LvqDatasetCli^ newval){testSet = newval;}}

		Tuple<double,double> ^ GetPcaNnErrorRate();

		static LvqDatasetCli^ ConstructFromArray(String^ label,int folds, bool extend, ColorArray^ colors,unsigned rngInstSeed, array<LvqFloat,2>^ points, array<int>^ pointLabels, int classCount);
		static LvqDatasetCli^ ConstructGaussianClouds(String^ label,int folds, bool extend, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep);
		static LvqDatasetCli^ ConstructStarDataset(String^ label,int folds, bool extend, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform, double noiseSigma);
	};
}

