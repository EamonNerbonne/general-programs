#include "StdAfx.h"
#include "LvqDatasetCli.h"
#include "LvqModelCli.h"

#include "LvqTypedefs.h"
#include "SmartSum.h"

//#include "CreateDataset.h"
//#include "LvqDataset.h"

namespace LvqLibCli {
	using boost::mt19937;


	LvqDataset* toNativeDataset(array<LvqFloat,2>^ points, array<int>^ pointLabels, unsigned shuffleSeed, int classCount) {
		
		vector<int> cppLabels;
		Matrix_NN cppPoints;
		cliToCpp(points,cppPoints);
		cliToCpp(pointLabels,cppLabels);
		//Console::WriteLine(cppLabels.size());
		//for each(auto i in cppLabels) Console::WriteLine(i);
		return CreateDatasetRaw(shuffleSeed,(int)cppPoints.rows(),(int)cppPoints.cols(),classCount, cppPoints.data(), cppLabels.data());
	}

	LvqDatasetCli^ LvqDatasetCli::Unfolder(String^label, int folds,bool extend, bool normalizeDims, bool normalizeByScaling, ColorArray^ colors, array<String^>^ classes, LvqDataset * newDataset) {
		array<GcManualPtr<LvqDataset>^ >^ datasets = gcnew array<GcManualPtr<LvqDataset>^ >(folds);
		array<GcManualPtr<LvqDataset>^ >^ testDatasets = gcnew array<GcManualPtr<LvqDataset>^ >(folds);

		for(int i=0;i<folds;i++) {
			auto trn = CreateDatasetFold(newDataset,i,folds,false);
			auto tst = CreateDatasetFold(newDataset,i,folds,true);
			ExtendAndNormalize(trn,tst,extend,normalizeDims,normalizeByScaling);

			datasets[i] = gcnew GcManualPtr<LvqDataset>(trn, MemAllocEstimateDataset(trn), FreeDataset);
			testDatasets[i] = gcnew GcManualPtr<LvqDataset>(tst, MemAllocEstimateDataset(tst), FreeDataset);
		}
		FreeDataset(newDataset);
		
		return gcnew LvqDatasetCli(label,colors,classes,datasets,testDatasets,nullptr);
	}

