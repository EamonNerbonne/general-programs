#include "StdAfx.h"
#include "LvqDataSetCli.h"
#include "DataSetUtils.h"
namespace LvqLibCli {
	using boost::mt19937;
	LvqDataSetCli ^ LvqDataSetCli::ConstructFromArray(String^ label, array<double,2>^ points, array<int>^ pointLabels, int classCount) {
		return gcnew LvqDataSetCli(label,new LvqDataSet(cliToCpp(points),cliToCpp(pointLabels),classCount));
	}

	LvqDataSetCli::LvqDataSetCli(String^label, LvqDataSet * newDataset) : label(label), dataset(newDataset,newDataset->MemAllocEstimate()) { 
		colors = EmnExtensions::Wpf::OldGraph::GraphRandomPen::MakeDistributedColors(ClassCount);
	}

	LvqDataSetCli^ LvqDataSetCli::ConstructGaussianClouds(String^label,array<unsigned int>^ rngParamsSeed, array<unsigned int>^ rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep) {
		vector<unsigned> ps(cliToCpp(rngParamsSeed));
		vector<unsigned> is(cliToCpp(rngInstSeed));

		mt19937 rngParam;
		mt19937 rngIter;
		rngParam.seed(ps.begin(),ps.end());
		rngIter.seed(is.begin(),is.end());
		
		return gcnew LvqDataSetCli(label,DataSetUtils::ConstructGaussianClouds(rngParam,rngIter, dims, classCount, pointsPerClass, meansep));
	}

	LvqDataSetCli^ LvqDataSetCli::ConstructStarDataset(String^label,array<unsigned int>^ rngParamsSeed, array<unsigned int>^ rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset) {
		vector<unsigned> ps(cliToCpp(rngParamsSeed));
		vector<unsigned> is(cliToCpp(rngInstSeed));
		mt19937 rngParam;
		mt19937 rngIter;
		rngParam.seed(ps.begin(),ps.end());
		rngIter.seed(is.begin(),is.end());
		return gcnew LvqDataSetCli(label,DataSetUtils::ConstructStarDataset(rngParam,rngIter, dims, starDims, numStarTails, classCount, pointsPerClass, starMeanSep, starClassRelOffset));
	}
}