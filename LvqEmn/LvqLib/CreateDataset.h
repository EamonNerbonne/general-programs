#pragma once
//#pragma managed(push, off)

#include <boost/random/mersenne_twister.hpp>
#include "LvqTypedefs.h"

class LvqDataset;
class CreateDataset
{
	CreateDataset(void) {}
	CreateDataset(CreateDataset const &) {}
public:
	static Matrix_NN MakePointCloud(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int pointCount, double meansep,double detScalePower);

	static LvqDataset* ConstructGaussianClouds(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int classCount, int pointsPerClass, double meansep);
	static LvqDataset* ConstructStarDataset(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int starDims, int numStarTails, int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyRotate, double noiseSigma);
};
//#pragma managed(pop)
