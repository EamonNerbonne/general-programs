#include "StdAfx.h"
#include "LvqDatasetCli.h"

#include "LvqTypedefs.h"
#include "SmartSum.h"

//#include "CreateDataset.h"
//#include "LvqDataset.h"

namespace LvqLibCli {
	using boost::mt19937;


	LvqDatasetCli ^ LvqDatasetCli::ConstructFromArray(String^ label, int folds, bool extend, bool normalizeDims, ColorArray^ colors, unsigned rngInstSeed, array<LvqFloat,2>^ points,
		array<int>^ pointLabels, int classCount) {

			vector<int> cppLabels;
			Matrix_NN cppPoints;
			cliToCpp(points,cppPoints);
			cliToCpp(pointLabels,cppLabels);
			return gcnew LvqDatasetCli(label,folds,extend,normalizeDims,colors,
				CreateDatasetRaw(0u,rngInstSeed,(int)cppPoints.rows(),(int)cppPoints.cols(),classCount,
				cppPoints.data(), cppLabels.data()));
	}

	LvqDatasetCli::LvqDatasetCli(String^label, int folds,bool extend, bool normalizeDims, ColorArray^ colors, LvqDataset * newDataset) 
		: colors(colors)
		, label(label)
		, folds(folds)
		, dataset(newDataset, MemAllocEstimateDataset(newDataset), FreeDataset) 
	{
		ExtendAndNormalize(newDataset,extend,normalizeDims);
		DataShape shape = GetDataShape(newDataset);
		pointCount = shape.pointCount;
		dimCount = shape.dimCount;
		classCount = shape.classCount;
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructGaussianClouds(String^label, int folds, bool extend,  bool normalizeDims, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, 
		int classCount, int pointsPerClass, double meansep) {
			return gcnew LvqDatasetCli(label,folds,extend,normalizeDims,colors, 
				CreateGaussianClouds(rngParamsSeed,rngInstSeed,dims,classCount*pointsPerClass, classCount, 
				meansep));
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructStarDataset(String^label, int folds, bool extend,  bool normalizeDims, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, 
		int starDims, int numStarTails,	int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma) {
			return gcnew LvqDatasetCli(label,folds,extend,normalizeDims,colors,
				CreateStarDataset(rngParamsSeed,rngInstSeed,dims,classCount*pointsPerClass, classCount,
				starDims, numStarTails,starMeanSep,starClassRelOffset,randomlyRotate,noiseSigma,globalNoiseMaxSigma));
	}

	Tuple<double,double> ^ LvqDatasetCli::GetPcaNnErrorRate() {
		if(HasTestSet())
			return Tuple::Create(NearestNeighborSplitPcaErrorRate(GetTrainingDataset(), GetTestDataset()), double::NaN);

		SmartSum<1> nnErrorRate(1);
		for(int fold=0; fold<folds; ++fold) {
			nnErrorRate.CombineWith(
				NearestNeighborXvalPcaErrorRate(GetTrainingDataset(),fold,folds),
				1.0
				);
		}
		return Tuple::Create(nnErrorRate.GetMean()(0,0),nnErrorRate.GetSampleVariance()(0,0));
	}

	array<int>^ LvqDatasetCli::ClassLabels(){
		array<int>^ retval = gcnew array<int>(pointCount);
		pin_ptr<int> pinRetval = &retval[0];
		GetPointLabels(dataset, pinRetval);
		return retval;
	}
	int LvqDatasetCli::GetTrainingSubsetSize(int fold) { return ::GetTrainingSubsetSize(GetTrainingDataset(), fold,folds); }//TODO:ABI: make this a public methods taking pointers.
	int LvqDatasetCli::ClassCount::get(){return classCount;}
	int LvqDatasetCli::PointCount::get(){return pointCount;}
	int LvqDatasetCli::Dimensions::get(){return dimCount;}
}