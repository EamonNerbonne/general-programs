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

