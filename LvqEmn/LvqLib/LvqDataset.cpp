#include "stdafx.h"
#include "LvqDataSet.h"
#include "utils.h"

LvqDataSet::LvqDataSet(MatrixXd points, vector<int> pointLabels, int classCountPar) 
	: trainPoints(points)
	, trainPointLabels(pointLabels)
	, classCount(classCountPar)
	, decay_lr(1.0/sqrt(double(pointLabels.size())))
	, lr_P(0.001)
	, lr_B(0.001)
	, lr_point(0.1)
	, trainIter(0)
{
	assert(points.cols() == pointLabels.size());
	assert(*std::max_element(pointLabels.begin(),pointLabels.end()) < classCount);
	assert(*std::min_element(pointLabels.begin(),pointLabels.end()) >= 0);
	
	trainClassFrequency.resize(classCount);
	for(int i=0;i<classCount;i++)
		trainClassFrequency[i]=0;
	for(int i=0;i<(int)trainPointLabels.size();i++)
		trainClassFrequency[trainPointLabels[i]]++;

}

LvqModel LvqDataSet::ConstructModel(vector<int> protodistribution) {
	MatrixXd means( trainPoints.rows(), classCount);
	means.setZero();
	
	for(int i=0;i<(int)trainPointLabels.size();++i) {
		means.col(trainPointLabels[i]) += trainPoints.col(i);
	}
	for(int i=0;i<classCount;i++) {
		if(trainClassFrequency[i] >0)
			means.col(i) /= double(trainClassFrequency[i]);
	}

	return LvqModel(protodistribution, means);
}


void LvqDataSet::TrainModel(int iters, boost::mt19937 & randGen, LvqModel & model) {
	vector<int> ordering;
	for(int iter=0;iter<iters;iter++) {
		makeRandomOrder(randGen, ordering, (int)trainPointLabels.size());
		for(int tI=0;tI<ordering.size();++tI) {
			int pointIndex = ordering[tI];
			
			int pointClass = trainPointLabels[pointIndex];
			
			double overallLR = 1.0/(decay_lr*trainIter + 1.0)/trainClassFrequency[pointClass];

			model.learnFrom(trainPoints.col(pointIndex), pointClass, lr_P*overallLR, lr_B*overallLR, lr_point*overallLR);
			trainIter++;
		}
	}
}

