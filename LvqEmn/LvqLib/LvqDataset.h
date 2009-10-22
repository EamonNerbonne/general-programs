#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

#include "G2mLvqModel.h"
using namespace std;

class LvqDataSet
{
public:
	MatrixXd trainPoints; //one dimension per row, one point per column
	vector<int> trainPointLabels;
	vector<int> trainClassFrequency;
	int classCount;
	int trainIter;

	LvqDataSet(MatrixXd const & points, vector<int> pointLabels, int classCount);

	G2mLvqModel* ConstructModel(vector<int> protodistribution) const;

	void TrainModel(int iters, boost::mt19937 & randGen, G2mLvqModel & model);

	double ErrorRate(G2mLvqModel const & model) const;
	PMatrix ProjectPoints(G2mLvqModel const & model) const;
};
