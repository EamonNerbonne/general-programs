#include "stdafx.h"
#include "LvqModel.h"
#include "utils.h"
#include "LvqDataset.h"

using namespace std;
using namespace Eigen;

static double correctScaleFactorForDecay(double decay) {
	return (3./4.) * (decay*2.0 + 2.) / (decay*2.0 + 1.);// * sqrt ((decay*2.0 + 3.5) / (decay*2.0 + 0.3)) * sqrt(sqrt( (decay*2.0 + 2.5) / (decay*2.0 + 0.15)));
}

LvqModel::LvqModel(LvqModelSettings & initSettings)
	: 
	trainIter(0)
	, totalIter(0)
	, totalElapsed(0.0)
	, totalLR(0.0)
	, settings(initSettings.RuntimeSettings)
	, epochsTrained(0)
{
	int protoCount = accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0);
	iterationScaleFactor = initSettings.iterScaleFactor * correctScaleFactorForDecay(initSettings.decay)  / (initSettings.LrPp  ? 1.0  :  sqrt((double)protoCount));
	iterationScalePower = - (2.0 * initSettings.decay + 1) / (2.0 * initSettings.decay + 2.0);

	if(initSettings.LrPp)
		per_proto_trainIter.resize(protoCount,0.0);
}

double LvqModel::RegisterEpochDone(int itersTrained, double elapsed, int epochs) {
	totalIter +=itersTrained;
	totalElapsed += elapsed;
	epochsTrained += epochs;
	return totalIter;
}

void LvqModel::AddTrainingStat(Statistics& statQueue, LvqDataset const * trainingSet, LvqDataset const * testSet) const {
	LvqDatasetStats trainingstats;
	if(trainingSet) 
		trainingstats = trainingSet->ComputeCostAndErrorRate(*this);

	vector<double> stats;
	stats.push_back(double(totalIter));
	stats.push_back(totalElapsed);
	stats.push_back(trainingstats.errorRate());
	stats.push_back(trainingstats.meanCost());
	LvqDatasetStats teststats;
	if(testSet ) 
		teststats = testSet->ComputeCostAndErrorRate(*this);
	stats.push_back(teststats.errorRate());
	stats.push_back(teststats.meanCost());

	stats.push_back(trainingstats.distanceGood().GetMean()[0]);
	stats.push_back(trainingstats.distanceBad().GetMean()[0]);

	stats.push_back(trainingstats.distanceGood().GetVariance()[0]);
	stats.push_back(trainingstats.distanceBad().GetVariance()[0]);

	stats.push_back(totalLR * settings.LR0);

	stats.push_back(trainingstats.muJmean());
	if(!this->IdenticalMu()) stats.push_back(trainingstats.muKmean());
	stats.push_back(trainingstats.muJmax());
	if(!this->IdenticalMu()) stats.push_back(trainingstats.muKmax());

	this->AppendOtherStats(stats, trainingSet,testSet); 

	statQueue.push(std::move(stats));
}

std::vector<std::wstring> LvqModel::TrainingStatNames() const {
	std::vector<std::wstring> retval;
	retval.push_back(L"Training Iterations!iterations");
	retval.push_back(L"Elapsed Seconds!seconds");
	retval.push_back(L"Training Error!error rate!Error Rates");
	retval.push_back(L"Training Cost!cost function!Cost Function");
	retval.push_back(L"Test Error!error rate!Error Rates");
	retval.push_back(L"Test Cost!cost function!Cost Function");
	retval.push_back(L"Nearest Correct Prototype Distance!distance!Prototype Distance");
	retval.push_back(L"Nearest Incorrect Prototype Distance!distance!Prototype Distance");
	retval.push_back(L"Nearest Correct Prototype Distance Variance!distance variance!$Prototype Distance Variance");
	retval.push_back(L"Nearest Incorrect Prototype Distance Variance!distance variance!$Prototype Distance Variance");

	retval.push_back(L"Cumulative Learning Rate!!$Cumulative Learning Rates");

	//retval.push_back(L"Cumulative \u03BC-scaled Learning Rate!!Cumulative Learning Rates");
	if(this->IdenticalMu()) {
		retval.push_back(L"mean \u03BC!mean \u03BC!mean \u03BC");//greek:\u03BC math:\U0001D707
		retval.push_back(L"max \u03BC!max \u03BC!$max \u03BC");//greek:\u03BC math:\U0001D707
	} else {
		retval.push_back(L"mean \u03BC J!mean \u03BC!mean \u03BC");
		retval.push_back(L"mean \u03BC K!mean \u03BC!mean \u03BC");
		retval.push_back(L"max \u03BC J!max \u03BC!$max \u03BC");
		retval.push_back(L"max \u03BC K!max \u03BC!$max \u03BC");
	}
	AppendTrainingStatNames(retval); 
	return retval;
}

void LvqModel::AppendTrainingStatNames(std::vector<std::wstring> & ) const { }
void LvqModel::AppendOtherStats(std::vector<double> & , LvqDataset const * , LvqDataset const *  ) const { }
