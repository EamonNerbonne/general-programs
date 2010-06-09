#include "StdAfx.h"
#include "LvqDatasetCli.h"
#include "DatasetUtils.h"
namespace LvqLibCli {
	using boost::mt19937;
	LvqDatasetCli ^ LvqDatasetCli::ConstructFromArray(String^ label,ColorArray^ colors, array<double,2>^ points, array<int>^ pointLabels, int classCount) {
		vector<int> cppLabels;
		MatrixXd cppPoints;
		cliToCpp(points,cppPoints);
		cliToCpp(pointLabels,cppLabels);
		return gcnew LvqDatasetCli(label,colors, new LvqDataset(cppPoints,cppLabels,classCount));
	}

	LvqDatasetCli::LvqDatasetCli(String^label, ColorArray^ colors, LvqDataset * newDataset) : colors(colors),label(label), dataset(newDataset,newDataset->MemAllocEstimate()) { }

	LvqDatasetCli^ LvqDatasetCli::ConstructGaussianClouds(String^label,ColorArray^ colors,unsigned  rngParamsSeed, unsigned rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngIter(rngInstSeed);
		return gcnew LvqDatasetCli(label,colors,DatasetUtils::ConstructGaussianClouds(rngParam,rngIter, dims, classCount, pointsPerClass, meansep));
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructStarDataset(String^label,ColorArray^ colors,unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngIter(rngInstSeed);
		return gcnew LvqDatasetCli(label,colors,DatasetUtils::ConstructStarDataset(rngParam,rngIter, dims, starDims, numStarTails, classCount, pointsPerClass, starMeanSep, starClassRelOffset,randomlyTransform));
	}
}