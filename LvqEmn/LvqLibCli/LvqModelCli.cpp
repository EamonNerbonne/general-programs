#include "stdafx.h"

#include <vcclr.h>
#include <boost/scoped_ptr.hpp>
#include <queue>
#include "LvqModelCli.h"
#include "LvqModelSettingsCli.h"
//#include "LvqProjectionModel.h"
#include "LvqDatasetCli.h"
//#include "LvqDataset.h"
#include "SmartSum.h"
#include "utils.h"

using namespace System::Linq;
using namespace System::Runtime::InteropServices;

namespace LvqLibCli {
	typedef std::queue<std::vector<double>> Statistics;
	struct StatCollector {
		Statistics statsList;
		void Callback(size_t statsCount, LvqStat* stats) { statsList.push(std::vector<LvqStat>(stats, stats+statsCount)); }
	};
	extern "C" void StatCallbackTrampoline(void* context, size_t statsCount, LvqStat* stats) {
		static_cast<StatCollector*>(context)->Callback(statsCount, stats);
	}
	void SinkStats(List<LvqTrainingStatCli>^ stats, Statistics & nativeStats) {
		if(stats)
			while(!nativeStats.empty()) {
				LvqTrainingStatCli cliStat;
				cppToCli(nativeStats.front(),cliStat);
				nativeStats.pop();
				stats->Add(cliStat);
			}
	}

	struct NameCollector {
		gcroot<List<String^>^> nameList;
		NameCollector() : nameList(gcnew List<String^>()) {}
		void Callback(size_t statsCount, wchar_t const **names) {
			for(size_t i=0;i<statsCount;i++)
				nameList->Add(Marshal::PtrToStringUni((IntPtr)const_cast<wchar_t*>( names[i])));
		}
	};
	extern "C" void NameCollectorTrampoline(void* context, size_t statsCount, wchar_t const **names) {
		static_cast<NameCollector*>(context)->Callback(statsCount, names);
	}

	LvqModelCli::WrappedModel^ Wrap(LvqModel* nativeModel) {
		return gcnew GcManualPtr<LvqModel>(nativeModel, MemAllocEstimateModel(nativeModel), FreeModel);
	}

	LvqModelCli::LvqModelCli(String^ label, LvqDatasetCli^ trainingSet, int datafold, LvqModelSettingsCli^ modelSettings, bool trackStats)
		: label(label)
		, initSet(trainingSet)
		, initDataFold(datafold)
		, trainSync(gcnew Object())
		, copySync(gcnew Object())
		, stats(!trackStats?nullptr: gcnew List<LvqTrainingStatCli>())
	{ 
		msclr::lock l(trainSync);
		trainingSet->LastModel = this;
		LvqModel* nativeModel =  CreateLvqModel(modelSettings->ToNativeSettings(), trainingSet->GetTrainingDataset(), datafold,trainingSet->Folds());
		model = Wrap(nativeModel);

		DataShape modelShape = GetModelShape(nativeModel);
		classCount = modelShape.classCount;
		dimCount = modelShape.dimCount;
		protoCount = modelShape.pointCount;

		StatCollector statCollector;
		ComputeModelStats(trainingSet->GetTrainingDataset(), trainingSet->GetTestDataset(), datafold, trainingSet->Folds(), nativeModel, StatCallbackTrampoline, &statCollector);
		msclr::lock l2(copySync);
		SinkStats(stats, statCollector.statsList);
		modelCopy = Wrap(CloneLvqModel(nativeModel));
		GC::KeepAlive(this);
	}

	int LvqModelCli::ClassCount::get(){return classCount;}
	int LvqModelCli::Dimensions::get(){return dimCount;}
	bool LvqModelCli::IsProjectionModel::get(){ try{return ::IsProjectionModel(modelCopy->get()); }finally{GC::KeepAlive(this);} }

	double LvqModelCli::UnscaledLearningRate::get() { try { return GetUnscaledLearningRate (modelCopy->get());}finally{ GC::KeepAlive(this); } }


	bool LvqModelCli::FitsDataShape(LvqDatasetCli^ dataset) {return dataset!=nullptr && dataset->ClassCount == this->ClassCount && dataset->Dimensions == this->Dimensions;}

	LvqTrainingStatCli LvqModelCli::EvaluateStats(LvqDatasetCli^ dataset, int datafold){
		try {
			StatCollector statCollector;
			LvqTrainingStatCli cliStat;
			msclr::lock l2(copySync);
			ComputeModelStats(dataset->GetTrainingDataset(), dataset->GetTestDataset(), datafold,dataset->Folds(), modelCopy->get(), StatCallbackTrampoline, &statCollector);
			cppToCli(statCollector.statsList.front(),cliStat);
			return cliStat;
		} finally {
			GC::KeepAlive(this);
			GC::KeepAlive(dataset);
		}
	}

	LvqTrainingStatCli LvqModelCli::GetTrainingStat(int statI){
		msclr::lock l(copySync);
		return stats[statI];
	}

	int LvqModelCli::TrainingStatCount::get(){
		if(!stats) return 0;
		msclr::lock l(copySync);
		return stats->Count;
	}

