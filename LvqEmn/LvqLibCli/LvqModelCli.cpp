#include "stdafx.h"

#include <boost/scoped_ptr.hpp>
#include "LvqModelCli.h"
#include "LvqModelSettingsCli.h"
#include "LvqProjectionModel.h"
#include "LvqDatasetCli.h"
#include "LvqDataset.h"
#include "SmartSum.h"
#include "utils.h"

using namespace System::Linq;

namespace LvqLibCli {
	using boost::mt19937;

	int LvqModelCli::ClassCount::get(){return model->get()->ClassCount();}
	int LvqModelCli::Dimensions::get(){return model->get()->Dimensions();}
	double LvqModelCli::UnscaledLearningRate::get() {return modelCopy->get()->unscaledLearningRate(); }
	bool LvqModelCli::IsProjectionModel::get(){return nullptr != dynamic_cast<LvqProjectionModel*>(model->get()); }

	bool LvqModelCli::FitsDataShape(LvqDatasetCli^ dataset) {return dataset!=nullptr && dataset->ClassCount == this->ClassCount && dataset->Dimensions == this->Dimensions;}

	void LvqModelCli::SinkStats(LvqModel::Statistics & nativeStats) {
		while(!nativeStats.empty()) {
			LvqTrainingStatCli cliStat;
			cppToCli(nativeStats.front(),cliStat);
			nativeStats.pop();
			stats->Add(cliStat);
		}
	}

	LvqTrainingStatCli LvqModelCli::EvaluateStats(LvqDatasetCli^ dataset, int datafold){
		LvqModel::Statistics nativeStats;
		msclr::lock l2(copySync);
		modelCopy->get()->AddTrainingStat(nativeStats, dataset->GetDataset(), dataset->GetTrainingSubset(datafold), dataset->GetDataset(), dataset->GetTestSubset(datafold));
		LvqTrainingStatCli cliStat;
		cppToCli(nativeStats.front(),cliStat);
		return cliStat;
	}


	LvqModelCli::LvqModelCli(String^ label, LvqDatasetCli^ trainingSet,int datafold, LvqModelSettingsCli^ modelSettings)
		: label(label)
		, initSet(trainingSet)
		, initDataFold(datafold)
		, trainSync(gcnew Object())
		, copySync(gcnew Object())
		,stats(gcnew List<LvqTrainingStatCli>())
	{ 
		msclr::lock l(trainSync);
		trainingSet->LastModel = this;
		model = GcPtr::Create(ConstructLvqModel(as_lvalue( modelSettings->ToNativeSettings(trainingSet, datafold))));
		LvqModel::Statistics nativeStats;
		model->get()->AddTrainingStat(nativeStats,trainingSet->GetDataset(),trainingSet->GetTrainingSubset(datafold), trainingSet->GetDataset(), trainingSet->GetTestSubset(datafold));

		msclr::lock l2(copySync);
		SinkStats(nativeStats);
		modelCopy = GcPtr::Create(model->get()->clone());
	}

	LvqTrainingStatCli LvqModelCli::GetTrainingStat(int statI){
		msclr::lock l(copySync);
		return stats[statI];
	}

	int LvqModelCli::TrainingStatCount::get(){
		msclr::lock l(copySync);
		return stats->Count;
	}

	array<LvqTrainingStatCli>^ LvqModelCli::TrainingStats::get() {
		msclr::lock l(copySync);
		auto copy = gcnew array<LvqTrainingStatCli>(stats->Count);
		stats->CopyTo(copy);
		return copy;
	}

	array<LvqTrainingStatCli>^ LvqModelCli::GetTrainingStatsAfter(int statI) {
		msclr::lock l(copySync);
		return Enumerable::ToArray(Enumerable::Skip(stats,statI));
	}


	array<String^>^ LvqModelCli::TrainingStatNames::get() {
		return ToCli<array<String^>^>::From(model->get()->TrainingStatNames());
	}

	void LvqModelCli::ResetLearningRate() { msclr::lock l(trainSync); model->get()->resetLearningRate(); }

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
		if(modelCopy==nullptr) return ModelProjection();
		LvqProjectionModel* projectionModel = dynamic_cast<LvqProjectionModel*>(modelCopy->get());
		if(projectionModel==nullptr) return ModelProjection();

		auto labels = dataset->GetDataset()->getPointLabels();

		msclr::lock l(copySync);
		auto points = dataset->GetDataset()->ProjectPoints(projectionModel);
		auto prototypes = projectionModel->GetProjectedPrototypes();
		auto prototypelabels = projectionModel->GetPrototypeLabels();
		l.release();

		return ModelProjection(ToCliLabelledPoints(points, labels) ,ToCliLabelledPoints(prototypes,prototypelabels));
	}

	array<int>^ LvqModelCli::PrototypeLabels::get() { array<int>^ retval; cppToCli(modelCopy->get()->GetPrototypeLabels(),retval); return retval;}

	MatrixContainer<unsigned char> LvqModelCli::ClassBoundaries(double x0, double x1, double y0, double y1, int xCols, int yRows) {
		LvqProjectionModel::ClassDiagramT classDiagram(yRows,xCols);
		if(modelCopy==nullptr) return MatrixContainer<unsigned char>();
		LvqProjectionModel* projectionModelCopy = dynamic_cast<LvqProjectionModel*>(modelCopy->get());
		if(!projectionModelCopy) return MatrixContainer<unsigned char>();

		msclr::lock l(copySync);
		boost::scoped_ptr<LvqProjectionModel> projectionModelClone(static_cast<LvqProjectionModel*>(projectionModelCopy->clone()));
		l.release();

		projectionModelClone->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		return ToCli<MatrixContainer<unsigned char> >::From(classDiagram.transpose());
	}

	void LvqModelCli::Train(int epochsToDo,LvqDatasetCli^ trainingSet, int datafold){
		trainingSet->LastModel = this;
		msclr::lock l(trainSync);
		LvqModel::Statistics statsink;
		trainingSet->GetDataset()->TrainModel(epochsToDo, model->get(), &statsink, trainingSet->GetTrainingSubset(datafold), trainingSet->GetDataset(), trainingSet->GetTestSubset(datafold));
		msclr::lock l2(copySync);
		SinkStats(statsink);
		model->get()->CopyTo(*modelCopy->get());
	}

	void LvqModelCli::TrainUpto(int epochsToReach,LvqDatasetCli^ trainingSet, int datafold){
		msclr::lock l(trainSync);
		Train(epochsToReach - model->get()->epochsTrained,trainingSet,datafold);
	}
}