	LvqDatasetCli ^ LvqDatasetCli::ConstructFromArray(String^ label, int folds, bool extend, bool normalizeDims,bool normalizeByScaling, ColorArray^ colors, array<String^>^ classes, unsigned rngInstSeed, 
		array<LvqFloat,2>^ points, array<int>^ pointLabels, array<LvqFloat,2>^ testpoints, array<int>^ testpointLabels) {
			LvqDataset* nativedataset = toNativeDataset(points, pointLabels, rngInstSeed,colors->Length);
			if(!testpoints)
				return Unfolder(label,folds,extend,normalizeDims,normalizeByScaling,colors,classes,nativedataset);

			LvqDataset* nativetestdataset = toNativeDataset(testpoints, testpointLabels, rngInstSeed+1,colors->Length);

			ExtendAndNormalize(nativedataset,nativetestdataset,extend,normalizeDims,normalizeByScaling);


			array<GcManualPtr<LvqDataset>^ >^ datasetArr = gcnew array<GcManualPtr<LvqDataset>^ > {
				 gcnew GcManualPtr<LvqDataset>(nativedataset, MemAllocEstimateDataset(nativedataset), FreeDataset)
			};
			array<GcManualPtr<LvqDataset>^ >^ testdatasetArr = gcnew array<GcManualPtr<LvqDataset>^ > {
				 gcnew GcManualPtr<LvqDataset>(nativetestdataset, MemAllocEstimateDataset(nativetestdataset), FreeDataset)
			};

			return gcnew LvqDatasetCli(label, colors, classes, datasetArr, testdatasetArr,nullptr);
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



	LvqDatasetCli::LvqDatasetCli(String^label, ColorArray^ colors, array<String^>^ classes, array<GcManualPtr<LvqDataset>^ >^ newDatasets, array<GcManualPtr<LvqDataset>^ >^ newTestDatasets, LvqDatasetCli^ parent) 
		: parent(parent)
		, colors(colors)
		, classNames(classes)
		, label(label)
		, datasets(newDatasets)
		, testSet(newTestDatasets==nullptr?nullptr:gcnew LvqDatasetCli(nullptr,colors, classes, newTestDatasets,nullptr, parent==nullptr?nullptr:parent->testSet))
	{
		if(newTestDatasets!=nullptr && newDatasets->Length!=newTestDatasets->Length) throw gcnew ArgumentException("newTestDatasets","test datasets must have the same number of folds as the training datasets");
		datashape = GetShapes(datasets);
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructGaussianClouds(String^label, int folds, bool extend,  bool normalizeDims, bool normalizeByScaling, ColorArray^ colors, array<String^>^ classes, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, 
		int pointsPerClass, double meansep) {
			return Unfolder(label,folds,extend,normalizeDims, normalizeByScaling, colors, classes,
				CreateGaussianClouds(rngParamsSeed,rngInstSeed,dims,classes->Length*pointsPerClass, classes->Length, 
				meansep));
	}

	LvqDatasetCli^ LvqDatasetCli::ConstructStarDataset(String^label, int folds, bool extend,  bool normalizeDims,bool normalizeByScaling, ColorArray^ colors, array<String^>^ classes, unsigned rngParamsSeed, unsigned rngInstSeed, int dims, 
		int starDims, int numStarTails,	 int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma) {
			return Unfolder(label,folds,extend,normalizeDims, normalizeByScaling, colors, classes,
				CreateStarDataset(rngParamsSeed,rngInstSeed,dims,classes->Length*pointsPerClass, classes->Length,
				starDims, numStarTails,starMeanSep,starClassRelOffset,randomlyRotate,noiseSigma,globalNoiseMaxSigma));
	}
	using namespace System::Threading::Tasks;
	ref class ModelExtensionComputer {
		int fold;
		LvqDatasetCli^ dataset, ^toInclude;
		array<LvqModelCli^>^ models;
		Tuple<GcManualPtr<LvqDataset>^,GcManualPtr<LvqDataset>^>^ Execute() {
			auto retval =models[fold]->ExtendDatasetByProjection(dataset, toInclude,fold);
			GC::KeepAlive(this);
			return retval;
		}
		
	public:
		Task<Tuple<GcManualPtr<LvqDataset>^,GcManualPtr<LvqDataset>^>^>^ newDatasetTask;
		ModelExtensionComputer(int fold,LvqDatasetCli^ dataset,LvqDatasetCli^ toInclude,array<LvqModelCli^>^ models) :fold(fold), dataset(dataset), toInclude(toInclude), models(models){
			newDatasetTask = Task::Factory->StartNew(gcnew Func<Tuple<GcManualPtr<LvqDataset>^,GcManualPtr<LvqDataset>^>^>(this, &ModelExtensionComputer::Execute));
		}
	};

	using namespace System::Text::RegularExpressions;

	ref struct RegexConsts {
		static initonly Regex^ dimcountregex = gcnew Regex("\\-[0-9]+D[^-]*(?=\\-)|$");
	};

	LvqDatasetCli^ LvqDatasetCli::ConstructByModelExtension(array<LvqModelCli^>^ models) {
		LvqDatasetCli^toInclude = this;
		while(toInclude->parent !=nullptr) toInclude = toInclude->parent;

		auto newDatasetComputer = gcnew array<ModelExtensionComputer^ >(models->Length);
		for(int i=0;i<models->Length;++i) {
			newDatasetComputer[i] = gcnew ModelExtensionComputer(i,this,toInclude,models);
		}
		auto newDatasets = gcnew array<GcManualPtr<LvqDataset>^ >(models->Length);
		auto newDatasetsTest = gcnew array<GcManualPtr<LvqDataset>^ >(models->Length);
		for(int i=0;i<newDatasets->Length;++i) {
			newDatasets[i] = newDatasetComputer[i]->newDatasetTask->Result->Item1;
			newDatasetsTest[i] = newDatasetComputer[i]->newDatasetTask->Result->Item2;
		}
		DataShape shape=GetDataShape(newDatasets[0]->get());
		return gcnew LvqDatasetCli(RegexConsts::dimcountregex->Replace(label,"$0X"+shape.dimCount,1), colors, classNames, newDatasets, newDatasetsTest, this);
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