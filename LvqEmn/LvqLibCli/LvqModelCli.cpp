#include "stdafx.h"

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
	double LvqModelCli::CurrentLearningRate::get() {return modelCopy->get()->currentLearningRate(); }
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
		auto modelCopyRef = modelCopy;
		if(modelCopyRef!=nullptr) {
			msclr::lock l(copySync);
			LvqModel* currentBackup = modelCopyRef->get();

			return toCli(currentBackup->TrainingStats()[statI]);
		}
		GC::KeepAlive(modelCopyRef);
		return LvqTrainingStatCli();
	}

	int LvqModelCli::TrainingStatCount::get(){
		msclr::lock l(copySync);
		auto modelCopyRef = modelCopy;
		return modelCopyRef!=nullptr ?static_cast<int>( modelCopyRef->get()->TrainingStats().size()):0;
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
		msclr::lock l(copySync);
		auto modelCopyRef = modelCopy;
		if(modelCopyRef==nullptr) return ModelProjection();
		LvqProjectionModel* projectionModel = dynamic_cast<LvqProjectionModel*>(modelCopyRef->get());
		if(projectionModel==nullptr) return ModelProjection();
		auto projection =  ToCliLabelledPoints(dataset->GetDataset()->ProjectPoints(projectionModel), dataset->GetDataset()->getPointLabels()) ;
		auto prototypes = ToCliLabelledPoints(projectionModel->GetProjectedPrototypes(),projectionModel->GetPrototypeLabels());

		auto retval = ModelProjection(projection,prototypes);
		GC::KeepAlive(modelCopyRef);
		return retval;
	}

	array<int,2>^ LvqModelCli::ClassBoundaries( double x0, double x1, double y0, double y1,int xCols, int yRows) {
		LvqProjectionModel::ClassDiagramT classDiagram(yRows,xCols);
		{
			auto modelCopyRef = modelCopy;

			if(modelCopyRef==nullptr) return nullptr;

			LvqProjectionModel* projectionModel = dynamic_cast<LvqProjectionModel*>( modelCopyRef->get());
			if(!projectionModel) return nullptr;
			msclr::lock l(copySync);
			projectionModel->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
			GC::KeepAlive(modelCopyRef);
		}
		return ToCli<array<int,2>^>::From(classDiagram.transpose());
	}

	void LvqModelCli::Train(int epochsToDo,LvqDatasetCli^ trainingSet, int datafold){
		trainingSet->LastModel = this;
		msclr::lock l(trainSync);
		trainingSet->GetDataset()->TrainModel(epochsToDo,  model->get(), trainingSet->GetTrainingSubset(datafold), trainingSet->GetDataset(), trainingSet->GetTestSubset(datafold));
		msclr::lock l2(copySync);
		model->get()->CopyTo(*modelCopy->get());
	}

	void LvqModelCli::TrainUpto(int epochsToReach,LvqDatasetCli^ trainingSet, int datafold){
		trainingSet->LastModel = this;
		msclr::lock l(trainSync);
		trainingSet->GetDataset()->TrainModel(epochsToReach - model->get()->epochsTrained,  model->get(), trainingSet->GetTrainingSubset(datafold), trainingSet->GetDataset(), trainingSet->GetTestSubset(datafold));
		msclr::lock l2(copySync);
		model->get()->CopyTo(*modelCopy->get());
		//newBackup = GcPtr::Create(model->get()->clone());
	}

}