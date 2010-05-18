#pragma once
#include "stdafx.h"
using namespace Eigen;

#include "AbstractLvqModel.h"
using namespace std;

class LvqDataSet
{
public:
	MatrixXd trainPoints; //one dimension per row, one point per column
	vector<int> trainPointLabels;
	vector<int> trainClassFrequency; //TODO: rename, these datasets aren't necessarily training sets.
	int classCount;

	LvqDataSet(MatrixXd const & points, vector<int> pointLabels, int classCount);

	MatrixXd ComputeClassMeans() const;

	void TrainModel(int iters, boost::mt19937 & randGen, AbstractLvqModel * model) const;

	double ErrorRate(AbstractLvqModel const * model) const;
	PMatrix ProjectPoints(AbstractProjectionLvqModel const * model) const;
	size_t MemAllocEstimate() const;
	int dimensions() const { return trainPoints.rows();}
};
