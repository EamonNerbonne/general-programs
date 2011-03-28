#pragma once
//#pragma managed(push, off)
using namespace Eigen;
#include "LvqTypedefs.h"

#include "LvqModel.h"
class LvqProjectionModel;

struct LvqDatasetStats {
	double meanCost, errorRate, distGoodMean, distGoodVar, distBadMean, distBadVar,muKmean,muJmean;
	LvqDatasetStats():meanCost(0.0), errorRate(0.0), distGoodMean(0.0), distGoodVar(0.0), distBadMean(0.0), distBadVar(0.0)
	{}
};

class LvqDataset
{
	Matrix_NN points; //one dimension per row, one point per column
	std::vector<int> pointLabels;
	int classCount;
	LvqDataset(LvqDataset const & src, std::vector<int> const & subset);
public:
	Matrix_NN const & getPoints()const {return points;}
	std::vector<int> const & getPointLabels()const {return pointLabels;}
	int getClassCount()const {return classCount;}
	int getPointCount()const {return static_cast<int>(pointLabels.size());}
	void shufflePoints(boost::mt19937& rng);
	void ExtendByCorrelations();

	LvqDataset* Extract(std::vector<int> const & subset) const;
	Matrix_NN ExtractPoints(std::vector<int> const & subset) const;
	std::vector<int> ExtractLabels(std::vector<int> const & subset) const;

	LvqDataset(Matrix_NN const & points, std::vector<int> pointLabels, int classCount);
	Matrix_NN ComputeClassMeans(std::vector<int> const & subset) const;
	Matrix_P ComputePcaProjection(std::vector<int> const & subset) const;

	int NearestNeighborClassify(std::vector<int> const & subset, Vector_N point) const;
	int NearestNeighborClassify(std::vector<int> const & subset, Matrix_P projection, Vector_2 & point) const;

	double NearestNeighborProjectedErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet, Matrix_P projection) const;
	double NearestNeighborPcaErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const;
	double NearestNeighborErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const;

	size_t AppropriateAnimationEpochs(std::vector<int> const  & trainingSubset) const;
	void TrainModel(int epochs, LvqModel * model, LvqModel::Statistics * statisticsSink, std::vector<int> const & trainingSubset, LvqDataset const * testData, std::vector<int> const & testSubset) const;

	LvqDatasetStats ComputeCostAndErrorRate(std::vector<int> const & subset, LvqModel const * model) const;

	Matrix_P ProjectPoints(LvqProjectionModel const * model) const;
	size_t MemAllocEstimate() const;
	int dimensions() const { return static_cast<int>(points.rows());}

	std::vector<int> GetTrainingSubset(int fold, int foldcount) const;
	std::vector<int> GetTestSubset(int fold, int foldcount) const;
};
//#pragma managed(pop)