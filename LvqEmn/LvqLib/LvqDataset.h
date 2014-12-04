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

class LvqDataset
{
	Matrix_NN points; //one dimension per row, one point per column
	VectorXi pointLabels;
	int m_classCount;
	LvqDataset(LvqDataset const & src, std::vector<int> const & subset);
public:

	LvqDataset(Matrix_NN const & points, VectorXi const & pointLabels, int classCount);
	LvqDataset* Extract(std::vector<int> const & subset) const;
	std::pair<LvqDataset*,LvqDataset*> ExtendUsingModel(LvqDataset const * testdataset,LvqDataset const * extendDataset, LvqDataset const * extendTestdataset, LvqModel const & model) const;

	LvqDataset ExtendWithOther(LvqDataset const & otherdataset) const;
	LvqDataset CreateLogDistDataset(LvqModel const & model) const;
	LvqDataset CreateInvSqrtDistDataset(LvqModel const & model) const;
	LvqDataset CreateQrProjectedDataset(LvqModel const & model) const;


	void shufflePoints(boost::mt19937& rng);
	void ExtendByCorrelations();
	std::pair<Vector_N,Vector_N> NormalizationParameters() const;
	void ApplyNormalization(std::pair<Vector_N,Vector_N> pars, bool normalizeByScaling);

	Matrix_NN const & getPoints() const {return points;}
	VectorXi const & getPointLabels()const {return pointLabels;}
	int classCount() const {return m_classCount;}
	ptrdiff_t pointCount()const {return pointLabels.size();}
	ptrdiff_t dimCount() const { return points.rows();}


	Matrix_NN ExtractPoints(std::vector<int> const & subset) const;
	std::vector<int> ExtractLabels(std::vector<int> const & subset) const;

	Matrix_NN ComputeClassMeans() const;
	Matrix_P ComputePcaProjection() const;
	Matrix_NN ComputePcaProjection(int dims) const;

	int NearestNeighborClassify(Vector_N point) const;
	int NearestNeighborClassify(Matrix_P projection, Vector_2 & point) const;

	double NearestNeighborProjectedErrorRate(LvqDataset const & testData, Matrix_P projection) const;
	double NearestNeighborPcaErrorRate(LvqDataset const & testData) const;
	double NearestNeighborErrorRate(LvqDataset const & testData) const;

	void TrainModel(int epochs, LvqModel & model, LvqModel::Statistics * statisticsSink, LvqDataset const* testData, int*labelOrderSink, bool sortedTrain) const;

	LvqDatasetStats ComputeCostAndErrorRate(LvqModel const & model) const;

	Matrix_P ProjectPoints(LvqProjectionModel const & model) const;
	size_t MemAllocEstimate() const;

	
	std::vector<int> GetTrainingSubset(int fold, int foldcount) const;
	std::vector<int> InRandomOrder(boost::mt19937& rng ) const;
	std::vector<int> GetTestSubset(int fold, int foldcount) const;
};
//#pragma managed(pop)