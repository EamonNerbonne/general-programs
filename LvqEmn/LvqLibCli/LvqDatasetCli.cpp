#include "StdAfx.h"
#include "LvqDatasetCli.h"
#include "DatasetUtils.h"
namespace LvqLibCli {
	using boost::mt19937;
	LvqDatasetCli ^ LvqDatasetCli::ConstructFromArray(String^ label, array<double,2>^ points, array<int>^ pointLabels, int classCount) {
		return gcnew LvqDatasetCli(label,new LvqDataset(cliToCpp(points),cliToCpp(pointLabels),classCount));
	}

	LvqDatasetCli::LvqDatasetCli(String^label, LvqDataset * newDataset) : label(label), dataset(newDataset,newDataset->MemAllocEstimate()) { 
		colors = EmnExtensions::Wpf::OldGraph::GraphRandomPen::MakeDistributedColors(ClassCount);
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructGaussianClouds(String^label,unsigned  rngParamsSeed, unsigned rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngIter(rngInstSeed);
		return gcnew LvqDatasetCli(label,DatasetUtils::ConstructGaussianClouds(rngParam,rngIter, dims, classCount, pointsPerClass, meansep));
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructStarDataset(String^label,unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngIter(rngInstSeed);
		return gcnew LvqDatasetCli(label,DatasetUtils::ConstructStarDataset(rngParam,rngIter, dims, starDims, numStarTails, classCount, pointsPerClass, starMeanSep, starClassRelOffset));
	}
}