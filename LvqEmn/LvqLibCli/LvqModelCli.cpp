#include "stdafx.h"

#include "LvqModelCli.h"
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"
#include "WrappingUtils.h"

namespace LvqLibCli {

	LvqModelCli::LvqModelCli(LvqDataSetCli^ dataset, int protosPerClass, bool useGsm)
		: dataset(dataset)
		, model(NULL)
		, modelCopy(NULL)
		, rnd(new boost::mt19937(42))
		, mainSync(gcnew Object())
		, backupSync(gcnew Object())
		, nativeAllocEstimate(0)
	{
		int classCount = dataset->GetDataSet()->classCount;
		
		vector<int> protoDistrib;
		for(int i=0;i<classCount;++i)
			protoDistrib.push_back(protosPerClass);

		boost::mt19937 forkedRng(*rnd);//this approach deterministically advances the rnd internal counter to aid in reproducability; it's not stochastically ideal though.

        if(useGsm)
		 	model = new GsmLvqModel(forkedRng, true, protoDistrib, dataset->GetDataSet()->ComputeClassMeans()); 
		else 
			model = new G2mLvqModel(forkedRng, true, protoDistrib, dataset->GetDataSet()->ComputeClassMeans()); 

		BackupModel();
		
		nativeAllocEstimate = model->MemAllocEstimate() *2 +sizeof(boost::mt19937);

		GC::AddMemoryPressure(nativeAllocEstimate);
	}

	LvqModelCli::!LvqModelCli() {GC::RemoveMemoryPressure(nativeAllocEstimate);}

	double LvqModelCli::ErrorRate() { 
		msclr::lock l(backupSync);
		return dataset->GetDataSet()->ErrorRate(modelCopy); 
	}

	array<double,2>^ LvqModelCli::CurrentProjection() { 
		msclr::lock l(backupSync);
		return cppToCli(dataset->GetDataSet()->ProjectPoints(modelCopy)); 
	}

	array<int,2>^ LvqModelCli::ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows) {
		MatrixXi classDiagram(yRows,xCols);
		{
			msclr::lock l(backupSync);
			modelCopy->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		}
		return cppToCli(classDiagram.transpose());
	}

	void LvqModelCli::TrainEpoch(int epochsToDo) {
		msclr::lock l(mainSync);
		dataset->GetDataSet()->TrainModel(epochsToDo,  *rnd, model);
		BackupModel();
	}
	
	Tuple<array<double,2>^, array<int>^>^ LvqModelCli:: PrototypePositions() {
		msclr::lock l(backupSync);
		return Tuple::Create(cppToCli( modelCopy->GetProjectedPrototypes()), cppToCli(modelCopy->GetPrototypeLabels()));
	}
}