#include "stdafx.h"

#include "LvqWrapper.h"
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"

namespace LVQCppCli {
	
	template<typename T>
	Matrix<T,Eigen::Dynamic,Eigen::Dynamic> arrayToMatrix(array<T,2>^ points) {
		Matrix<T,Eigen::Dynamic,Eigen::Dynamic> nPoints(points->GetLength(1), points->GetLength(0));
		for(int i=0; i<points->GetLength(0); ++i)
			for(int j=0; j<points->GetLength(1); ++j)
				nPoints(j,i) = points[i, j];

		return nPoints;
	}

	template<typename T, int rowsDEF, int colsDEF>
	array<T,2>^ matrixToArray(Matrix<T,rowsDEF,colsDEF>  const & matrix) {
		array<T,2>^ points = gcnew array<T,2>(matrix.cols(),matrix.rows());
		for(int i=0; i<points->GetLength(0); ++i)
			for(int j=0; j<points->GetLength(1); ++j)
				points[i, j] = matrix(j,i);

		return points;
	}

	template<typename T, int rowsDEF, int colsDEF>
	array<T,2>^ matrixToArrayNOFLIP(Matrix<T,rowsDEF,colsDEF>  const & matrix) {
		array<T,2>^ points = gcnew array<T,2>(matrix.rows(),matrix.cols());
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

		model = new G2mLvqModel(protoDistrib, dataset->ComputeClassMeans()); 
	}

	double LvqWrapper::ErrorRate() { return dataset->ErrorRate(model); }

	array<double,2>^ LvqWrapper::CurrentProjection() {
		return matrixToArray(dataset->ProjectPoints(model));
	}

	array<int,2>^ LvqWrapper::ClassBoundaries(double x0, double x1, double y0, double y1,int xCols, int yRows) {
		MatrixXi classDiagram(yRows,xCols);
		model->ClassBoundaryDiagram(x0,x1,y0,y1,classDiagram);
		return matrixToArrayNOFLIP(classDiagram);
	}

}