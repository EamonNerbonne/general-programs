#pragma once
//#pragma managed(push, off)
using namespace Eigen;
#include "LvqTypedefs.h"

class LvqModel;
class LvqProjectionModel;

struct LvqDatasetStats {
	double meanCost, errorRate, distGoodMean, distGoodVar, distBadMean, distBadVar,muJmean,muKmean;
	LvqDatasetStats():meanCost(0.0), errorRate(0.0), distGoodMean(0.0), distGoodVar(0.0), distBadMean(0.0), distBadVar(0.0)
	{}
};

class LvqDataset
{
	MatrixXd points; //one dimension per row, one point per column
	std::vector<int> pointLabels;
	int classCount;
	LvqDataset(LvqDataset const & src, std::vector<int> const & subset);
public:
	MatrixXd const & getPoints()const {return points;}
	std::vector<int> const & getPointLabels()const {return pointLabels;}
	int getClassCount()const {return classCount;}
	int getPointCount()const {return static_cast<int>(pointLabels.size());}
	void shufflePoints(boost::mt19937& rng);
	void ExtendByCorrelations();

	LvqDataset* Extract(std::vector<int> const & subset) const;
	MatrixXd ExtractPoints(std::vector<int> const & subset) const;
	std::vector<int> ExtractLabels(std::vector<int> const & subset) const;

	LvqDataset(MatrixXd const & points, std::vector<int> pointLabels, int classCount);
	MatrixXd ComputeClassMeans(std::vector<int> const & subset) const;

	int NearestNeighborClassify(std::vector<int> const & subset, Eigen::VectorXd point) const;
	int NearestNeighborClassify(std::vector<int> const & subset, PMatrix projection, Eigen::Vector2d & point) const;

	double NearestNeighborErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet, PMatrix projection) const;
	double NearestNeighborPcaErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const;
	double NearestNeighborErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const;

	void TrainModel(int epochs, LvqModel * model, std::vector<int> const & trainingSubset, LvqDataset const * testData, std::vector<int> const & testSubset) const;

	LvqDatasetStats ComputeCostAndErrorRate(std::vector<int> const & subset, LvqModel const * model) const;

	PMatrix ProjectPoints(LvqProjectionModel const * model) const;
	size_t MemAllocEstimate() const;
	int dimensions() const { return static_cast<int>(points.rows());}

	std::vector<int> GetTrainingSubset(int fold, int foldcount) const;
	std::vector<int> GetTestSubset(int fold, int foldcount) const;
};
//#pragma managed(pop)