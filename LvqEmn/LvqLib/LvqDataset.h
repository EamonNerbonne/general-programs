#pragma once
//#pragma managed(push, off)
using namespace Eigen;
#include "LvqTypedefs.h"
#include "SmartSum.h"
#include "LvqModel.h"
class LvqProjectionModel;

class LvqDatasetStats {
	double meanCost_sum, errorRate_sum, muKmean_sum, muJmean_sum,muKmax_val,muJmax_val;
	SmartSum<1> distGood, distBad;
	size_t counter;
public:
	LvqDatasetStats()
		: meanCost_sum(0.0), errorRate_sum(0.0), muKmean_sum(0.0),muJmean_sum(0.0), muKmax_val(0.0), muJmax_val(0.0), counter(0)
	{

	}
	void Add(MatchQuality match) {
		assert(-1<=match.costFunc && match.costFunc<=1);
		counter++;
		errorRate_sum+= match.isErr ?1:0;
		meanCost_sum += match.costFunc;
		distGood.CombineWith(match.distGood,1.0);
		distBad.CombineWith(match.distBad,1.0);
		muKmean_sum+=-match.muK;//we want positives.
		muJmean_sum+=match.muJ;
		muKmax_val = std::max(muKmax_val, -match.muK);
		muJmax_val = std::max(muJmax_val, match.muJ);
	}

	double meanCost() const {return meanCost_sum / counter;}
	double errorRate()  const{return errorRate_sum / counter;}
	double muKmean()  const{return muKmean_sum / counter; }
	double muJmean() const {return muJmean_sum / counter; }
	double muKmax() const {return muKmax_val;}
	double muJmax()  const {return muJmax_val;}
	SmartSum<1> distanceGood() const {return distGood;} 
	SmartSum<1> distanceBad() const {return distBad;}
};

struct LvqDataset
{
private:
	Matrix_NN points; //one dimension per row, one point per column
	std::vector<int> pointLabels;
	int classCount;
	LvqDataset(LvqDataset const & src, std::vector<int> const & subset);
public:
	LvqDataset(Matrix_NN const & points, std::vector<int> pointLabels, int classCount);
	LvqDataset* Extract(std::vector<int> const & subset) const;
	LvqDataset* ExtendUsingModel(int fold,int foldCount, LvqModel const & model) const;


	void shufflePoints(boost::mt19937& rng);
	void ExtendByCorrelations();
	void NormalizeDimensions();



	Matrix_NN const & getPoints()const {return points;}
	std::vector<int> const & getPointLabels()const {return pointLabels;}
	int getClassCount()const {return classCount;}
	int getPointCount()const {return static_cast<int>(pointLabels.size());}


	Matrix_NN ExtractPoints(std::vector<int> const & subset) const;
	std::vector<int> ExtractLabels(std::vector<int> const & subset) const;

	Matrix_NN ComputeClassMeans(std::vector<int> const & subset) const;
	Matrix_P ComputePcaProjection(std::vector<int> const & subset) const;

	int NearestNeighborClassify(std::vector<int> const & subset, Vector_N point) const;
	int NearestNeighborClassify(std::vector<int> const & subset, Matrix_P projection, Vector_2 & point) const;

	double NearestNeighborProjectedErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet, Matrix_P projection) const;
	double NearestNeighborPcaErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const;
	double NearestNeighborErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const;

	void TrainModel(int epochs, LvqModel * model, LvqModel::Statistics * statisticsSink, std::vector<int> const & trainingSubset, LvqDataset const * testData, std::vector<int> const & testSubset, int*labelOrderSink, bool sortedTrain) const;

	LvqDatasetStats ComputeCostAndErrorRate(std::vector<int> const & subset, LvqModel const * model) const;

	Matrix_P ProjectPoints(LvqProjectionModel const * model) const;
	size_t MemAllocEstimate() const;
	int dimensions() const { return static_cast<int>(points.rows());}

	std::vector<int> GetEverythingSubset() const;
	std::vector<int> GetTrainingSubset(int fold, int foldcount) const;
	int GetTrainingSubsetSize(int fold, int foldcount) const;
	std::vector<int> GetTestSubset(int fold, int foldcount) const;
	int GetTestSubsetSize(int fold, int foldcount) const;
};
//#pragma managed(pop)