	array<LvqTrainingStatCli>^ LvqModelCli::TrainingStats::get() {
		if(!stats) return nullptr;
		msclr::lock l(copySync);
		auto copy = gcnew array<LvqTrainingStatCli>(stats->Count);
		stats->CopyTo(copy);
		return copy;
	}

	array<LvqTrainingStatCli>^ LvqModelCli::GetTrainingStatsAfter(int statI) {
		if(!stats) return nullptr;
		msclr::lock l(copySync);
		return Enumerable::ToArray(Enumerable::Skip(stats,statI));
	}

	array<String^>^ LvqModelCli::TrainingStatNames::get() {
		try{
			NameCollector nameCollector;
			GetTrainingStatNames(model->get(), NameCollectorTrampoline, &nameCollector);
			return nameCollector.nameList->ToArray();
		} finally {
			GC::KeepAlive(this);
		}
	}

	void LvqModelCli::ResetLearningRate() { try{ msclr::lock l(trainSync); ::ResetLearningRate(model->get());}finally{GC::KeepAlive(this);} }

	template<typename T>
	array<CliLvqLabelledPoint>^ ToCliLabelledPoints(T const & pointmatrix,vector<int> const labels) {
		array<CliLvqLabelledPoint>^ retval = gcnew array<CliLvqLabelledPoint>(static_cast<int>(labels.size()));
		for(int i=0;i<static_cast<int>(labels.size()); ++i) {
			cppToCli(pointmatrix.col(i), retval[i].point);
			cppToCli(labels[i], retval[i].label);
		}
		return retval;
	}

	ModelProjection LvqModelCli::CurrentProjectionAndPrototypes(LvqDatasetCli^ dataset) {
		if(modelCopy==nullptr || !IsProjectionModel) return ModelProjection();

		Matrix_2N points(LVQ_LOW_DIM_SPACE,(size_t)dataset->PointCount), prototypes(LVQ_LOW_DIM_SPACE,(size_t)protoCount);
		vector<int> pointLabels(dataset->PointCount), protoLabels(protoCount);

		GetPointLabels(dataset->GetTrainingDataset(), &pointLabels[0]);
		msclr::lock l(copySync);
		ProjectPoints(modelCopy->get(), dataset->GetTrainingDataset(), points.data());
		ProjectPrototypes(modelCopy->get(), prototypes.data());
		GetPrototypeLabels(modelCopy->get(), &protoLabels[0]);
		l.release();

		return ModelProjection(ToCliLabelledPoints(points, pointLabels) ,ToCliLabelledPoints(prototypes,protoLabels));
	}

	array<int>^ LvqModelCli::PrototypeLabels::get() {
		array<int>^ retval = gcnew array<int>(protoCount);
		pin_ptr<int> pinRetval = &retval[0];
		msclr::lock l(copySync);
		GetPrototypeLabels(modelCopy->get(), pinRetval);
		GC::KeepAlive(this);
		return retval;
	}

	MatrixContainer<unsigned char> LvqModelCli::ClassBoundaries(double x0, double x1, double y0, double y1, int xCols, int yRows) {
		if(modelCopy==nullptr || !IsProjectionModel || !xCols || !yRows) return MatrixContainer<unsigned char>();

		MatrixContainer<unsigned char> image(yRows, xCols);
		pin_ptr<unsigned char> pinImage = &image.arr[0];
		msclr::lock l(copySync);
		LvqModel* clone=CloneLvqModel(modelCopy->get());
		l.release();
		::ClassBoundaries(clone,x0,x1,y0,y1,xCols,yRows, pinImage);
		FreeModel(clone);
		return image;

		//LvqProjectionModel::ClassDiagramT classDiagram(yRows,xCols);
		//boost::scoped_ptr<LvqProjectionModel> projectionModelClone(static_cast<LvqProjectionModel*>(projectionModelCopy->clone()));
		//projectionModelClone->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		//return ToCli<MatrixContainer<unsigned char> >::From(classDiagram.transpose());
	}

	void LvqModelCli::Train(int epochsToDo,LvqDatasetCli^ trainingSet, int datafold){
		trainingSet->LastModel = this;
		StatCollector statCollector;
		LvqModel* nativeModel=model->get();
		msclr::lock l(trainSync);
		TrainModel(trainingSet->GetTrainingDataset(), trainingSet->GetTestDataset(), datafold, trainingSet->Folds(), nativeModel,epochsToDo, StatCallbackTrampoline, &statCollector);
		GC::KeepAlive(trainingSet);
		msclr::lock l2(copySync);
		SinkStats(stats, statCollector.statsList);
		CopyLvqModel((LvqModel const *)nativeModel,modelCopy->get());
		GC::KeepAlive(this);
	}

	void LvqModelCli::TrainUpto(int epochsToReach,LvqDatasetCli^ trainingSet, int datafold){
		msclr::lock l(trainSync);
		Train(epochsToReach - GetEpochsTrained(model->get()), trainingSet,datafold);
		GC::KeepAlive(this);
	}
}