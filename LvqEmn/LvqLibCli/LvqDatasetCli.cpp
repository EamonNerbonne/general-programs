#include "StdAfx.h"
#include "LvqDatasetCli.h"
#include "DatasetUtils.h"
#include "SmartSum.h"
#include "LvqDataset.h"

namespace LvqLibCli {
	using boost::mt19937;


	LvqDatasetCli ^ LvqDatasetCli::ConstructFromArray(String^ label,int folds,bool extend,ColorArray^ colors,unsigned rngInstSeed,  array<double,2>^ points, array<int>^ pointLabels, int classCount) {
		mt19937 rngIter(rngInstSeed);
		vector<int> cppLabels;
		MatrixXd cppPoints;
		cliToCpp(points,cppPoints);
		cliToCpp(pointLabels,cppLabels);
		return gcnew LvqDatasetCli(label,folds,extend,colors, new LvqDataset(cppPoints,cppLabels,classCount), rngIter);
	}

	LvqDatasetCli::LvqDatasetCli(String^label, int folds,bool extend,ColorArray^ colors, LvqDataset * newDataset,mt19937& rngOrder) 
		: colors(colors)
		, label(label)
		, folds(folds)
		, dataset(newDataset,newDataset->MemAllocEstimate())
	{  dataset->shufflePoints(rngOrder); if(extend)dataset->ExtendByCorrelations(); }

	LvqDatasetCli^ LvqDatasetCli::ConstructGaussianClouds(String^label,int folds,bool extend,ColorArray^ colors,unsigned  rngParamsSeed, unsigned rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngInst(rngInstSeed);
		return gcnew LvqDatasetCli(label,folds,extend,colors,DatasetUtils::ConstructGaussianClouds(rngParam,rngInst, dims, classCount, pointsPerClass, meansep),rngInst);
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructStarDataset(String^label,int folds,bool extend,ColorArray^ colors,unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails,int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngInst(rngInstSeed);
		return gcnew LvqDatasetCli(label,folds,extend,colors,DatasetUtils::ConstructStarDataset(rngParam,rngInst, dims, starDims, numStarTails, classCount, pointsPerClass, starMeanSep, starClassRelOffset,randomlyTransform),rngInst);
	}

	Tuple<double,double> ^ LvqDatasetCli::GetPcaNnErrorRate()  {
		SmartSum<1> nnErrorRate(1);
		for(int fold=0;fold<folds;++fold) {
			nnErrorRate.CombineWith(
				dataset->NearestNeighborPcaErrorRate(
					dataset->GetTrainingSubset(fold,folds),
					dataset.get(),
					dataset->GetTestSubset(fold,folds)
				),
				1.0
			);
		}
		return Tuple::Create(nnErrorRate.GetMean()(0,0),nnErrorRate.GetSampleVariance()(0,0));
	}

	array<int>^ LvqDatasetCli::ClassLabels(){ array<int>^ retval; cppToCli(dataset->getPointLabels(), retval); return retval;}
	array<double,2>^ LvqDatasetCli::RawPoints() { array<double,2>^ retval; cppToCli(dataset->getPoints(), retval); return retval;}
	vector<int> LvqDatasetCli::GetTrainingSubset(int fold) { return dataset->GetTrainingSubset(fold,folds); }
	vector<int> LvqDatasetCli::GetTestSubset(int fold) { return dataset->GetTestSubset(fold,folds); }
	int LvqDatasetCli::ClassCount::get(){return dataset->getClassCount();}
	int LvqDatasetCli::PointCount::get(){return dataset->getPointCount();}
	int LvqDatasetCli::Dimensions::get(){return dataset->dimensions();}
}