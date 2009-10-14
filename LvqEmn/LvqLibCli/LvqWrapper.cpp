#include "stdafx.h"

#include "LvqWrapper.h"
namespace LVQCppCli {
	LvqWrapper::LvqWrapper(array<double,2>^ points, array<int>^ pointLabels, int classCount,int protosPerClass) {
		MatrixXd nPoints(points->GetLength(0), points->GetLength(1));
		for(int i=0; i<points->GetLength(0); ++i)
			for(int j=0; j<points->GetLength(1); ++j)
				nPoints(i,j) = points[i, j];

		
		vector<int> trainingLabels(pointLabels->Length);

		for(int i=0; i<pointLabels->Length; ++i)
			trainingLabels[i] = pointLabels[i];

		dataset = new LvqDataSet(nPoints, trainingLabels, classCount);
		
		vector<int> protoDistrib;
		for(int i=0;i<classCount;++i)
			protoDistrib.push_back(protosPerClass);

		model = dataset->ConstructModel(protoDistrib);
	}
	double LvqWrapper::Evaluate() { return dataset->Evaluate(*model); }
}