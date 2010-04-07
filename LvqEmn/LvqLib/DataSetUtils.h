#pragma once
#include "stdafx.h"
#include "LvqDataSet.h"

class DataSetUtils
{
	DataSetUtils(void) {}
	DataSetUtils(DataSetUtils const &) {}
public:
	static Eigen::MatrixXd MakePointCloud(boost::mt19937 & rndGen, int dims, int pointCount, double meansep);

	template<typename T>static  void RandomMatrixInit(boost::mt19937 & rng, MatrixBase< T>& mat, double mean, double sigma) {
		using namespace boost;
		normal_distribution<> distrib(mean,sigma);
		variate_generator<mt19937&, normal_distribution<> > rndGen(rng, distrib);

		for(int j=0; j<mat.cols(); j++)
			for(int i=0; i<mat.rows(); i++)
				mat(i,j) = rndGen();
	}

	static LvqDataSet* ConstructGaussianClouds(boost::mt19937 & rndGen, int dims, int classCount, int pointsPerClass, double meansep);
	static LvqDataSet* ConstructStarDataset(boost::mt19937 & rndGen, int dims, int starDims, int numStarTails,  int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset);
};