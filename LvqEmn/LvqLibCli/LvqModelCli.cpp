#include "stdafx.h"

#include <boost/scoped_ptr.hpp>
#include "LvqModelCli.h"
#include "LvqModelSettingsCli.h"
#include "LvqProjectionModel.h"
#include "LvqDatasetCli.h"
#include "LvqDataset.h"
#include "SmartSum.h"
#include "utils.h"

namespace LvqLibCli {
	using boost::mt19937;

	int LvqModelCli::ClassCount::get(){return model->get()->ClassCount();}
	int LvqModelCli::Dimensions::get(){return model->get()->Dimensions();}
	double LvqModelCli::UnscaledLearningRate::get() {return modelCopy->get()->unscaledLearningRate(); }
	bool LvqModelCli::IsProjectionModel::get(){return nullptr != dynamic_cast<LvqProjectionModel*>(model->get()); }

	bool LvqModelCli::FitsDataShape(LvqDatasetCli^ dataset) {return dataset!=nullptr && dataset->ClassCount == this->ClassCount && dataset->Dimensions == this->Dimensions;}


	LvqModelCli::LvqModelCli(String^ label, LvqDatasetCli^ trainingSet,int datafold, LvqModelSettingsCli^ modelSettings)
		: label(label)
		, initSet(trainingSet)
		, trainSync(gcnew Object())
		, copySync(gcnew Object())
	{ 
		msclr::lock l(trainSync);
		trainingSet->LastModel = this;
		model = GcPtr::Create(ConstructLvqModel(as_lvalue( modelSettings->ToNativeSettings(trainingSet, datafold))));
		model->get()->AddTrainingStat(trainingSet->GetDataset(),trainingSet->GetTrainingSubset(datafold), trainingSet->GetDataset(), trainingSet->GetTestSubset(datafold),0,0.0);
		msclr::lock l2(copySync);
		modelCopy = GcPtr::Create(model->get()->clone());
	}

	LvqTrainingStatCli LvqModelCli::GetTrainingStat(int statI){
		if(modelCopy==nullptr)LvqTrainingStatCli();

		msclr::lock l(copySync);
		LvqModel* currentBackup = modelCopy->get();

		return toCli(currentBackup->TrainingStats()[statI]);
	}

	int LvqModelCli::TrainingStatCount::get(){
		msclr::lock l(copySync);
		return modelCopy!=nullptr ?static_cast<int>(modelCopy->get()->TrainingStats().size()):0;
	}

	array<LvqTrainingStatCli>^ LvqModelCli::GetTrainingStatsAfter(int statI) {
		if(modelCopy==nullptr)LvqTrainingStatCli();
		msclr::lock l(copySync);
		LvqModel* currentBackup = modelCopy->get();
		int maxStat = std::max(statI, (int)currentBackup->TrainingStats().size());

		array<LvqTrainingStatCli>^ stats = gcnew array<LvqTrainingStatCli>(maxStat-statI);
		for(int i=0;i<stats->Length;++i)
			stats[i] = toCli(currentBackup->TrainingStats()[statI+i]);
		return stats;
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


	ModelProjection LvqModelCli::CurrentProjectionAndPrototypes( LvqDatasetCli^ dataset){
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

	array<int,2>^ LvqModelCli::ClassBoundaries( double x0, double x1, double y0, double y1,int xCols, int yRows) {
		LvqProjectionModel::ClassDiagramT classDiagram(yRows,xCols);
		if(modelCopy==nullptr) return nullptr;
		LvqProjectionModel* projectionModelCopy = dynamic_cast<LvqProjectionModel*>(modelCopy->get());
		if(!projectionModelCopy) return nullptr;

		msclr::lock l(copySync);
		boost::scoped_ptr<LvqProjectionModel> projectionModelClone(static_cast<LvqProjectionModel*>(projectionModelCopy->clone()));
		l.release();

		projectionModelClone->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		return ToCli<array<int,2>^>::From(classDiagram.transpose());
	}

	void LvqModelCli::Train(int epochsToDo,LvqDatasetCli^ trainingSet, int datafold){
		trainingSet->LastModel = this;
		msclr::lock l(trainSync);
		trainingSet->GetDataset()->TrainModel(epochsToDo, model->get(), trainingSet->GetTrainingSubset(datafold), trainingSet->GetDataset(), trainingSet->GetTestSubset(datafold));
		msclr::lock l2(copySync);
		model->get()->CopyTo(*modelCopy->get());
	}

	void LvqModelCli::TrainUpto(int epochsToReach,LvqDatasetCli^ trainingSet, int datafold){
		trainingSet->LastModel = this;
		msclr::lock l(trainSync);
		trainingSet->GetDataset()->TrainModel(epochsToReach - model->get()->epochsTrained, model->get(), trainingSet->GetTrainingSubset(datafold), trainingSet->GetDataset(), trainingSet->GetTestSubset(datafold));
		msclr::lock l2(copySync);
		model->get()->CopyTo(*modelCopy->get());
	}
}