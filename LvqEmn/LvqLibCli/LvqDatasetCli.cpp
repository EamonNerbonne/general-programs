#include "StdAfx.h"
#include "LvqDataSetCli.h"
#include "DataSetUtils.h"
namespace LVQCppCli {
	LvqDataSetCli ^ LvqDataSetCli::ConstructFromArray(array<double,2>^ points, array<int>^ pointLabels, int classCount)
	{
		return gcnew LvqDataSetCli(new LvqDataSet(cliToCpp(points),cliToCpp(pointLabels),classCount));
	}

	LvqDataSetCli::LvqDataSetCli(LvqDataSet * newDataset) : dataset(NULL), nativeAllocEstimate(0)
	{
		dataset.Attach(newDataset);
		nativeAllocEstimate = dataset->MemAllocEstimate();
		GC::AddMemoryPressure(nativeAllocEstimate);
	}


	LvqDataSetCli::!LvqDataSetCli() { GC::RemoveMemoryPressure(nativeAllocEstimate);}

	LvqDataSetCli^ LvqDataSetCli::ConstructGaussianClouds( Func<unsigned int>^ rng, int dims, int classCount, int pointsPerClass, double meansep) {
		boost::mt19937 internalRnd(rng);
		return gcnew LvqDataSetCli(DataSetUtils::ConstructGaussianClouds(internalRnd, dims, classCount, pointsPerClass, meansep));
	}

	LvqDataSetCli^ LvqDataSetCli::ConstructStarDataset(Func<unsigned int>^ rng, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset) {
		boost::mt19937 internalRnd(rng);
		printf("%d\n",rng());
		return gcnew LvqDataSetCli(DataSetUtils::ConstructStarDataset(internalRnd, dims, starDims, numStarTails, classCount, pointsPerClass, starMeanSep, starClassRelOffset));
	}

}