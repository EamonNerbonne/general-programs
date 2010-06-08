#include "stdafx.h"
#include "LvqDataset.h"
#include "utils.h"
#include <xmmintrin.h>
using namespace std;
LvqDataset::LvqDataset(MatrixXd const & points, vector<int> pointLabels, int classCountPar) 
	: points(points)
	, pointLabels(pointLabels)
	, classCount(classCountPar)
{
	assert(points.cols() == pointLabels.size());
	assert(*std::max_element(pointLabels.begin(),pointLabels.end()) < classCount);
	assert(*std::min_element(pointLabels.begin(),pointLabels.end()) >= 0);
	
	//pointLabels.shrink_to_fit();
}

MatrixXd LvqDataset::ComputeClassMeans() const {
	MatrixXd means( points.rows(), classCount);
	means.setZero();
	boost::scoped_array<int> freq(new int[classCount]);
	for(int i=0;i<classCount;++i) freq[i]=0;

	for(int i=0;i<(int)pointLabels.size();++i) {
		means.col(pointLabels[i]) += points.col(i);
		freq[pointLabels[i]]++;
	}
	for(int i=0;i<classCount;i++) {
		if(freq[i] >0)
			means.col(i) /= double(freq[i]);
	}
	return means;
}


size_t LvqDataset::MemAllocEstimate() const {
	return sizeof(LvqDataset) + sizeof(int) * pointLabels.size()  + sizeof(double)*points.size();
}

void EIGEN_STRONG_INLINE prefetch(void const * start,int lines) {
	for(int i=0;i<lines;i++)
		_mm_prefetch( (const char*)start + 64*i, _MM_HINT_NTA);//_MM_HINT_T0
}

void LvqDataset::TrainModel(int epochs, boost::mt19937 & randGen, AbstractLvqModel * model) const {
	BenchTimer t;
	t.start();
	int dims = static_cast<int>(points.rows());
	boost::scoped_array<int> ordering(new int[pointLabels.size()+1] );
	ordering[pointLabels.size()] = 0;
	VectorXd pointA(dims);
	int cacheLines = (dims*sizeof(points(0,0) ) +63)/ 64 ;

	for(int epoch=0; epoch<epochs; ++epoch) {
		makeRandomOrder(randGen, ordering.get(), (int)pointLabels.size());
		pointA=points.col(ordering[0]);
		for(int tI=0; tI<(int)pointLabels.size(); ++tI) {
		//	_mm_prefetch((char*)model,_MM_HINT_T0);
			int pointIndex = ordering[tI];
			int pointClass = pointLabels[pointIndex];
			pointA = points.col(pointIndex);
			prefetch( &points.coeff (0, ordering[tI+1]) ,cacheLines);
			model->learnFrom(pointA, pointClass);
		}
	}
	t.stop();
	model->AddTrainingStat(this, 0, (int)(epochs*pointLabels.size()), t.getCpuTime());
}

double LvqDataset::ErrorRate(AbstractLvqModel const * model)const {
	int errs=0;
	for(int i=0;i<(int)pointLabels.size();++i) 
		if(model->classify(points.col(i)) != pointLabels[i])
			errs++;
	return errs / double(pointLabels.size());
}

double LvqDataset::CostFunction(AbstractLvqModel const * model)const {
	VectorXd a;
	double totalCost=0;
	for(int i=0;i<(int)pointLabels.size();++i) 
		totalCost+=model->costFunction(points.col(i), pointLabels[i]);
	return totalCost / double(pointLabels.size());
}

PMatrix LvqDataset::ProjectPoints(AbstractProjectionLvqModel const * model) const {
	return model->projectionMatrix() * points;
}