#include "stdafx.h"

#include "LvqModelCli.h"
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"
#include "GmLvqModel.h"

namespace LvqLibCli {
	using boost::mt19937;

	LvqModelCli::LvqModelCli(String^ label, unsigned rngParamsSeed, unsigned rngInstSeed, int protosPerClass, int modelType,int parallelModels,LvqDatasetCli^ trainingSet)
		: protosPerClass(protosPerClass)
		, modelType(modelType)
		, label(label)
		, model(gcnew WrappedModelArray(parallelModels) )
		, modelCopy(nullptr)
		, mainSync(gcnew Object())
	{ 
		vector<int> protoDistrib;
		for(int i=0;i<trainingSet->ClassCount;++i)
			protoDistrib.push_back(protosPerClass);

		msclr::lock l2(mainSync);
		initSet = trainingSet;
		for(int i=0;i<model->Length;i++) {
			AbstractLvqModel* newmodel;
			if(modelType == LvqModelCli::GSM_TYPE)
		 		newmodel = new GsmLvqModel(mt19937(rngParamsSeed+i),mt19937(rngInstSeed+i), true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans( trainingSet->GetDataset()->entireSet() )); 
			else if(modelType == LvqModelCli::G2M_TYPE)
				newmodel = new G2mLvqModel(mt19937(rngParamsSeed+i),mt19937(rngInstSeed+i), true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans(trainingSet->GetDataset()->entireSet())); 
			else  if(modelType == LvqModelCli::GM_TYPE)
				newmodel = new GmLvqModel(mt19937(rngParamsSeed+i),mt19937(rngInstSeed+i), true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans(trainingSet->GetDataset()->entireSet())); 
			else
				return;
			model[i] = GcPtr::Create(newmodel);
			model[i]->get()->AddTrainingStat( trainingSet->GetDataset(),trainingSet->GetDataset()->entireSet(),(LvqDataset*) 0, vector<int>(),0,0.0);
		}
		BackupModel();
	}
	array<LvqTrainingStatCli>^ LvqModelCli :: TrainingStats::get(){
		using System::Collections::Generic::List;
		WrappedModelArray^ currentBackup = modelCopy;
		msclr::lock l2(currentBackup);
		int statCount =int( currentBackup[0]->get()->trainingStats.size());
		int statDim = statCount==0?0:  int(currentBackup[0]->get()->trainingStats[0].values.size());
		if(currentBackup->Length ==1) {
			return ToCli<array<LvqTrainingStatCli>^>::From(currentBackup[0]->get()->trainingStats);
		}
		Eigen::VectorXd zero = VectorXd::Zero(statDim);
		List<LvqTrainingStatCli>^ retval = gcnew List<LvqTrainingStatCli>();
		for(int si=0;si<statCount;++si) {
			SmartSum<Eigen::VectorXd> stat(zero);
				for each(WrappedModel^ m in currentBackup)
					stat.CombineWith(m->get()->trainingStats[si].values,1.0);
				retval->Add(LvqTrainingStatCli::toCli(currentBackup[0]->get()->trainingStats[si].trainingIter, stat.GetMean(),stat.GetVariance()));
		}
		return retval->ToArray();
	}

	void LvqModelCli::ResetLearningRate() {
		msclr::lock l2(mainSync); 
		for each(WrappedModel ^ m in model) 
			m->get()->resetLearningRate();
	}


	array<double,2>^ LvqModelCli::CurrentProjectionOf(LvqDatasetCli^ dataset) { 
		WrappedModelArray^ currentBackup = modelCopy;

		if(currentBackup==nullptr)
			return nullptr;
		msclr::lock l(currentBackup);
		AbstractProjectionLvqModel* projectionModel = dynamic_cast<AbstractProjectionLvqModel*>( currentBackup[0]->get());

		return projectionModel? ToCli<array<double,2>^>::From(dataset->GetDataset()->ProjectPoints(projectionModel)):nullptr ; 
	}

	ModelProjection LvqModelCli::CurrentProjectionAndPrototypes(LvqDatasetCli^ dataset){
		ModelProjection retval;
		WrappedModelArray^ currentBackup = modelCopy;
		if(currentBackup != nullptr) {
			msclr::lock l(currentBackup);
			retval.Data.Points = CurrentProjectionOf(dataset);
			if(retval.Data.Points ==  nullptr)
				return retval;
			retval.Data.ClassLabels = dataset->ClassLabels();
			Tuple<array<double,2>^,array<int>^>^ protos = PrototypePositions();
			retval.Prototypes.Points = protos->Item1;
			retval.Prototypes.ClassLabels = protos->Item2;
			retval.IsOk = true;
		}
		return retval;
	}


	array<int,2>^ LvqModelCli::ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows) {
		MatrixXi classDiagram(yRows,xCols);
		{
			WrappedModelArray^ currentBackup = modelCopy;

			if(currentBackup==nullptr)
				return nullptr; //TODO: should never happen?

			msclr::lock l(currentBackup);
			AbstractProjectionLvqModel* projectionModel = dynamic_cast<AbstractProjectionLvqModel*>( currentBackup[0]->get());
			if(!projectionModel) return nullptr;
			projectionModel->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		}
		return ToCli<array<int,2>^>::From(classDiagram.transpose());
	}

	void LvqModelCli::Train(int epochsToDo,LvqDatasetCli^ trainingSet){
		msclr::lock l(mainSync);
		trainingSet->LastModel = this;
#pragma omp parallel for
		for(int i=0;i<model->Length;i++)
		trainingSet->GetDataset()->TrainModel(epochsToDo,  model[i]->get(), trainingSet->GetDataset()->entireSet(), 0, vector<int>());
		BackupModel();
	}

	int LvqModelCli::OtherStatCount() {
		WrappedModelArray^ backupCopy=modelCopy;
		msclr::lock l(backupCopy); 
		return static_cast<int>(backupCopy[0]->get()->otherStats().size() - LvqTrainingStats::Extra );
	}

	
	Tuple<array<double,2>^, array<int>^>^ LvqModelCli::PrototypePositions() {
		WrappedModelArray^ currentBackup = modelCopy;
		msclr::lock l(currentBackup);
		AbstractProjectionLvqModel* projectionModel = dynamic_cast<AbstractProjectionLvqModel*>( currentBackup[0]->get());
		if(!projectionModel) return nullptr;
		return Tuple::Create(ToCli<array<double,2>^>::From(projectionModel->GetProjectedPrototypes()), ToCli<array<int>^>::From(projectionModel->GetPrototypeLabels()));
	}
}