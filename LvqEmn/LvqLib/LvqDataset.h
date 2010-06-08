#pragma once
#include "stdafx.h"
using namespace Eigen;

#include "AbstractLvqModel.h"


class LvqDataset
{
	MatrixXd points; //one dimension per row, one point per column
	std::vector<int> pointLabels;
	const int classCount;
public:
	MatrixXd const & getPoints()const {return points;}
	std::vector<int> const & getPointLabels()const {return pointLabels;}
	int getClassCount()const {return classCount;}
	int getPointCount()const {return static_cast<int>(pointLabels.size());}

	LvqDataset(MatrixXd const & points, std::vector<int> pointLabels, int classCount);

	MatrixXd ComputeClassMeans() const;

	void TrainModel(int epochs, boost::mt19937 & randGen, AbstractLvqModel * model) const;

	double ErrorRate(AbstractLvqModel const * model) const;
	double CostFunction(AbstractLvqModel const * model) const;

	PMatrix ProjectPoints(AbstractProjectionLvqModel const * model) const;
	size_t MemAllocEstimate() const;
	int dimensions() const { return static_cast<int>(points.rows());}
};
