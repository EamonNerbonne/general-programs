#include "StdAfx.h"
#include "LvqDataSetCli.h"
#include "DataSetUtils.h"
namespace LvqLibCli {
	LvqDataSetCli ^ LvqDataSetCli::ConstructFromArray(String^ label, array<double,2>^ points, array<int>^ pointLabels, int classCount)
	{
		return gcnew LvqDataSetCli(label,new LvqDataSet(cliToCpp(points),cliToCpp(pointLabels),classCount));
	}

	LvqDataSetCli::LvqDataSetCli(String^label, LvqDataSet * newDataset) : dataset(NULL), nativeAllocEstimate(0)
	{
		this->label=label;
		dataset.Attach(newDataset);
		nativeAllocEstimate = dataset->MemAllocEstimate();
		GC::AddMemoryPressure(nativeAllocEstimate);
	}


	LvqDataSetCli::!LvqDataSetCli() { GC::RemoveMemoryPressure(nativeAllocEstimate);}

	LvqDataSetCli^ LvqDataSetCli::ConstructGaussianClouds(String^label,Func<unsigned int>^ rng, int dims, int classCount, int pointsPerClass, double meansep) {
		boost::mt19937 internalRnd(rng);
		return gcnew LvqDataSetCli(label,DataSetUtils::ConstructGaussianClouds(internalRnd, dims, classCount, pointsPerClass, meansep));
	}

	LvqDataSetCli^ LvqDataSetCli::ConstructStarDataset(String^label,Func<unsigned int>^ rng, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset) {
		boost::mt19937 internalRnd(rng);
		printf("%d\n",rng());
		return gcnew LvqDataSetCli(label,DataSetUtils::ConstructStarDataset(internalRnd, dims, starDims, numStarTails, classCount, pointsPerClass, starMeanSep, starClassRelOffset));
	}
}