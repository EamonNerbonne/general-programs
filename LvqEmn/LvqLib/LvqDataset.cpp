#include "stdafx.h"
#include "LvqDataSet.h"
#include "utils.h"
#include <xmmintrin.h>

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
	//trainClassFrequency.shrink_to_fit();
	//trainPointLabels.shrink_to_fit();
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


size_t LvqDataSet::MemAllocEstimate() const {
	return sizeof(LvqDataSet) + sizeof(int) * (trainPointLabels.size() + trainClassFrequency.size()) + sizeof(double)*trainPoints.size();
}

void EIGEN_STRONG_INLINE prefetch(void const * start,int lines) {
	for(int i=0;i<lines;i++)
		_mm_prefetch( (const char*)start + 64*i, _MM_HINT_NTA);//_MM_HINT_T0
}

void LvqDataSet::TrainModel(int epochs, boost::mt19937 & randGen, AbstractLvqModel * model) const {
	int dims = trainPoints.rows();
	boost::scoped_array<int> ordering(new int[trainPointLabels.size()+1] );
	ordering[trainPointLabels.size()] = 0;
	VectorXd pointA(dims);
	VectorXd pointB(dims);
	int cacheLines = (dims*sizeof(trainPoints(0,0) ) +63)/ 64 ;

	for(int epoch=0; epoch<epochs; ++epoch) {
		makeRandomOrder(randGen, ordering.get(), (int)trainPointLabels.size());
		pointA=trainPoints.col(ordering[0]);
		for(int tI=0; tI<(int)trainPointLabels.size(); ++tI) {
		//	_mm_prefetch((char*)model,_MM_HINT_T0);
			int pointIndex = ordering[tI];
			int pointClass = trainPointLabels[pointIndex];
			pointA = trainPoints.col(pointIndex);
			prefetch( &trainPoints.coeff (0, ordering[tI+1]) ,cacheLines);
			model->learnFrom(pointA, pointClass);
		}
	}
}

double LvqDataSet::ErrorRate(AbstractLvqModel const * model)const {
	VectorXd a;
	int errs=0;
	for(int i=0;i<(int)trainPointLabels.size();++i) 
		if(model->classify(a=trainPoints.col(i)) != trainPointLabels[i])
			errs++;
	return errs / double(trainPointLabels.size());
}

PMatrix LvqDataSet::ProjectPoints(AbstractProjectionLvqModel const * model) const {
	return model->projectionMatrix() * trainPoints;
}