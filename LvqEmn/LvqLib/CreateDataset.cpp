#include "stdafx.h"

#include "CreateDataset.h"
#include "LvqDataset.h"
#include "RandomMatrix.h"

using namespace std;

//Generates a gaussian cloud
//center is normally distributed with center~N(0,
Matrix_NN CreateDataset::MakePointCloud(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int pointCount, double meansep) {

	Vector_N offset(dims);
	RandomMatrixInit(rngParams, offset, 0, meansep/sqrt(static_cast<double>(dims)));

	Matrix_NN P = randomScalingMatrix<Matrix_NN>(rngParams, dims,1.0);
	Matrix_NN points(dims,pointCount);
	RandomMatrixInit(rngInst, points, 0, 1.0);

	return P * points + offset * Vector_N::Ones(pointCount).transpose();
}

LvqDataset* CreateDataset::ConstructGaussianClouds(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int classCount, int pointsPerClass, double meansep){

	Matrix_NN allpoints(dims, classCount*pointsPerClass);
	for(int classLabel=0;classLabel < classCount; classLabel++) {
		allpoints.block(0, classLabel*pointsPerClass, dims, pointsPerClass) = CreateDataset::MakePointCloud(rngParams,rngInst, dims, pointsPerClass,  meansep * classCount);
	}

	vector<int> trainingLabels(allpoints.cols());
	for(int i=0; i<(int)trainingLabels.size(); ++i) 
		trainingLabels[i] = i/pointsPerClass;

	return new LvqDataset(allpoints, trainingLabels, classCount); 
}

Matrix_NN MakeTailMeans(boost::mt19937 & rndGen, int numStarTails, int starDim, double meansep) {
	Matrix_NN tailMeans(starDim,numStarTails);
	RandomMatrixInit(rndGen, tailMeans, 0, numStarTails*meansep/sqrt(static_cast<double>(starDim)));
	return tailMeans;
}

vector<Matrix_NN> MakeTailTransforms(boost::mt19937 & rndGen, int numStarTails, int starDim) {
	vector<Matrix_NN> tailTransforms;
	for(int i=0;i<numStarTails;i++) 
		tailTransforms.push_back(randomScalingMatrix<Matrix_NN>(rndGen, starDim,0.5));
	return tailTransforms;
}

typedef boost::uniform_int<> starChoiceDistrib;
typedef boost::variate_generator<boost::mt19937 &, starChoiceDistrib> starChoiceGen;

LvqDataset* CreateDataset::ConstructStarDataset(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int starDims, int numStarTails, int classCount, int pointsPerClass,
		double starMeanSep, double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma){
	Matrix_NN postInitTransform = randomOrthogonalMatrix<Matrix_NN>(rngParams,dims);//always compute random transform, even if not needed, to ensure identical usage of random generator
	if(!randomlyRotate) postInitTransform.setIdentity();

	vector<Matrix_NN> tailTransforms = MakeTailTransforms(rngParams, numStarTails, starDims);
	Matrix_NN tailMeans = MakeTailMeans(rngParams, numStarTails, starDims, starMeanSep);

	starChoiceGen starRndChoose(rngInst, starChoiceDistrib(0, numStarTails - 1));

	Vector_N starRaw(starDims),fullPoint(dims);
	Matrix_NN points(dims, pointsPerClass * classCount);
	vector<int> pointLabels(points.cols());
	int pointIndex=0;

	for(int label=0;label<classCount;++label) {
		Matrix_NN currentTailMeans = tailMeans + MakeTailMeans(rngParams, numStarTails, starDims, starMeanSep * starClassRelOffset);
		for(int i=0;i<pointsPerClass;++i) {
			int starIdx = starRndChoose();
			RandomMatrixInit(rngInst, starRaw, 0, 1.0);

			fullPoint.block(0,0,starDims,1)	= currentTailMeans.col(starIdx) + tailTransforms[starIdx] * starRaw;

			Eigen::Block<Vector_N> restBlock(fullPoint.block(starDims, 0, dims - starDims, 1));
			RandomMatrixInit(rngInst, restBlock,0,noiseSigma);
			points.block(0, pointIndex, dims, 1) = postInitTransform* fullPoint;
			pointLabels[pointIndex] = label;
			pointIndex++;
		}
	}

	Vector_N perDimSigma(dims);
	UniformRandomizeMatrix(perDimSigma, rngParams, 0.0, globalNoiseMaxSigma);
	if(globalNoiseMaxSigma > 0.0) {
		Matrix_NN globalNoise(dims, pointsPerClass * classCount);
		RandomMatrixInit(rngInst, globalNoise, 0.0, 1.0);
		globalNoise = perDimSigma.asDiagonal() * globalNoise;
		points += globalNoise;
	}


	assert(pointIndex == pointsPerClass * classCount);
	return new LvqDataset(points, pointLabels, classCount); 
}

