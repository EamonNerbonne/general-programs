#include "StdAfx.h"
#include "LvqDatasetCli.h"
#include "LvqModelCli.h"

#include "LvqTypedefs.h"
#include "SmartSum.h"

//#include "CreateDataset.h"
//#include "LvqDataset.h"

namespace LvqLibCli {
	using boost::mt19937;


	LvqDatasetCli ^ LvqDatasetCli::ConstructFromArray(String^ label, int folds, bool extend, bool normalizeDims,bool normalizeByScaling, ColorArray^ colors, unsigned rngInstSeed, 
		array<LvqFloat,2>^ points, array<int>^ pointLabels, int classCount) {
			vector<int> cppLabels;
			Matrix_NN cppPoints;
			cliToCpp(points,cppPoints);
			cliToCpp(pointLabels,cppLabels);
			LvqDataset* nativedataset = CreateDatasetRaw(0u,rngInstSeed,(int)cppPoints.rows(),(int)cppPoints.cols(),classCount, cppPoints.data(), cppLabels.data());
			return gcnew LvqDatasetCli(label,folds,extend,normalizeDims,normalizeByScaling,colors,nativedataset);
	}

	GcAutoPtr<vector<DataShape> >^ GetShapes( array<GcManualPtr<LvqDataset>^ >^ newDatasets) {
		vector<DataShape> shapes;
		for(int i=0;i<newDatasets->Length;i++) {

			DataShape shape = GetDataShape(newDatasets[i]->get());
			shapes.push_back(shape);
			assert(shape.dimCount == shapes[0].dimCount);
			assert(shape.classCount == shapes[0].classCount);
			assert(abs((int)shape.pointCount - (int)shapes[0].pointCount)<=1);
			//std::cout<<shape.pointCount<<"; ";
		}
		//std::cout<<"\n";
		return gcnew GcAutoPtr<vector<DataShape> >(new vector<DataShape>(shapes), sizeof(shapes)+shapes.size()*sizeof(DataShape));
	}

	LvqDatasetCli::LvqDatasetCli(String^label, int folds,bool extend, bool normalizeDims, bool normalizeByScaling, ColorArray^ colors, LvqDataset * newDataset) 
		: colors(colors)
		, label(label)
		, datasets(gcnew array<GcManualPtr<LvqDataset>^ >(folds))
	{
		ExtendAndNormalize(newDataset,extend,normalizeDims, normalizeByScaling);

		array<GcManualPtr<LvqDataset>^ >^ testDatasets = gcnew array<GcManualPtr<LvqDataset>^ >(folds);

		for(int i=0;i<folds;i++) {
			auto trn = CreateDatasetFold(newDataset,i,folds,false);
			datasets[i] = gcnew GcManualPtr<LvqDataset>(trn, MemAllocEstimateDataset(trn), FreeDataset);

			auto tst = CreateDatasetFold(newDataset,i,folds,true);
			testDatasets[i] = gcnew GcManualPtr<LvqDataset>(tst, MemAllocEstimateDataset(tst), FreeDataset);

		}
		FreeDataset(newDataset);
		datashape = GetShapes(datasets);
		

		testSet = gcnew LvqDatasetCli(nullptr, colors,testDatasets,nullptr);
	}

	LvqDatasetCli::LvqDatasetCli(String^label, ColorArray^ colors, array<GcManualPtr<LvqDataset>^ >^ newDatasets, array<GcManualPtr<LvqDataset>^ >^ newTestDatasets) 
		: colors(colors)
		, label(label)
		, datasets(newDatasets)
		, testSet(newTestDatasets==nullptr?nullptr:gcnew LvqDatasetCli(nullptr,colors,newTestDatasets,nullptr))
	{
		if(newTestDatasets!=nullptr && newDatasets->Length!=newTestDatasets->Length) throw gcnew ArgumentException("newTestDatasets","test datasets must have the same number of folds as the training datasets");
		datashape = GetShapes(datasets);
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructGaussianClouds(String^label, int folds, bool extend,  bool normalizeDims, bool normalizeByScaling, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, 
		int classCount, int pointsPerClass, double meansep) {
			return gcnew LvqDatasetCli(label,folds,extend,normalizeDims, normalizeByScaling,colors, 
				CreateGaussianClouds(rngParamsSeed,rngInstSeed,dims,classCount*pointsPerClass, classCount, 
				meansep));
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructStarDataset(String^label, int folds, bool extend,  bool normalizeDims,bool normalizeByScaling, ColorArray^ colors, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, 
		int starDims, int numStarTails,	int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma) {
			return gcnew LvqDatasetCli(label,folds,extend,normalizeDims, normalizeByScaling,colors,
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

	using namespace System::Text::RegularExpressions;

	ref struct RegexConsts {
		static initonly Regex^ dimcountregex = gcnew Regex("\\-[0-9]+D[^-]*(?=\\-)|$");
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
		DataShape shape=GetDataShape(newDatasets[0]->get());
		return gcnew LvqDatasetCli(RegexConsts::dimcountregex->Replace(label,"$0X"+shape.dimCount,1), colors, newDatasets,newDatasetsTest);
	}
	using namespace System::Threading::Tasks;
	ref class NnErrComputer {
		int fold;
		LvqDatasetCli^ dataset;
		double Execute() {
			double retval= NearestNeighborSplitPcaErrorRate(dataset->GetTrainingDataset(fold),dataset->GetTestDataset(fold));
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
		
		auto nnErr = gcnew array<NnErrComputer^>(Folds());
		for(int fold=0; fold<Folds(); ++fold) {
			nnErr[fold] = gcnew NnErrComputer(fold,this);
		}
		
		SmartSum<1> nnErrorRate(1);
		for(int fold=0; fold<Folds(); ++fold) {
			nnErrorRate.CombineWith(nnErr[fold]->nn->Result, 1.0);
		}
		return Tuple::Create(nnErrorRate.GetMean()(0,0),nnErrorRate.GetSampleVariance()(0,0));
	}

	array<int>^ LvqDatasetCli::ClassLabels(int fold) {
		array<int>^ retval = gcnew array<int>(PointCount(fold));
		pin_ptr<int> pinRetval = &retval[0];
		GetPointLabels(GetTrainingDataset(fold), pinRetval);
		return retval;
	}
	int LvqDatasetCli::PointCount(int fold) { return FoldShape(fold).pointCount; }
	int LvqDatasetCli::ClassCount::get(){return FoldShape(0).classCount;}
	int LvqDatasetCli::Dimensions::get(){return FoldShape(0).dimCount;}
}