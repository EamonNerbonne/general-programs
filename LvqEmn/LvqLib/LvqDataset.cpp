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
	assert(points.cols() == int(pointLabels.size()));
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


void LvqDataset::shufflePoints(boost::mt19937& rng) {
	vector<int> shufLabels(getPointCount());
	MatrixXd shufPoints(points.rows(),points.cols());
		
	using boost::scoped_array;
	scoped_array<int> idxs(new int[getPointCount()]);
	makeRandomOrder(rng,idxs.get(),static_cast<int>(points.cols()));
	
	for(int colI=0;colI<points.cols();++colI) {
		shufPoints.col(idxs[colI]) = points.col(colI);
		shufLabels[idxs[colI]] = pointLabels[colI];
	}
	points = shufPoints;
	pointLabels = shufLabels;
}



void LvqDataset::TrainModel(int epochs, AbstractLvqModel * model, std::vector<int> const  & trainingSubset, LvqDataset const * testData, std::vector<int> const  & testSubset) const {
	int dims = static_cast<int>(points.rows());
	VectorXd pointA(dims);
	int cacheLines = (dims*sizeof(points(0,0) ) +63)/ 64 ;

	for(int epoch=0; epoch<epochs; ++epoch) {
		double pointCostSum=0;
		int errs=0;
		vector<int> shuffledOrder(trainingSubset);
		shuffle(model->RngIter(), shuffledOrder.begin(), shuffledOrder.end());
		BenchTimer t;
		t.start();
		for(int tI=0; tI<(int)shuffledOrder.size(); ++tI) {
			bool wasErr=false;
			double pointCost=0;
			int pointIndex = shuffledOrder[tI];
			int pointClass = pointLabels[pointIndex];
			pointA = points.col(pointIndex);
			prefetch( &points.coeff (0, shuffledOrder[(tI+1)%shuffledOrder.size()]), cacheLines);
			model->learnFrom(pointA, pointClass,&wasErr,&pointCost);
			errs+=wasErr?1:0;
			pointCostSum+=pointCost;
		}
		t.stop();
		model->AddTrainingStatFast(pointCostSum/double(shuffledOrder.size()),errs/double(shuffledOrder.size()), testData,testSubset, (int)(1*shuffledOrder.size()), t.value(CPU_TIMER));
	}
}

void LvqDataset::ComputeCostAndErrorRate(std::vector<int> const & subset, AbstractLvqModel const * model,double &meanCost,double & errorRate) const{
	VectorXd a;
	double totalCost=0;
	int errs=0;
	for(int i=0;i<(int)subset.size();++i) {
		bool isErr;
		double pointCost;
		model->computeCostAndError(points.col(subset[i]), pointLabels[subset[i]],isErr,pointCost);
		totalCost+=pointCost;
		errs+=isErr?1:0;
	}
	meanCost = totalCost / double(subset.size());
	errorRate = errs/double(subset.size());
}

PMatrix LvqDataset::ProjectPoints(AbstractProjectionLvqModel const * model) const {
	return model->projectionMatrix() * points;
}