#pragma once
#include "stdafx.h"
using namespace Eigen;

#include "LvqModel.h"
#include "LvqProjectionModel.h"

//TODO: make explicit subset classes for training/test stuff.

class LvqDataset
{
	MatrixXd points; //one dimension per row, one point per column
	std::vector<int> pointLabels;
	const int classCount;
	LvqDataset(LvqDataset const & src, std::vector<int> const & subset);
public:
	MatrixXd const & getPoints()const {return points;}
	std::vector<int> const & getPointLabels()const {return pointLabels;}
	int getClassCount()const {return classCount;}
	int getPointCount()const {return static_cast<int>(pointLabels.size());}
	void shufflePoints(boost::mt19937& rng);

	LvqDataset* Extract(std::vector<int> const & subset) const;
	MatrixXd ExtractPoints(std::vector<int> const & subset) const;
	std::vector<int> ExtractLabels(std::vector<int> const & subset) const;

	LvqDataset(MatrixXd const & points, std::vector<int> pointLabels, int classCount);
	MatrixXd ComputeClassMeans(std::vector<int> const & subset) const;

	int NearestNeighborClassify(std::vector<int> const & subset, Eigen::VectorXd point) const;
	int NearestNeighborClassify(std::vector<int> const & subset, PMatrix projection, Eigen::Vector2d point) const;

	double NearestNeighborErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet, PMatrix projection) const;
	double NearestNeighborPcaErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const;
	double NearestNeighborErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const;

	void TrainModel(int epochs, LvqModel * model, std::vector<int> const  & trainingSubset, LvqDataset const * testData, std::vector<int> const  & testSubset) const;

	void ComputeCostAndErrorRate(std::vector<int> const & subset, LvqModel const * model,double &cost,double &errorRate) const;

	PMatrix ProjectPoints(LvqProjectionModel const * model) const;
	size_t MemAllocEstimate() const;
	int dimensions() const { return static_cast<int>(points.rows());}

	std::vector<int> GetTrainingSubset(int fold, int foldcount) const;
	std::vector<int> GetTestSubset(int fold, int foldcount) const;
};
