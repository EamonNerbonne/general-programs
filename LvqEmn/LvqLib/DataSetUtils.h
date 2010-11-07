#pragma once
//#pragma managed(push, off)

#include <boost/random/mersenne_twister.hpp>
#include <Eigen/Core>

class LvqDataset;
class DatasetUtils
{
	DatasetUtils(void) {}
	DatasetUtils(DatasetUtils const &) {}
public:
	static Eigen::MatrixXd MakePointCloud(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int pointCount, double meansep);

	static LvqDataset* ConstructGaussianClouds(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int classCount, int pointsPerClass, double meansep);
	static LvqDataset* ConstructStarDataset(boost::mt19937 & rngParams, boost::mt19937 & rngInst, int dims, int starDims, int numStarTails,  int classCount, int pointsPerClass, double starMeanSep, double starClassRelOffset, bool randomlyRotate);
};
//#pragma managed(pop)
