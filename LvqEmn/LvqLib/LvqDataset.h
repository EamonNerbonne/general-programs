#pragma once
#include "stdafx.h"
USING_PART_OF_NAMESPACE_EIGEN

#include <xmmintrin.h>
#include "AbstractLvqModel.h"
using namespace std;

//	void TrainModel(int iters, boost::mt19937 & randGen, AbstractLvqModel * model) const;
void EIGEN_STRONG_INLINE prefetch(void const * start,int lines) {
	for(int i=0;i<lines;i++)
		_mm_prefetch( (const char*)start + 64*i, _MM_HINT_NTA);//_MM_HINT_T0
}
	
class LvqDataSet
{
public:
	MatrixXd trainPoints; //one dimension per row, one point per column
	vector<int> trainPointLabels;
	vector<int> trainClassFrequency;
	int classCount;

	LvqDataSet(MatrixXd const & points, vector<int> pointLabels, int classCount);

	MatrixXd ComputeClassMeans() const;

	template <typename T>
	void TrainModel(int epochs, boost::mt19937 & randGen, T * model) const {
	int dims = trainPoints.rows();
	boost::scoped_array<int> ordering(new int[trainPointLabels.size()+1] );
	ordering[trainPointLabels.size()] = 0;
	typename T::TVectorIn  pointA(dims);
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




	double ErrorRate(AbstractLvqModel const * model) const;
	PMatrix ProjectPoints(AbstractProjectionLvqModel const * model) const;
	size_t MemAllocEstimate() const;
};
