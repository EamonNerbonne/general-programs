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
		#pragma omp parallel for
		for(int i=0;i<model->Length;i++) {
			LvqModel* newmodel=0;
			if(modelType == LvqModelCli::GSM_TYPE)
		 		newmodel = new GsmLvqModel(mt19937(rngParamsSeed+i),mt19937(rngInstSeed+i), true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans( trainingSet->GetTrainingSubset(i) )); 
			else if(modelType == LvqModelCli::G2M_TYPE)
				newmodel = new G2mLvqModel(mt19937(rngParamsSeed+i),mt19937(rngInstSeed+i), true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans(trainingSet->GetTrainingSubset(i))); 
			else  if(modelType == LvqModelCli::GM_TYPE)
				newmodel = new GmLvqModel(mt19937(rngParamsSeed+i),mt19937(rngInstSeed+i), true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans(trainingSet->GetTrainingSubset(i))); 
			if(newmodel) {
				WrappedModel^ m = GcPtr::Create(newmodel);
				m->get()->AddTrainingStat(trainingSet->GetDataset(),trainingSet->GetTrainingSubset(i), trainingSet->GetDataset(), trainingSet->GetTestSubset(i),0,0.0);
				model[i] = m;
			}
		}
		BackupModel();
	}

	array<LvqTrainingStatCli>^ LvqModelCli :: TrainingStats::get(){
		using System::Collections::Generic::List;
		WrappedModelArray^ currentBackup = modelCopy;
		msclr::lock l2(currentBackup);
		int statCount =int( currentBackup[0]->get()->TrainingStats().size());
		int statDim = statCount==0?0:  int(currentBackup[0]->get()->TrainingStats()[0].values.size());
		if(currentBackup->Length ==1) {
			return ToCli<array<LvqTrainingStatCli>^>::From(currentBackup[0]->get()->TrainingStats());
		}
		Eigen::VectorXd zero = VectorXd::Zero(statDim);
		array<LvqTrainingStatCli>^ retval = gcnew array<LvqTrainingStatCli>(statCount);
		for(int si=0;si<statCount;++si) {
			SmartSum<Eigen::VectorXd> stat(zero);
			for each(WrappedModel^ m in currentBackup)
				stat.CombineWith(m->get()->TrainingStats()[si].values,1.0);
			retval[si] = LvqTrainingStatCli::toCli(currentBackup[0]->get()->TrainingStats()[si].trainingIter, stat.GetMean(), (stat.GetSampleVariance().array().sqrt() * (1.0/sqrt(stat.GetWeight()))).matrix() );
		}
		GC::KeepAlive(currentBackup);
		return retval;
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
		LvqProjectionModel* projectionModel = dynamic_cast<LvqProjectionModel*>( currentBackup[0]->get());

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

	void LvqModelCli::BackupModel() {
		msclr::lock l2(mainSync); 
		WrappedModelArray^ newBackup = gcnew WrappedModelArray(model->Length);
		msclr::lock l(newBackup);//necessary?
		for(int i=0;i<newBackup->Length;i++)
			newBackup[i] = GcPtr::Create(model[i]->get()->clone());
		modelCopy = newBackup;
	}

	array<int,2>^ LvqModelCli::ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows) {
		MatrixXi classDiagram(yRows,xCols);
		{
			WrappedModelArray^ currentBackup = modelCopy;

			if(currentBackup==nullptr)
				return nullptr; //TODO: should never happen?

			msclr::lock l(currentBackup);
			LvqProjectionModel* projectionModel = dynamic_cast<LvqProjectionModel*>( currentBackup[0]->get());
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

	int LvqModelCli::OtherStatCount() {
		WrappedModelArray^ backupCopy=modelCopy;
		msclr::lock l(backupCopy); 
		return static_cast<int>(backupCopy[0]->get()->otherStats().size() - LvqTrainingStats::Extra );
	}

	
	Tuple<array<double,2>^, array<int>^>^ LvqModelCli::PrototypePositions() {
		WrappedModelArray^ currentBackup = modelCopy;
		msclr::lock l(currentBackup);
		LvqProjectionModel* projectionModel = dynamic_cast<LvqProjectionModel*>( currentBackup[0]->get());
		if(!projectionModel) return nullptr;
		return Tuple::Create(ToCli<array<double,2>^>::From(projectionModel->GetProjectedPrototypes()), ToCli<array<int>^>::From(projectionModel->GetPrototypeLabels()));
	}
}