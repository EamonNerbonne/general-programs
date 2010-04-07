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

LvqDataSet* DataSetUtils::ConstructGaussianClouds(boost::mt19937 & rndGen, int dims, int classCount, int pointsPerClass, double meansep){

	MatrixXd allpoints(dims, classCount*pointsPerClass);
	for(int classLabel=0;classLabel < classCount; classLabel++) {
		allpoints.block(0, classLabel*pointsPerClass, dims, pointsPerClass) = DataSetUtils::MakePointCloud(rndGen, dims, pointsPerClass, meansep);
	}

	vector<int> trainingLabels(allpoints.cols());
	for(int i=0; i<(int)trainingLabels.size(); ++i) 
		trainingLabels[i] = i/pointsPerClass;

	return new LvqDataSet(allpoints, trainingLabels, classCount); 
}

MatrixXd MakeTailMeans(boost::mt19937 & rndGen, int numStarTails, int starDim, double meansep) {
	MatrixXd tailMeans(starDim,numStarTails);
	DataSetUtils::RandomMatrixInit(rndGen,tailMeans,0,meansep);
	return tailMeans;
}
vector<MatrixXd> MakeTailTransforms(boost::mt19937 & rndGen, int numStarTails, int starDim) {
	vector<MatrixXd> tailTransforms;
	for(int i=0;i<numStarTails;i++) {
		MatrixXd t(starDim,starDim);
		DataSetUtils::RandomMatrixInit(rndGen,t,0,1.0);
		normalizeMatrix(t);
		tailTransforms.push_back(t);
	}
	return tailTransforms;
}

typedef boost::uniform_int<> starChoiceDistrib;
typedef boost::variate_generator<boost::mt19937 &, starChoiceDistrib> starChoiceGen;

LvqDataSet* DataSetUtils::ConstructStarDataset(boost::mt19937 & rndGen, int dims, int starDims, int numStarTails, int classCount,  int pointsPerClass,  double starMeanSep, double starClassRelOffset){
	vector<MatrixXd> tailTransforms = MakeTailTransforms(rndGen, numStarTails, starDims);
	MatrixXd tailMeans = MakeTailMeans(rndGen, numStarTails, starDims, starMeanSep);
	
	starChoiceGen starRndChoose(rndGen, starChoiceDistrib(0, numStarTails -1));

	VectorXd starRaw(starDims);
	MatrixXd points(dims, pointsPerClass * classCount);
	vector<int> pointLabels(points.cols());
	int pointIndex=0;

	for(int label=0;label<classCount;++label) {
		MatrixXd currentTailMeans = tailMeans + MakeTailMeans(rndGen, numStarTails, starDims, starMeanSep * starClassRelOffset);
		for(int i=0;i<pointsPerClass;++i) {
			int starIdx = starRndChoose();
			RandomMatrixInit(rndGen, starRaw, 0, 1.0);
			points.block(0, pointIndex, starDims, 1) = currentTailMeans.col(starIdx) + tailTransforms[starIdx] * starRaw;
			Eigen::Block<MatrixXd> restBlock(points.block(starDims, pointIndex, dims - starDims, 1));
			RandomMatrixInit(rndGen, restBlock,0,1.0);
			pointLabels[pointIndex] = label;
			pointIndex++;
		}
	}
	assert(pointIndex == pointsPerClass * classCount);
	return new LvqDataSet(points, pointLabels, classCount); 
}

