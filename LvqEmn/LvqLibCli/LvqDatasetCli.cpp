#include "StdAfx.h"
#include "LvqDatasetCli.h"
#include "LvqModelCli.h"

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
		, datasets( gcnew array<GcManualPtr<LvqDataset>^ >(1))
	{
		datasets[0] = gcnew GcManualPtr<LvqDataset>(newDataset, MemAllocEstimateDataset(newDataset), FreeDataset);
		ExtendAndNormalize(newDataset,extend,normalizeDims);
		DataShape shape = GetDataShape(newDataset);
		pointCount = shape.pointCount;
		dimCount = shape.dimCount;
		classCount = shape.classCount;
	}
	LvqDatasetCli::LvqDatasetCli(String^label, int folds, ColorArray^ colors, array<GcManualPtr<LvqDataset>^ >^ newDatasets) 
		: colors(colors)
		, label(label)
		, folds(folds)
		, datasets(newDatasets)
	{
		DataShape shape = GetDataShape(datasets[0]->get());
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
	using namespace System::Threading::Tasks;
	ref class ModelExtensionComputer {
		int fold;
		LvqDatasetCli^ dataset;
		array<LvqModelCli^>^ models;
		Tuple<GcManualPtr<LvqDataset>^,GcManualPtr<LvqDataset>^>^ Execute() {
			auto retval =models[fold]->ExtendDatasetByProjection(dataset, fold);
			GC::KeepAlive(this);
			return retval;
		}
		
	public:
		Task<Tuple<GcManualPtr<LvqDataset>^,GcManualPtr<LvqDataset>^>^>^ newDatasetTask;
		ModelExtensionComputer(int fold,LvqDatasetCli^ dataset,array<LvqModelCli^>^ models) :fold(fold), dataset(dataset), models(models){
			newDatasetTask = Task::Factory->StartNew(gcnew Func<Tuple<GcManualPtr<LvqDataset>^,GcManualPtr<LvqDataset>^>^>(this, &ModelExtensionComputer::Execute));
		}
	};


	LvqDatasetCli^ LvqDatasetCli::ConstructByModelExtension(array<LvqModelCli^>^ models) {
		auto newDatasetComputer = gcnew array<ModelExtensionComputer^ >(models->Length);
		for(int i=0;i<models->Length;++i) {
			newDatasetComputer[i] = gcnew ModelExtensionComputer(i,this,models);
		}
		auto newDatasets = gcnew array<GcManualPtr<LvqDataset>^ >(models->Length);
		auto newDatasetsTest = gcnew array<GcManualPtr<LvqDataset>^ >(models->Length);
		for(int i=0;i<newDatasets->Length;++i) {
			newDatasets[i] = newDatasetComputer[i]->newDatasetTask->Result->Item1;
			newDatasetsTest[i] = newDatasetComputer[i]->newDatasetTask->Result->Item2;
		}
		auto retval = gcnew LvqDatasetCli("X"+label, folds, colors,newDatasets);
		if(this->HasTestSet())
			retval->TestSet = gcnew LvqDatasetCli("X"+TestSet->label, folds, colors,newDatasetsTest);
		
		return retval;
	}
	using namespace System::Threading::Tasks;
	ref class NnErrComputer {
		int fold;
		LvqDatasetCli^ dataset;
		double Execute() {
			double retval= NearestNeighborXvalPcaErrorRate(dataset->GetTrainingDataset(fold),fold,dataset->Folds());
			GC::KeepAlive(dataset);
			return retval;
		}

	public:
		Task<double>^ nn;
		NnErrComputer(int fold,LvqDatasetCli^ dataset) :fold(fold), dataset(dataset){
			nn = Task::Factory->StartNew(gcnew Func<double>(this, &NnErrComputer::Execute));
		}
	};

	Tuple<double,double> ^ LvqDatasetCli::GetPcaNnErrorRate() {

		if(HasTestSet())
			return Tuple::Create(NearestNeighborSplitPcaErrorRate(GetTrainingDataset(0), GetTestDataset(0)), double::NaN);
		
		auto nnErr = gcnew array<NnErrComputer^>(folds);
		for(int fold=0; fold<folds; ++fold) {
			nnErr[fold] = gcnew NnErrComputer(fold,this);
		}
		
		SmartSum<1> nnErrorRate(1);
		for(int fold=0; fold<folds; ++fold) {
			nnErrorRate.CombineWith(nnErr[fold]->nn->Result, 1.0);
		}
		return Tuple::Create(nnErrorRate.GetMean()(0,0),nnErrorRate.GetSampleVariance()(0,0));
	}

	array<int>^ LvqDatasetCli::ClassLabels(){
		array<int>^ retval = gcnew array<int>(pointCount);
		pin_ptr<int> pinRetval = &retval[0];
		GetPointLabels(GetTrainingDataset(0), pinRetval);
		return retval;
	}
	int LvqDatasetCli::GetTrainingSubsetSize(int fold) { return ::GetTrainingSubsetSize(GetTrainingDataset(fold), fold,folds); }//TODO:ABI: make this a public methods taking pointers.
	int LvqDatasetCli::ClassCount::get(){return classCount;}
	int LvqDatasetCli::PointCount::get(){return pointCount;}
	int LvqDatasetCli::Dimensions::get(){return dimCount;}
}