#include "stdafx.h"
#include "LvqDataSet.h"
#include "utils.h"

LvqDataSet::LvqDataSet(MatrixXd const & points, vector<int> pointLabels, int classCountPar) 
	: trainPoints(points)
	, trainPointLabels(pointLabels)
	, classCount(classCountPar)
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

MatrixXd LvqDataSet::ComputeClassMeans() const {
	MatrixXd means( trainPoints.rows(), classCount);
	means.setZero();
	
	for(int i=0;i<(int)trainPointLabels.size();++i) {
		means.col(trainPointLabels[i]) += trainPoints.col(i);
	}
	for(int i=0;i<classCount;i++) {
		if(trainClassFrequency[i] >0)
			means.col(i) /= double(trainClassFrequency[i]);
	}

	return means;
}

void LvqDataSet::TrainModel(int epochs, boost::mt19937 & randGen, AbstractLvqModel * model) const {
	double lrIterScale = model->iterationScaleFactor();

	boost::scoped_array<int> ordering(new int[trainPointLabels.size()] );
	VectorXd new_point(trainPoints.rows());
	for(int epoch=0; epoch<epochs; ++epoch) {

		makeRandomOrder(randGen, ordering.get(), (int)trainPointLabels.size());
		for(int tI=0; tI<(int)trainPointLabels.size(); ++tI) {
			int pointIndex = ordering[tI];
			int pointClass = trainPointLabels[pointIndex];
			double baseLR = std::pow(model->trainIter*lrIterScale + 1.0, - 0.65); 
			double overallLR = baseLR * 0.3;//  / trainClassFrequency[pointClass] * sqrt(double(trainPointLabels.size()));
			new_point = trainPoints.col(pointIndex);
			model->learnFrom(new_point, pointClass, overallLR);
			model->trainIter++;
		}
	}
}

double LvqDataSet::ErrorRate(AbstractLvqModel const * model)const {
	VectorXd tmp_point(trainPoints.rows());
	int errs=0;
	for(int i=0;i<(int)trainPointLabels.size();++i) 
		if(model->classify(trainPoints.col(i)) != trainPointLabels[i])
			errs++;
	return errs / double(trainPointLabels.size());
}

PMatrix LvqDataSet::ProjectPoints(AbstractProjectionLvqModel const * model) const {
	return model->getProjection() * trainPoints;
}