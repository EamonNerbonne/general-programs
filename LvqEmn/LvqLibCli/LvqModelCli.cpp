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

	LvqModelCli::LvqModelCli(String^ label, int parallelModels, LvqDatasetCli^ trainingSet, LvqModelSettingsCli^ modelSettings)
		: label(label)
		, model(gcnew WrappedModelArray(parallelModels) )
		, modelCopy(nullptr)
		, mainSync(gcnew Object())
		, initSet(trainingSet)
		, cachedTrainingStats(gcnew List<LvqTrainingStatCli>())
	{ 
		msclr::lock l2(mainSync);
		#pragma omp parallel for
		for(int i=0;i<model->Length;i++) {
			WrappedModel^ m = GcPtr::Create(ConstructLvqModel(as_lvalue( modelSettings->ToNativeSettings(trainingSet, i))));
			m->get()->AddTrainingStat(trainingSet->GetDataset(),trainingSet->GetTrainingSubset(i), trainingSet->GetDataset(), trainingSet->GetTestSubset(i),0,0.0);
			model[i] = m;
		}
		BackupModel();
	}

	IEnumerable<LvqTrainingStatCli>^ LvqModelCli :: TrainingStats::get(){
		using System::Collections::Generic::List;
		WrappedModelArray^ currentBackup = modelCopy;
		msclr::lock l2(currentBackup);
		int statCount =int(currentBackup[0]->get()->TrainingStats().size());
		int statDim = statCount==0  ?  0  : int(currentBackup[0]->get()->TrainingStatNames().size());
		if(currentBackup->Length ==1) {
			auto const & trainingStats = currentBackup[0]->get()->TrainingStats();
			for(int si=cachedTrainingStats->Count;si<statCount;++si) 
				cachedTrainingStats->Add(LvqTrainingStatCli::toCli(trainingStats[si]));
			return cachedTrainingStats;
		}
		
		SmartSum<Eigen::Dynamic> stat(statDim);
		for(int si=cachedTrainingStats->Count;si<statCount;++si) {
			stat.Reset();	
			for each(WrappedModel^ m in currentBackup)
				stat.CombineWith(m->get()->TrainingStats()[si],1.0);
			cachedTrainingStats->Add(
				LvqTrainingStatCli::toCli(stat.GetMean(), (stat.GetSampleVariance().array().sqrt() * (1.0/sqrt(stat.GetWeight()))).matrix() )
				);
		}
		GC::KeepAlive(currentBackup);
		return cachedTrainingStats;
	}

	void LvqModelCli::ResetLearningRate() {
		msclr::lock l2(mainSync); 
		for each(WrappedModel ^ m in model) 
			m->get()->resetLearningRate();
	}


	array<double,2>^ LvqModelCli::CurrentProjectionOf(int modelIdx, LvqDatasetCli^ dataset) { 
		WrappedModelArray^ currentBackup = modelCopy;

		if(currentBackup==nullptr)
			return nullptr;
		msclr::lock l(currentBackup);
		LvqProjectionModel* projectionModel = dynamic_cast<LvqProjectionModel*>( currentBackup[modelIdx]->get());

		return projectionModel ? ToCli<array<double,2>^>::From(dataset->GetDataset()->ProjectPoints(projectionModel)) : nullptr ; 
	}

	ModelProjection LvqModelCli::CurrentProjectionAndPrototypes(int modelIdx, LvqDatasetCli^ dataset){
		ModelProjection retval;
		WrappedModelArray^ currentBackup = modelCopy;
		if(currentBackup != nullptr) {
			msclr::lock l(currentBackup);
			retval.Data.Points = CurrentProjectionOf(modelIdx,dataset);
			if(retval.Data.Points ==  nullptr)
				return retval;
			retval.Data.ClassLabels = dataset->ClassLabels();
			Tuple<array<double,2>^,array<int>^>^ protos = PrototypePositions(modelIdx);
			retval.Prototypes.Points = protos->Item1;
			retval.Prototypes.ClassLabels = protos->Item2;
			retval.IsOk = true;
		}
		return retval;
	}

	void LvqModelCli::BackupModel() {
		msclr::lock l2(mainSync); 
		WrappedModelArray^ newBackup = gcnew WrappedModelArray(model->Length);
		msclr::lock l(newBackup);//necessary?
		for(int i=0;i<newBackup->Length;i++)
			newBackup[i] = GcPtr::Create(model[i]->get()->clone());
		modelCopy = newBackup;
	}

	array<int,2>^ LvqModelCli::ClassBoundaries(int modelIdx, double x0, double x1, double y0, double y1,int xCols, int yRows) {
		LvqProjectionModel::ClassDiagramT classDiagram(yRows,xCols);
		{
			WrappedModelArray^ currentBackup = modelCopy;

			if(currentBackup==nullptr)
				return nullptr; //TODO: should never happen?

			msclr::lock l(currentBackup);
			LvqProjectionModel* projectionModel = dynamic_cast<LvqProjectionModel*>( currentBackup[modelIdx]->get());
			if(!projectionModel) return nullptr;
			projectionModel->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		}
		return ToCli<array<int,2>^>::From(classDiagram.transpose());
	}

	void LvqModelCli::Train(int epochsToDo,LvqDatasetCli^ dataSet){
		msclr::lock l(mainSync);
		dataSet->LastModel = this;
		#pragma omp parallel for
		for(int i=0;i<model->Length;i++)
			dataSet->GetDataset()->TrainModel(epochsToDo,  model[i]->get(), dataSet->GetTrainingSubset(i), dataSet->GetDataset(), dataSet->GetTestSubset(i));
		BackupModel();
	}

	array<String^>^ LvqModelCli::TrainingStatNames::get() {
		WrappedModelArray^ backupCopy=modelCopy;
		msclr::lock l(backupCopy); 
		return ToCli<array<String^>^>::From(backupCopy[0]->get()->TrainingStatNames());
	}

	
	Tuple<array<double,2>^, array<int>^>^ LvqModelCli::PrototypePositions(int modelIdx) {
		WrappedModelArray^ currentBackup = modelCopy;
		msclr::lock l(currentBackup);
		LvqProjectionModel* projectionModel = dynamic_cast<LvqProjectionModel*>(currentBackup[modelIdx]->get());
		if(!projectionModel) return nullptr;
		return Tuple::Create(ToCli<array<double,2>^>::From(projectionModel->GetProjectedPrototypes()), ToCli<array<int>^>::From(projectionModel->GetPrototypeLabels()));
	}

	int LvqModelCli::ClassCount::get(){return model[0]->get()->ClassCount();}
	int LvqModelCli::Dimensions::get(){return model[0]->get()->Dimensions();}
	bool LvqModelCli::IsMultiModel::get(){return model->Length > 1;}
	int LvqModelCli::ModelCount::get() {return model->Length;}

	bool LvqModelCli::FitsDataShape(LvqDatasetCli^ dataset) {return dataset!=nullptr && dataset->ClassCount == this->ClassCount && dataset->Dimensions == this->Dimensions;}

}