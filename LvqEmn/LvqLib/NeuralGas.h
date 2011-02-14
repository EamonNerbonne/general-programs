#pragma once
#include <Eigen/Core>
#include <vector>

#define NG_LAMBDA_START 10.0
#define NG_LAMBDA_END 0.01
#define NG_LR_START 0.5
#define NG_LR_END 0.5

using std::vector;

class NeuralGas
{
	int trainIter, finalIter;
	int statMoments;
	double totalElapsed;

	vector<VectorXd> prototype;


public:
	NeuralGas(void);
	~NeuralGas(void);

	double unscaledLearningRate() const { 
		double scaledIter = trainIter*iterationScaleFactor+1.0;
		return 1.0 / sqrt(scaledIter*sqrt(scaledIter)); 
	}


};

