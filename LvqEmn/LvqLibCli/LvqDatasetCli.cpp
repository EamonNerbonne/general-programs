#include "StdAfx.h"
#include "LvqDatasetCli.h"
#include "CreateDataset.h"
#include "SmartSum.h"
#include "LvqDataset.h"

namespace LvqLibCli {
	using boost::mt19937;


	LvqDatasetCli ^ LvqDatasetCli::ConstructFromArray(String^ label, int folds, bool extend, bool normalizeDims, ColorArray^ colors, unsigned rngInstSeed, array<LvqFloat,2>^ points,
			array<int>^ pointLabels, int classCount) {
		mt19937 rngIter(rngInstSeed);
		vector<int> cppLabels;
		Matrix_NN cppPoints;
		cliToCpp(points,cppPoints);
		cliToCpp(pointLabels,cppLabels);
		return gcnew LvqDatasetCli(label,folds,extend,normalizeDims,colors, new LvqDataset(cppPoints,cppLabels,classCount), rngIter);
	}

	LvqDatasetCli::LvqDatasetCli(String^label, int folds,bool extend, bool normalizeDims, ColorArray^ colors, LvqDataset * newDataset,mt19937& rngOrder) 
		: colors(colors)
		, label(label)
		, folds(folds)
		, dataset(newDataset,newDataset->MemAllocEstimate()) //TODO:ABI: make this a public methods taking pointers.
	{
		if(extend) dataset->ExtendByCorrelations(); //TODO:ABI make these public methods taking pointers.
		if(normalizeDims) dataset->NormalizeDimensions();  //TODO:ABI make these public methods taking pointers.
		dataset->shufflePoints(rngOrder);  //TODO:ABI this should be in creator function.
	}//TODO:ABI: constructor functions should take seed value, not mt19937, and should know folds.

	LvqDatasetCli^ LvqDatasetCli::ConstructGaussianClouds(String^label, int folds, bool extend,  bool normalizeDims, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, 
			int classCount, int pointsPerClass, double meansep) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngInst(rngInstSeed);
		return gcnew LvqDatasetCli(label,folds,extend,normalizeDims,colors,CreateDataset::ConstructGaussianClouds(rngParam,rngInst, dims, classCount, pointsPerClass, meansep),rngInst);
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructStarDataset(String^label, int folds, bool extend,  bool normalizeDims, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, 
			int starDims, int numStarTails,	int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyTransform, double noiseSigma, double globalNoiseMaxSigma) {
		mt19937 rngParam(rngParamsSeed);
		mt19937 rngInst(rngInstSeed);
		return gcnew LvqDatasetCli(label,folds,extend,normalizeDims,colors,CreateDataset::ConstructStarDataset(rngParam, rngInst, dims, starDims, numStarTails, classCount, pointsPerClass, 
				starMeanSep, starClassRelOffset, randomlyTransform, noiseSigma, globalNoiseMaxSigma),rngInst);
	}

	Tuple<double,double> ^ LvqDatasetCli::GetPcaNnErrorRate() {
		if(HasTestSet())
			return Tuple::Create(
				GetTrainingDataset()->NearestNeighborPcaErrorRate( //TODO:ABI: make this a function taking two dataset pointers
					GetTrainingSubset(0),
					GetTestDataset(),
					GetTestSubset(0)
				), double::NaN);
				
		SmartSum<1> nnErrorRate(1);
		for(int fold=0;fold<folds;++fold) {
			nnErrorRate.CombineWith(
				GetTrainingDataset()->NearestNeighborPcaErrorRate(//TODO:ABI: make this a function taking one dataset pointer, and two folds.
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
	vector<int> LvqDatasetCli::GetTrainingSubset(int fold) { return GetTrainingDataset()->GetTrainingSubset(fold,folds); } //TODO:ABI:remove.
	int LvqDatasetCli::GetTrainingSubsetSize(int fold) { return GetTrainingDataset()->GetTrainingSubsetSize(fold,folds); }//TODO:ABI: make this a public methods taking pointers.
	vector<int> LvqDatasetCli::GetTestSubset(int fold) { return HasTestSet() ? GetTestDataset()->GetTrainingSubset(0,0) : GetTestDataset()->GetTestSubset(fold,folds); }//TODO:ABI:remove.
	int LvqDatasetCli::ClassCount::get(){return dataset->getClassCount();}
	int LvqDatasetCli::PointCount::get(){return dataset->getPointCount();}
	int LvqDatasetCli::Dimensions::get(){return dataset->dimensions();}
}