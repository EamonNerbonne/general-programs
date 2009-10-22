#include "stdafx.h"
#include "LvqDataSet.h"
#include "utils.h"

LvqDataSet::LvqDataSet(MatrixXd const & points, vector<int> pointLabels, int classCountPar) 
	: trainPoints(points)
	, trainPointLabels(pointLabels)
	, classCount(classCountPar)
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
	boost::scoped_array<int> ordering(new int[trainPointLabels.size()] );
	VectorXd new_point(trainPoints.rows()), tmp_point(trainPoints.rows());
	for(int iter=0; iter<iters; iter++) {

		makeRandomOrder(randGen, ordering.get(), (int)trainPointLabels.size());
		for(int tI=0; tI<(int)trainPointLabels.size(); ++tI) {
			int pointIndex = ordering[tI];
			int pointClass = trainPointLabels[pointIndex];
			double baseLR = std::pow(trainIter/double(trainPointLabels.size()) + 1.0, - 0.65); 
			double overallLR = baseLR  / trainClassFrequency[pointClass] * sqrt(double(trainPointLabels.size()));
			new_point = trainPoints.col(pointIndex);
			model.learnFrom(new_point, pointClass, overallLR, tmp_point);
			trainIter++;
		}
	}
}

double LvqDataSet::ErrorRate(LvqModel const & model)const {
	VectorXd tmp_point(trainPoints.rows());
	int errs=0;
	for(int i=0;i<(int)trainPointLabels.size();++i) 
		if(model.classify(trainPoints.col(i), tmp_point) != trainPointLabels[i])
			errs++;
	return errs / double(trainPointLabels.size());
}

PMatrix LvqDataSet::ProjectPoints(LvqModel const & model) const {
	return model.getP() * trainPoints;
}