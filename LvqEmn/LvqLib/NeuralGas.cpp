#include "StdAfx.h"
#include "NeuralGas.h"
#include "LvqDataset.h"
#include "shuffle.h"
#include "prefetch.h"
using namespace std;


#define NG_LAMBDA_START 10.0
#define NG_LAMBDA_END 0.01
#define NG_LR_START 0.5
#define NG_LR_END 0.5
static double ln_lambda_start = log(NG_LAMBDA_START);
static double ln_lambda_end = log(NG_LAMBDA_END);
static double ln_lr_start = log(NG_LR_START);
static double ln_lr_end = log(NG_LR_END);

double NeuralGas::lr() const { return exp(ln_lr_start + trainIter / (double) finalIter * (ln_lr_end - ln_lr_start)); }

double NeuralGas::lambda() const { return exp(ln_lambda_start + trainIter / (double) finalIter * (ln_lambda_end - ln_lambda_start)); }

double NeuralGas::learnFrom(Vector_N const & point) {
	assert(trainIter < finalIter);
	tmp_deltaFrom = (prototypes.colwise() - point);
	
    for(Index i=0; (size_t)i < tmp_prototypes_ordering.size(); ++i)
		tmp_prototypes_ordering[i] = make_pair(tmp_deltaFrom.col(i).squaredNorm(), i);
    sort(tmp_prototypes_ordering.begin(), tmp_prototypes_ordering.end());

	//now tmp_prototypes_ordering[i].second is the index of the i-th ranked prototype.
	double cost = 0.0;

	double h_lambda = 1.0;
	double h_factor = exp(-1.0/lambda());
	double curr_lr = lr();
	for(Index i=0; (size_t)i < tmp_prototypes_ordering.size(); ++i) {
		Index pi = tmp_prototypes_ordering[i].second;
		prototypes.col(pi).noalias() -= curr_lr * h_lambda * tmp_deltaFrom.col(pi);
		cost += h_lambda * tmp_deltaFrom.col(pi).squaredNorm();

		h_lambda *= h_factor;
	}
	
	size_t oldI = trainIter * trainCosts.size() / finalIter;
	trainIter++;
	size_t newI = trainIter * trainCosts.size() / finalIter;
	if(oldI!=newI) 
		trainCosts[oldI] = make_pair(trainIter - 1, cost);
	return cost;
}

void NeuralGas::do_training(boost::mt19937& rng, Matrix_NN const & dataset){
	assert(dataset.rows() == prototypes.rows());
	assert(dataset.cols() > prototypes.cols());
	Vector_N point(prototypes.rows());

	int cacheLines = ((int)point.rows() * sizeof(point(0)) + 63)/ 64 ;
	vector<int> order(dataset.cols());
	for(size_t i=0;i<order.size();++i) order[i]=i;
	
	while(trainIter < finalIter) {
		shuffle(rng,order,order.size());
		for(size_t tI=0;tI < order.size() && trainIter < finalIter; ++tI) {
			point = dataset.col(order[tI]);
			prefetch( &dataset.coeff (0, order[(tI+1)%order.size()]), cacheLines);
			learnFrom(point);
		}
	}
}

NeuralGas::NeuralGas(boost::mt19937 & rng, unsigned proto_count, Matrix_NN const & dataset, int totalIterCount, size_t statMoments) 
	: trainIter(0)
	, finalIter(totalIterCount)
	, trainCosts(min(statMoments, (size_t)totalIterCount))
	, totalElapsed(0.0)
{
	prototypes.resize(dataset.rows(), proto_count);
	tmp_deltaFrom.resizeLike(prototypes);
	tmp_delta.resize(dataset.rows());
	tmp_prototypes_ordering.resize(proto_count);
	for(size_t i =0; i< proto_count; ++i)
		prototypes.col(i) = dataset.col(rng() % dataset.cols());
}

