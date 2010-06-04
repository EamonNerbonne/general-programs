#include "stdafx.h"

#include "LvqModelCli.h"
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"
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

		AbstractProjectionLvqModel* newmodel;
        if(modelType == LvqModelCli::GSM_TYPE)
		 	newmodel = new GsmLvqModel(*rngParam, true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans()); 
		else if(modelType == LvqModelCli::G2M_TYPE)
			newmodel = new G2mLvqModel(*rngParam, true, protoDistrib, trainingSet->GetDataset()->ComputeClassMeans()); 
		else return;
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

		return cppToCli(dataset->GetDataset()->ProjectPoints(currentBackup->get())); 
	}

	array<int,2>^ LvqModelCli::ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows) {
		MatrixXi classDiagram(yRows,xCols);
		{
			WrappedModel^ currentBackup = modelCopy;

			if(currentBackup==nullptr)
				return nullptr; //TODO: should never happen?

			msclr::lock l(currentBackup);
			currentBackup->get()->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		}
		return cppToCli(classDiagram.transpose());
	}

	void LvqModelCli::Train(int epochsToDo,LvqDatasetCli^ trainingSet){
		msclr::lock l(mainSync);
		trainingSet->LastModel = this;
		if(modelCopy==nullptr)
			Init(trainingSet);

		trainingSet->GetDataset()->TrainModel(epochsToDo,  *rngIter, model->get());
		BackupModel();
	}
	
	Tuple<array<double,2>^, array<int>^>^ LvqModelCli::PrototypePositions() {
		WrappedModel^ currentBackup = modelCopy;
		msclr::lock l(currentBackup);
		return Tuple::Create(cppToCli( currentBackup->get()->GetProjectedPrototypes()), cppToCli(currentBackup->get()->GetPrototypeLabels()));
	}
}