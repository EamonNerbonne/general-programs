#include "StdAfx.h"
#include "LvqDatasetCli.h"
#include "DatasetUtils.h"
namespace LvqLibCli {
	using boost::mt19937;


	LvqDatasetCli ^ LvqDatasetCli::ConstructFromArray(String^ label,int folds,ColorArray^ colors,unsigned rngInstSeed,  array<double,2>^ points, array<int>^ pointLabels, int classCount) {
		mt19937 rngIter(rngInstSeed);
		vector<int> cppLabels;
		MatrixXd cppPoints;
		cliToCpp(points,cppPoints);
		cliToCpp(pointLabels,cppLabels);
		return gcnew LvqDatasetCli(label,folds,colors, new LvqDataset(cppPoints,cppLabels,classCount), rngIter);
	}

	LvqDatasetCli::LvqDatasetCli(String^label, int folds,ColorArray^ colors, LvqDataset * newDataset,mt19937& rngOrder) 
		: colors(colors)
		, label(label)
		, folds(folds)
		, dataset(newDataset,newDataset->MemAllocEstimate())
	{ dataset->shufflePoints(rngOrder); }

	LvqDatasetCli^ LvqDatasetCli::ConstructGaussianClouds(String^label,int folds,ColorArray^ colors,unsigned  rngParamsSeed, unsigned rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngInst(rngInstSeed);
		return gcnew LvqDatasetCli(label,folds,colors,DatasetUtils::ConstructGaussianClouds(rngParam,rngInst, dims, classCount, pointsPerClass, meansep),rngInst);
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructStarDataset(String^label,int folds,ColorArray^ colors,unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngInst(rngInstSeed);
		return gcnew LvqDatasetCli(label,folds,colors,DatasetUtils::ConstructStarDataset(rngParam,rngInst, dims, starDims, numStarTails, classCount, pointsPerClass, starMeanSep, starClassRelOffset,randomlyTransform),rngInst);
	}
}