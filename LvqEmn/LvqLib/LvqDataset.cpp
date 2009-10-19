#include "stdafx.h"
#include "LvqDataSet.h"
#include "utils.h"

LvqDataSet::LvqDataSet(MatrixXd const & points, vector<int> pointLabels, int classCountPar) 
	: trainPoints(points)
	, trainPointLabels(pointLabels)
	, classCount(classCountPar)
	, decay_lr(1.0/double(pointLabels.size()))
	, lr_P(1.0)
	, lr_B(1.0)
	, lr_point(10.0)
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

LvqModel* LvqDataSet::ConstructModel(vector<int> protodistribution) const {
	MatrixXd means( trainPoints.rows(), classCount);
	means.setZero();
	
	for(int i=0;i<(int)trainPointLabels.size();++i) {
		means.col(trainPointLabels[i]) += trainPoints.col(i);
	}
	for(int i=0;i<classCount;i++) {
		if(trainClassFrequency[i] >0)
			means.col(i) /= double(trainClassFrequency[i]);
	}

	return new LvqModel(protodistribution, means);
}

void LvqDataSet::TrainModel(int iters, boost::mt19937 & randGen, LvqModel & model) {
	vector<int> ordering;
	for(int iter=0;iter<iters;iter++) {
		makeRandomOrder(randGen, ordering, (int)trainPointLabels.size());
		for(int tI=0;tI<(int)ordering.size();++tI) {
			int pointIndex = ordering[tI];
			
			int pointClass = trainPointLabels[pointIndex];
			
			double overallLR = 1.0/(decay_lr*trainIter + 1.0)/trainClassFrequency[pointClass];

			model.learnFrom(trainPoints.col(pointIndex), pointClass, lr_P*overallLR, lr_B*overallLR, lr_point*overallLR);
			trainIter++;
		}
	}
}

double LvqDataSet::ErrorRate(LvqModel const & model)const {
	int errs=0;
	for(int i=0;i<(int)trainPointLabels.size();++i) 
		if(model.classify(trainPoints.col(i)) != trainPointLabels[i])
			errs++;
	return errs / double(trainPointLabels.size());
}

PMatrix LvqDataSet::ProjectPoints(LvqModel const & model) const {
	return model.getP() * trainPoints;
}