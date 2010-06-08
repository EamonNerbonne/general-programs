#include "stdafx.h"

#include "LvqModelCli.h"
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"
#include "GmLvqModel.h"

#include "WrappingUtils.h"

namespace LvqLibCli {
	using boost::mt19937;

	LvqModelCli::LvqModelCli(String^ label, unsigned rngParamsSeed, unsigned rngInstSeed, int protosPerClass, int modelType,LvqDatasetCli^ trainingSet)
		: protosPerClass(protosPerClass)
		, modelType(modelType)
		, label(label)
		, model(nullptr)
		, modelCopy(nullptr)
		, rngParam(new mt19937(rngParamsSeed))
		, rngIter(new mt19937(rngInstSeed))
		, mainSync(gcnew Object())
	{ 
		Init(trainingSet);
	}

	void LvqModelCli::Init(LvqDatasetCli^ trainingSet)
	{
		vector<int> protoDistrib;
		for(int i=0;i<trainingSet->ClassCount;++i)
			protoDistrib.push_back(protosPerClass);


		msclr::lock l2(mainSync);
		initSet = trainingSet;
		model=nullptr;
		modelCopy=nullptr;

		AbstractLvqModel* newmodel;
        if(modelType == LvqModelCli::GSM_TYPE)
		 	newmodel = new GsmLvqModel(*rngParam, true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans()); 
		else if(modelType == LvqModelCli::G2M_TYPE)
			newmodel = new G2mLvqModel(*rngParam, true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans()); 
		else  if(modelType == LvqModelCli::GM_TYPE)
			newmodel = new GmLvqModel(*rngParam, true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans()); 
		else
			return;
		model = GcPtr::Create(newmodel);
		model->get()->AddTrainingStat( trainingSet->GetDataset(),0,0,0.0);

		BackupModel();
	}

	double LvqModelCli::ErrorRate(LvqDatasetCli^testSet) {
		WrappedModel^ currentBackup = modelCopy;
		if(currentBackup==nullptr)
			return 1.0;
		msclr::lock l(currentBackup);

		return testSet->GetDataset()->ErrorRate(currentBackup->get()); 
	}

	array<double,2>^ LvqModelCli::CurrentProjectionOf(LvqDatasetCli^ dataset) { 
		WrappedModel^ currentBackup = modelCopy;

		if(currentBackup==nullptr)
			return nullptr;
		msclr::lock l(currentBackup);
		AbstractProjectionLvqModel* projectionModel = dynamic_cast<AbstractProjectionLvqModel*>( currentBackup->get());

		return projectionModel? cppToCli(dataset->GetDataset()->ProjectPoints(projectionModel)):nullptr ; 
	}

	ModelProjection LvqModelCli::CurrentProjectionAndPrototypes(LvqDatasetCli^ dataset){
		ModelProjection retval;
		WrappedModel^ currentBackup = modelCopy;
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
			WrappedModel^ currentBackup = modelCopy;

			if(currentBackup==nullptr)
				return nullptr; //TODO: should never happen?

			msclr::lock l(currentBackup);
			AbstractProjectionLvqModel* projectionModel = dynamic_cast<AbstractProjectionLvqModel*>( currentBackup->get());
			if(!projectionModel) return nullptr;
			projectionModel->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		}
		return cppToCli(classDiagram.transpose());
	}

	void LvqModelCli::Train(int epochsToDo,LvqDatasetCli^ trainingSet){
		msclr::lock l(mainSync);
		trainingSet->LastModel = this;
		if(modelCopy==nullptr)
			Init(trainingSet);

		trainingSet->GetDataset()->TrainModel(epochsToDo,  *rngIter, model->get(), trainingSet->GetDataset()->entireSet(), 0, vector<int>());
		BackupModel();
	}
	
	Tuple<array<double,2>^, array<int>^>^ LvqModelCli::PrototypePositions() {
		WrappedModel^ currentBackup = modelCopy;
		msclr::lock l(currentBackup);
		AbstractProjectionLvqModel* projectionModel = dynamic_cast<AbstractProjectionLvqModel*>( currentBackup->get());
		if(!projectionModel) return nullptr;
		return Tuple::Create(cppToCli(projectionModel->GetProjectedPrototypes()), cppToCli(projectionModel->GetPrototypeLabels()));
	}
}