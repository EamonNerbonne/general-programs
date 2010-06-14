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
	void shufflePoints(boost::mt19937& rng);

	std::vector<int> entireSet() const {std::vector<int> idxs((size_t)getPointCount()); for(int i=0;i<getPointCount();i++) idxs[i]=i; return idxs; }


	LvqDataset(MatrixXd const & points, std::vector<int> pointLabels, int classCount);
	MatrixXd ComputeClassMeans(std::vector<int> const & subset) const;

	void TrainModel(int epochs, AbstractLvqModel * model, std::vector<int> const  & trainingSubset, LvqDataset const * testData, std::vector<int> const  & testSubset) const;

	double ErrorRate(std::vector<int> const & subset, AbstractLvqModel const * model) const;
	double CostFunction(std::vector<int> const & subset, AbstractLvqModel const * model) const;

	PMatrix ProjectPoints(AbstractProjectionLvqModel const * model) const;
	size_t MemAllocEstimate() const;
	int dimensions() const { return static_cast<int>(points.rows());}
};
