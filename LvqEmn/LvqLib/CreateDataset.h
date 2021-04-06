#pragma once
//#pragma managed(push, off)

#include <random>
#include "LvqTypedefs.h"

class LvqDataset;
class CreateDataset
{
	CreateDataset(void) {}
	CreateDataset(CreateDataset const &) {}
public:
	static Matrix_NN MakePointCloud(std::mt19937 & rngParams, std::mt19937 & rngInst, int dims, int pointCount, double meansep);

	static LvqDataset* ConstructGaussianClouds(std::mt19937 & rngParams, std::mt19937 & rngInst, int dims, int classCount, int pointsPerClass, double meansep);
	static LvqDataset* ConstructStarDataset(std::mt19937 & rngParams, std::mt19937 & rngInst, int dims, int starDims, int numStarTails, int classCount, int pointsPerClass, double starMeanSep,
		double starClassRelOffset, bool randomlyRotate, double noiseSigma, double globalNoiseMaxSigma);
};
//#pragma managed(pop)
