#include "stdafx.h"

#include "LvqWrapper.h"
namespace LVQCppCli {
	MatrixXd arrayToMatrix(array<double,2>^ points) {
		MatrixXd nPoints(points->GetLength(0), points->GetLength(1));
		for(int i=0; i<points->GetLength(0); ++i)
			for(int j=0; j<points->GetLength(1); ++j)
				nPoints(i,j) = points[i, j];

		return nPoints;
	}

	array<double,2>^ matrixToArray(MatrixXd  const & matrix) {
		array<double,2>^ points = gcnew array<double,2>(matrix.rows(),matrix.cols());
		for(int i=0; i<points->GetLength(0); ++i)
			for(int j=0; j<points->GetLength(1); ++j)
				points[i, j] = matrix(i,j);

		return points;
	}


	LvqWrapper::LvqWrapper(array<double,2>^ points, array<int>^ pointLabels, int classCount,int protosPerClass)
		: dataset(NULL)
		, model(NULL)
		, rnd(new boost::mt19937(42))
	{
		MatrixXd nPoints = arrayToMatrix(points);

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

	array<double,2>^ LvqWrapper::CurrentProjection() {
		return matrixToArray(dataset->ProjectPoints(*model));
	}
}