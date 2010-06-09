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

MatrixXd LvqDataset::ComputeClassMeans(std::vector<int> const & subset) const {
	MatrixXd means( points.rows(), classCount);
	means.setZero();
	boost::scoped_array<int> freq(new int[classCount]);
	for(int i=0;i<classCount;++i) freq[i]=0;

	for(int i=0;i<(int)subset.size();++i) {
		means.col(pointLabels[subset[i]]) += points.col(subset[i]);
		freq[pointLabels[subset[i]]]++;
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


void LvqDataset::TrainModel(int epochs, AbstractLvqModel * model, std::vector<int> const  & trainingSubset, LvqDataset const * testData, std::vector<int> const  & testSubset) const {
	BenchTimer t;
	t.start();
	int dims = static_cast<int>(points.rows());
	vector<int> shuffledOrder(trainingSubset);
	VectorXd pointA(dims);
	int cacheLines = (dims*sizeof(points(0,0) ) +63)/ 64 ;

	for(int epoch=0; epoch<epochs; ++epoch) {
		shuffle(model->RngIter(), shuffledOrder.begin(), shuffledOrder.end());
		
		for(int tI=0; tI<(int)pointLabels.size(); ++tI) {
		//	_mm_prefetch((char*)model,_MM_HINT_T0);
			int pointIndex = shuffledOrder[tI];
			int pointClass = pointLabels[pointIndex];
			pointA = points.col(pointIndex);
			prefetch( &points.coeff (0, shuffledOrder[(tI+1)%shuffledOrder.size()]), cacheLines);
			model->learnFrom(pointA, pointClass);
		}
	}
	t.stop();
	model->AddTrainingStat(this,trainingSubset, testData,testSubset, (int)(epochs*pointLabels.size()), t.getCpuTime());
}

double LvqDataset::ErrorRate(std::vector<int> const & subset,AbstractLvqModel const * model)const {
	int errs=0;
	for(int i=0;i<(int)subset.size();++i) 
		if(model->classify(points.col(subset[i])) != pointLabels[subset[i]])
			errs++;
	return errs / double(subset.size());
}

double LvqDataset::CostFunction(std::vector<int> const & subset,AbstractLvqModel const * model)const {
	VectorXd a;
	double totalCost=0;
	for(int i=0;i<(int)subset.size();++i) 
		totalCost+=model->costFunction(points.col(subset[i]), pointLabels[subset[i]]);
	return totalCost / double(subset.size());
}

PMatrix LvqDataset::ProjectPoints(AbstractProjectionLvqModel const * model) const {
	return model->projectionMatrix() * points;
}