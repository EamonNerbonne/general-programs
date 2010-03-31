#include "StdAfx.h"
#include "DataSetUtils.h"

USING_PART_OF_NAMESPACE_EIGEN
MatrixXd DataSetUtils::MakePointCloud(boost::mt19937 & rndGen, int dims, int pointCount, double meansep) {
	MatrixXd P(dims, dims);
	VectorXd offset(dims);

	MatrixXd points(dims,pointCount);

	RandomMatrixInit(rndGen, P, 0, 1.0);
	RandomMatrixInit(rndGen, points, 0, 1.0);
	RandomMatrixInit(rndGen, offset, 0, meansep);

	return P * points + offset * VectorXd::Ones(pointCount).transpose();
}

LvqDataSet* DataSetUtils::ConstructDataSet(boost::mt19937 & rndGen, int dims, int pointCount, int classCount, double meansep){

	MatrixXd allpoints(dims, classCount*pointCount);
	for(int classLabel=0;classLabel < classCount; classLabel++) {
		allpoints.block(0, classLabel*pointCount, dims, pointCount) = DataSetUtils::MakePointCloud(rndGen, dims, pointCount, meansep);
	}

	vector<int> trainingLabels(allpoints.cols());
	for(int i=0; i<(int)trainingLabels.size(); ++i) 
		trainingLabels[i] = i/pointCount;

	return new LvqDataSet(allpoints, trainingLabels, classCount); 
}

LvqDataSet* DataSetUtils::ConstructRandomStar(boost::mt19937 & rndGen, int numStarTails, int dims, int pointCount, int classCount, double meansep){
	const int starDim=2;
	vector<MatrixXd> tailTransforms;
	vector<VectorXd> tailMeans;
	for(int i=0;i<numStarTails;i++) {
		MatrixXd t(starDim,starDim);
		VectorXd m(starDim);
		RandomMatrixInit(rndGen,t,0,1.0);
		RandomMatrixInit(rndGen,m,0,meansep);
		tailTransforms.push_back(t);
		tailMeans.push_back(m);
	}
	MatrixXd finalTransform(dims,dims);
	RandomMatrixInit(rndGen,finalTransform,0,1.0);

	for(int label=0;label<classCount;++label) {
//		vector<VectorXd
		for(int i=0;i<numStarTails;i++) {
			MatrixXd t(starDim,starDim);
			VectorXd m(starDim);
			RandomMatrixInit(rndGen,t,0,1.0);
			RandomMatrixInit(rndGen,m,0,meansep);
			tailTransforms.push_back(t);
			tailMeans.push_back(m);
		}

	}

	MatrixXd allpoints(dims, classCount*pointCount);
	for(int classLabel=0;classLabel < classCount; classLabel++) {
		allpoints.block(0, classLabel*pointCount, dims, pointCount) = DataSetUtils::MakePointCloud(rndGen, dims, pointCount, meansep);
	}

	vector<int> trainingLabels(allpoints.cols());
	for(int i=0; i<(int)trainingLabels.size(); ++i) 
		trainingLabels[i] = i/pointCount;

	return new LvqDataSet(allpoints, trainingLabels, classCount); 
}

