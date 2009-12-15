#include "StdAfx.h"
#include "LvqDataSetCli.h"
namespace LVQCppCli {


	LvqDataSetCli::LvqDataSetCli(array<double,2>^ points, array<int>^ pointLabels, int classCount)
			: dataset(NULL)
	{
		MatrixXd nPoints = arrayToMatrix(points);

		vector<int> trainingLabels(pointLabels->Length);

		for(int i=0; i<pointLabels->Length; ++i)
			trainingLabels[i] = pointLabels[i];

		dataset = new LvqDataSet(nPoints, trainingLabels, classCount);
	}
}