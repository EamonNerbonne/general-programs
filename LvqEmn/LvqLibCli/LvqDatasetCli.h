#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataset.h"
//#include "WrappingUtils.h"

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
		LvqDatasetCli(String^label,int folds, ColorArray^ colors,  LvqDataset * newDataset,boost::mt19937& rngOrder);
	public:
		bool IsFolded() {return folds!=0;}
		std::vector<int> GetTrainingSubset(int fold) {
			if(!IsFolded()) return dataset->entireSet();
			fold = fold % folds;
			int pointCount = dataset->getPointCount();
			int foldStart = fold * pointCount / folds;
			int foldEnd = (fold+1) * pointCount / folds;

			std::vector<int> retval;
			for(int i=0;i<foldStart;++i)
				retval.push_back(i);
			for(int i=foldEnd;i<pointCount;++i)
				retval.push_back(i);
			return retval;
		}

		std::vector<int> GetTestSubset(int fold) {
			if(!IsFolded()) return vector<int>();
			fold = fold % folds;
			int pointCount = dataset->getPointCount();
			int foldStart = fold * pointCount / folds;
			int foldEnd = (fold+1) * pointCount / folds;
			std::vector<int> retval;
			for(int i=foldStart;i<foldEnd;++i)
				retval.push_back(i);
			return retval;
		}
		LvqDataset const * GetDataset() {return dataset;}
		array<int>^ ClassLabels(){ array<int>^ retval; cppToCli(dataset->getPointLabels(), retval); return retval;}
		array<double,2>^ RawPoints() { array<double,2>^ retval; cppToCli(dataset->getPoints(), retval); return retval;}
		property ColorArray^ ClassColors { ColorArray^ get(){return colors;} void set(ColorArray^ newcolors){colors=newcolors;}}
		property int ClassCount {int get(){return dataset->getClassCount();}}
		property int Dimensions {int get(){return dataset->dimensions();}}
		property String^ DatasetLabel {String^ get(){return label;}}
		property LvqModelCli^ LastModel { LvqModelCli^ get(){return lastModel;} void set(LvqModelCli^ newval){lastModel = newval;}}


		static LvqDatasetCli^ ConstructFromArray(String^ label,int folds, ColorArray^ colors,unsigned rngInstSeed,  array<double,2>^ points, array<int>^ pointLabels, int classCount);
		static LvqDatasetCli^ ConstructGaussianClouds(String^ label,int folds,ColorArray^ colors, unsigned rngParamsSeed, unsigned  rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep);
		static LvqDatasetCli^ ConstructStarDataset(String^ label,int folds,ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform);
	};
}

