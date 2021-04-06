#pragma once
#include "LvqTypedefs.h"
#include <vector>
#include <random>

class LvqDataset;

#ifdef NDEBUG
#define NG_DEFAULT_ITERS 100000
#else
#define NG_DEFAULT_ITERS 100000
#endif

class NeuralGas {

	int trainIter, finalIter;
	std::vector<std::pair<int, double> > trainCosts;
	double totalElapsed;

	Matrix_NN prototypes;
	
	typedef Vector_N::Index Index;
	Matrix_NN tmp_deltaFrom;
	Vector_N tmp_delta;
	std::vector<std::pair<double, Index> > tmp_prototypes_ordering;

public:
	NeuralGas(std::mt19937& rng, unsigned proto_count, Matrix_NN const & dataset, int totalIterCount=NG_DEFAULT_ITERS, size_t statMoments=2000);

	double lr() const;
	double lambda() const;

	double learnFrom(Vector_N const & point);
	void do_training(std::mt19937& rng, Matrix_NN const & dataset);
	std::vector<std::pair<int, double> > const & trainCosts_tracked() {return trainCosts;}

	Matrix_NN const & Prototypes() const { return prototypes; }
};

