#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

#include "AbstractLvqModel.h"
using namespace std;

class LvqDataSet
{
public:
	MatrixXd trainPoints; //one dimension per row, one point per column
	vector<int> trainPointLabels;
	vector<int> trainClassFrequency;
	int classCount;

	LvqDataSet(MatrixXd const & points, vector<int> pointLabels, int classCount);

	MatrixXd ComputeClassMeans() const;

	void TrainModel(int iters, boost::mt19937 & randGen, AbstractLvqModel * model) const;

	double ErrorRate(AbstractLvqModel const * model) const;
	PMatrix ProjectPoints(AbstractProjectionLvqModel const * model) const;
};
