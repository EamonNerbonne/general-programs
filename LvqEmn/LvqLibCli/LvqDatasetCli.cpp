#include "StdAfx.h"
#include "LvqDatasetCli.h"
#include "CreateDataset.h"
#include "SmartSum.h"
#include "LvqDataset.h"

namespace LvqLibCli {
	using boost::mt19937;


	LvqDatasetCli ^ LvqDatasetCli::ConstructFromArray(String^ label, int folds, bool extend, ColorArray^ colors, unsigned rngInstSeed, array<LvqFloat,2>^ points, array<int>^ pointLabels, int classCount) {
		mt19937 rngIter(rngInstSeed);
		vector<int> cppLabels;
		Matrix_NN cppPoints;
		cliToCpp(points,cppPoints);
		cliToCpp(pointLabels,cppLabels);
		return gcnew LvqDatasetCli(label,folds,extend,colors, new LvqDataset(cppPoints,cppLabels,classCount), rngIter);
	}

	LvqDatasetCli::LvqDatasetCli(String^label, int folds,bool extend,ColorArray^ colors, LvqDataset * newDataset,mt19937& rngOrder) 
		: colors(colors)
		, label(label)
		, folds(folds)
		, dataset(newDataset,newDataset->MemAllocEstimate())
	{ dataset->shufflePoints(rngOrder); if(extend)dataset->ExtendByCorrelations(); }

	LvqDatasetCli^ LvqDatasetCli::ConstructGaussianClouds(String^label, int folds, bool extend,ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int classCount, int pointsPerClass, double meansep) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngInst(rngInstSeed);
		return gcnew LvqDatasetCli(label,folds,extend,colors,CreateDataset::ConstructGaussianClouds(rngParam,rngInst, dims, classCount, pointsPerClass, meansep),rngInst);
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructStarDataset(String^label, int folds, bool extend, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, int starDims, int numStarTails,
			int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform, double noiseSigma) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngInst(rngInstSeed);
		return gcnew LvqDatasetCli(label,folds,extend,colors,CreateDataset::ConstructStarDataset(rngParam, rngInst, dims, starDims, numStarTails, classCount, pointsPerClass, starMeanSep, starClassRelOffset, randomlyTransform, noiseSigma),rngInst);
	}

	Tuple<double,double> ^ LvqDatasetCli::GetPcaNnErrorRate() {
		SmartSum<1> nnErrorRate(1);
		for(int fold=0;fold<folds;++fold) {
			nnErrorRate.CombineWith(
				GetTrainingDataset()->NearestNeighborPcaErrorRate(
					GetTrainingSubset(fold),
					GetTestDataset(),
					GetTestSubset(fold)
				),
				1.0
			);
		}
		return Tuple::Create(nnErrorRate.GetMean()(0,0),nnErrorRate.GetSampleVariance()(0,0));
	}

	array<int>^ LvqDatasetCli::ClassLabels(){ array<int>^ retval; cppToCli(dataset->getPointLabels(), retval); return retval;}
	array<LvqFloat,2>^ LvqDatasetCli::RawPoints() { array<LvqFloat,2>^ retval; cppToCli(dataset->getPoints(), retval); return retval;}
	vector<int> LvqDatasetCli::GetTrainingSubset(int fold) { return GetTrainingDataset()->GetTrainingSubset(fold,folds); }
	vector<int> LvqDatasetCli::GetTestSubset(int fold) { return HasTestSet() ? GetTestDataset()->GetTrainingSubset(0,0) : GetTestDataset()->GetTestSubset(fold,folds); }
	int LvqDatasetCli::ClassCount::get(){return dataset->getClassCount();}
	int LvqDatasetCli::PointCount::get(){return dataset->getPointCount();}
	int LvqDatasetCli::Dimensions::get(){return dataset->dimensions();}
}