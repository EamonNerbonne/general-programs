#include "stdafx.h"

#include "LvqModelCli.h"
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"
#include "WrappingUtils.h"

namespace LvqLibCli {

	LvqModelCli::LvqModelCli(Func<unsigned int>^ paramSeed, Func<unsigned int>^ iterSeed, int dims, int classCount, int protosPerClass, int modelType)
		: model(NULL)
		, modelCopy(NULL)
		, rngParam(new boost::mt19937(paramSeed))
		, rngIter(new boost::mt19937(iterSeed))
		, mainSync(gcnew Object())
		, backupSync(gcnew Object())
		, nativeAllocEstimate(0)
		, dims(dims)
		, classCount(classCount)
		, protosPerClass(protosPerClass)
		, modelType(modelType)
	{
		AddPressure(sizeof(boost::mt19937)*2);
	}


	void LvqModelCli::AddPressure(size_t size) {
		nativeAllocEstimate+=size;
		GC::AddMemoryPressure(size);
	}

	void LvqModelCli::RemovePressure(size_t size) {
		nativeAllocEstimate-=size;
		GC::RemoveMemoryPressure(size);
	}

	void LvqModelCli::ReleaseModels() {
		AbstractLvqModel* tmp = model.Detach();
		if(tmp != nullptr) {
			RemovePressure(tmp->MemAllocEstimate());
			delete tmp;
		}
		tmp = modelCopy.Detach();
		if(tmp != nullptr) {
			RemovePressure(tmp->MemAllocEstimate());
			delete tmp;
		}
	}


	void LvqModelCli::Init(LvqDataSetCli^ trainingSet)
	{
		vector<int> protoDistrib;
		for(int i=0;i<classCount;++i)
			protoDistrib.push_back(protosPerClass);


		msclr::lock l(backupSync);
		msclr::lock l2(mainSync);

		ReleaseModels();

        if(modelType == LvqModelCli::GSM_TYPE)
		 	model = new GsmLvqModel(*rngParam, true, protoDistrib, trainingSet->GetDataSet()->ComputeClassMeans()); 
		else if(modelType == LvqModelCli::G2M_TYPE)
			model = new G2mLvqModel(*rngParam, true, protoDistrib, trainingSet->GetDataSet()->ComputeClassMeans()); 
		else return;

		BackupModel();
		AddPressure( model->MemAllocEstimate()  + modelCopy-> MemAllocEstimate());
	}

	LvqModelCli::!LvqModelCli() {GC::RemoveMemoryPressure(nativeAllocEstimate);}

	double LvqModelCli::ErrorRate(LvqDataSetCli^testSet) {
		msclr::lock l(backupSync);
		if(!modelCopy.get())
			return 1.0;

		return testSet->GetDataSet()->ErrorRate(modelCopy); 
	}

	array<double,2>^ LvqModelCli::CurrentProjectionOf(LvqDataSetCli^ dataset) { 
		msclr::lock l(backupSync);
		if(!modelCopy.get())
			return nullptr;

		return cppToCli(dataset->GetDataSet()->ProjectPoints(modelCopy)); 
	}

	array<int,2>^ LvqModelCli::ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows) {
		MatrixXi classDiagram(yRows,xCols);
		{
			msclr::lock l(backupSync);
			if(!modelCopy.get())
				return nullptr;

			modelCopy->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		}
		return cppToCli(classDiagram.transpose());
	}

	void LvqModelCli::Train(int epochsToDo,LvqDataSetCli^ trainingSet){
		msclr::lock l(mainSync);
		if(!model.get())
			Init(trainingSet);

		trainingSet->GetDataSet()->TrainModel(epochsToDo,  *rngIter, model);
		BackupModel();
	}
	
	Tuple<array<double,2>^, array<int>^>^ LvqModelCli::PrototypePositions() {
		msclr::lock l(backupSync);
		return Tuple::Create(cppToCli( modelCopy->GetProjectedPrototypes()), cppToCli(modelCopy->GetPrototypeLabels()));
	}
}