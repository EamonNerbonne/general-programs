#include "StdAfx.h"
#include "LvqDataSetCli.h"
#include "DataSetUtils.h"
namespace LVQCppCli {
	LvqDataSetCli ^ LvqDataSetCli::ConstructFromArray(array<double,2>^ points, array<int>^ pointLabels, int classCount)
	{
		return gcnew LvqDataSetCli(new LvqDataSet(arrayToMatrix(points),cliToStl(pointLabels),classCount));
	}

	LvqDataSetCli::LvqDataSetCli(LvqDataSet * newDataset) : dataset(NULL), nativeAllocEstimate(0)
	{
		dataset.Attach(newDataset);
		nativeAllocEstimate = dataset->MemAllocEstimate();
		GC::AddMemoryPressure(nativeAllocEstimate);
	}


	LvqDataSetCli::!LvqDataSetCli() { GC::RemoveMemoryPressure(nativeAllocEstimate);}

	LvqDataSetCli^ LvqDataSetCli::MakeGaussianClouds( Func<unsigned int>^ rng, int dims, int pointCount, int classCount, double meansep) {
		boost::mt19937 internalRnd(rng);
		return gcnew LvqDataSetCli(DataSetUtils::ConstructDataSet(internalRnd,dims,pointCount,classCount,meansep));
	}
}