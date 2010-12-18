#include "stdafx.h"

#include "DatasetUtils.h"
#include "LvqDataset.h"
#include "RandomMatrix.h"

using namespace std;

//Generates a gaussian cloud
//center is normally distributed with center~N(0,
MatrixXd DatasetUtils::MakePointCloud(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int pointCount, double meansep,double detScalePower) {

	VectorXd offset(dims);
	RandomMatrixInit(rngParams, offset, 0, meansep/sqrt(static_cast<double>(dims)));

	MatrixXd P = randomScalingMatrix<MatrixXd>(rngParams, dims,1.0);
	MatrixXd points(dims,pointCount);
	RandomMatrixInit(rngInst, points, 0, 1.0);
	
	return P * points + offset * VectorXd::Ones(pointCount).transpose();
}

LvqDataset* DatasetUtils::ConstructGaussianClouds(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int classCount, int pointsPerClass, double meansep){

	MatrixXd allpoints(dims, classCount*pointsPerClass);
	for(int classLabel=0;classLabel < classCount; classLabel++) {
		allpoints.block(0, classLabel*pointsPerClass, dims, pointsPerClass) = DatasetUtils::MakePointCloud(rngParams,rngInst, dims, pointsPerClass, meansep,1.0);
	}

	vector<int> trainingLabels(allpoints.cols());
	for(int i=0; i<(int)trainingLabels.size(); ++i) 
		trainingLabels[i] = i/pointsPerClass;

	return new LvqDataset(allpoints, trainingLabels, classCount); 
}

MatrixXd MakeTailMeans(boost::mt19937 & rndGen, int numStarTails, int starDim, double meansep) {
	MatrixXd tailMeans(starDim,numStarTails);
	RandomMatrixInit(rndGen,tailMeans,0,meansep);
	return tailMeans;
}

vector<MatrixXd> MakeTailTransforms(boost::mt19937 & rndGen, int numStarTails, int starDim) {
	vector<MatrixXd> tailTransforms;
	for(int i=0;i<numStarTails;i++) {
		//MatrixXd t(starDim,starDim);
		//RandomMatrixInit(rndGen,t,0,1.0);
		//normalizeProjection(t);
		//tailTransforms.push_back(t);
		tailTransforms.push_back(randomScalingMatrix<MatrixXd>(rndGen, starDim,0.5));
	}
	return tailTransforms;
}

typedef boost::uniform_int<> starChoiceDistrib;
typedef boost::variate_generator<boost::mt19937 &, starChoiceDistrib> starChoiceGen;

LvqDataset* DatasetUtils::ConstructStarDataset(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int starDims, int numStarTails, int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyRotate){
	MatrixXd postInitTransform = randomOrthogonalMatrix<MatrixXd>(rngParams,dims);//always compute random transform, even if not needed, to ensure identical usage of random
	if(!randomlyRotate) postInitTransform.setIdentity();

	vector<MatrixXd> tailTransforms = MakeTailTransforms(rngParams, numStarTails, starDims);
	MatrixXd tailMeans = MakeTailMeans(rngParams, numStarTails, starDims, starMeanSep);
	
	starChoiceGen starRndChoose(rngInst, starChoiceDistrib(0, numStarTails -1));

	VectorXd starRaw(starDims),fullPoint(dims);
	MatrixXd points(dims, pointsPerClass * classCount);
	vector<int> pointLabels(points.cols());
	int pointIndex=0;

	for(int label=0;label<classCount;++label) {
		MatrixXd currentTailMeans = tailMeans + MakeTailMeans(rngParams, numStarTails, starDims, starMeanSep * starClassRelOffset);
		for(int i=0;i<pointsPerClass;++i) {
			int starIdx = starRndChoose();
			RandomMatrixInit(rngInst, starRaw, 0, 1.0);
			
			fullPoint.block(0,0,starDims,1)	= currentTailMeans.col(starIdx) + tailTransforms[starIdx] * starRaw;

			Eigen::Block<VectorXd> restBlock(fullPoint.block(starDims, 0, dims - starDims, 1));
			RandomMatrixInit(rngInst, restBlock,0,1.0);
			points.block(0, pointIndex, dims, 1) = postInitTransform* fullPoint;
			pointLabels[pointIndex] = label;
			pointIndex++;
		}
	}
	assert(pointIndex == pointsPerClass * classCount);
	return new LvqDataset(points, pointLabels, classCount); 
}

