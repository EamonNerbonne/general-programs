#pragma once
using namespace System;
#include <boost/random/mersenne_twister.hpp>
class LvqDataset;

namespace LvqLibCli {
	ref class LvqModelCli;
	public ref class LvqDatasetCli
	{
		typedef array<System::Windows::Media::Color> ColorArray;

		GcAutoPtr<LvqDataset> dataset;
		String^ label;
		LvqModelCli^ lastModel;
		ColorArray^ colors;
		int folds;
		LvqDatasetCli(String^label,int folds, bool extend,ColorArray^ colors,  LvqDataset * newDataset,boost::mt19937& rngOrder);
	public:
		bool IsFolded() {return folds!=0;}
		std::vector<int> GetTrainingSubset(int fold);
		std::vector<int> GetTestSubset(int fold);
		LvqDataset const * GetDataset() {return dataset;}
		array<int>^ ClassLabels();
		array<double,2>^ RawPoints();
		property ColorArray^ ClassColors { ColorArray^ get(){return colors;} void set(ColorArray^ newcolors){colors=newcolors;}}
		property int ClassCount {int get();}
		property int Dimensions {int get();}
		property String^ DatasetLabel {String^ get(){return label;}}
		property LvqModelCli^ LastModel { LvqModelCli^ get(){return lastModel;} void set(LvqModelCli^ newval){lastModel = newval;}}

		Tuple<double,double> ^ GetPcaNnErrorRate();

		static LvqDatasetCli^ ConstructFromArray(String^ label,int folds, bool extend, ColorArray^ colors,unsigned rngInstSeed,  array<double,2>^ points, array<int>^ pointLabels, int classCount);
		static LvqDatasetCli^ ConstructGaussianClouds(String^ label,int folds, bool extend, ColorArray^ colors, unsigned rngParamsSeed, unsigned  rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep);
		static LvqDatasetCli^ ConstructStarDataset(String^ label,int folds, bool extend, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform);
	};
}

