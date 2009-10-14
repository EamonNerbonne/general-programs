#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

#include "LvqModel.h"
using namespace std;

class LvqDataSet
{
	MatrixXd trainPoints; //one dimension per row, one point per column
	vector<int> trainPointLabels;
	vector<int> trainClassFrequency;
	int classCount;
	int trainIter;
public:
	double lr_P, lr_B, lr_point;
	double decay_lr; //final lr then e.g. lr_P(trainIter) = lr_P/(decay_lr*trainIter + 1 )

	LvqDataSet(MatrixXd points, vector<int> pointLabels, int classCount);

	LvqModel ConstructModel(vector<int> protodistribution);

	void TrainModel(int iters, boost::mt19937 & randGen, LvqModel & model); 
};
