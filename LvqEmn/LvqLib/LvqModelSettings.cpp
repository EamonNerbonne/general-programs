#include "StdAfx.h"
#include "LvqModelSettings.h"
#include "LvqModel.h"

#include "GmmLvqModel.h"
#include "G2mLvqModel.h"
#include "GmLvqModel.h"
#include "GsmLvqModel.h"

#include "LvqDataset.h"
#include "NeuralGas.h"
LvqModel* ConstructLvqModel(LvqModelSettings & initSettings) {
	switch(initSettings.ModelType) {
	case LvqModelSettings::GmModelType:
		return new GmLvqModel(initSettings);
		break;
	case LvqModelSettings::GsmModelType:
		return new GsmLvqModel(initSettings);
		break;
	case LvqModelSettings::G2mModelType:
		return new G2mLvqModel(initSettings);
		break;
	case LvqModelSettings::GmmModelType:
		return new GmmLvqModel(initSettings);
		break;
	default:
		return 0;
		break;
	}
}

LvqModelSettings::LvqModelSettings(LvqModelType modelType, boost::mt19937 & rngParams, boost::mt19937 & rngIter, std::vector<int> protodistrib, LvqDataset const * dataset, std::vector<int> trainingset) 
	: RandomInitialProjection(true)
	, RandomInitialBorders(false) 
	, NgUpdateProtos(false)
	, RngParams(rngParams)
	, PrototypeDistribution(protodistrib)
	, Dataset(dataset)
	, ModelType(modelType)
	, RuntimeSettings(static_cast<int>(dataset->getClassCount()), rngIter)
	, Dimensionality(0)
	, Trainingset(trainingset)
{ }

size_t LvqModelSettings::Dimensions() const { return Dataset->dimensions(); }

int LvqModelSettings::PrototypeCount() const {	return accumulate(PrototypeDistribution.begin(), PrototypeDistribution.end(), 0); }

using std::pair;
using std::make_pair;
pair<MatrixXd, VectorXi> LvqModelSettings::InitByClassMeans() const {
	int prototypecount = PrototypeCount();
	MatrixXd  prototypes(Dataset->dimensions(),prototypecount);
	VectorXi labels(prototypecount);
	MatrixXd classmeans = Dataset->ComputeClassMeans(Trainingset);
	int pi=0;
	for(size_t i = 0; i < PrototypeDistribution.size(); ++i) {
		for(int subpi =0; subpi < PrototypeDistribution[i]; ++subpi, ++pi){
			prototypes.col(pi).noalias() = classmeans.col(i);
			labels(pi) = (int)i;
		}
	}

	return make_pair(prototypes,labels);
}

using boost::mt19937;
pair<MatrixXd, VectorXi> LvqModelSettings::InitByNg(mt19937 & rng) const{
	if(std::all_of(PrototypeDistribution.begin(), PrototypeDistribution.end(), [](int count) {return count==1;}))
		return InitByClassMeans();

	vector<vector<int> > setsByClass(ClassCount());
	for(size_t si=0;si<Trainingset.size();++si) {
		int pointIndex = Trainingset[si];
		int label = Dataset->getPointLabels()[pointIndex];
		setsByClass[label].push_back(pointIndex);
	}

	int prototypecount = PrototypeCount();
	MatrixXd prototypes(Dimensions(), prototypecount);
	VectorXi labels(prototypecount);

	int pi=0;
	for(size_t i = 0; i < PrototypeDistribution.size(); ++i) {
		NeuralGas ng(rng, PrototypeDistribution[i], Dataset, setsByClass[i]);
		ng.do_training(rng, Dataset, setsByClass[i]);
		prototypes.block(0, pi, Dimensions(), PrototypeDistribution[i]).noalias() = ng.Prototypes();
		for(int subpi =0; subpi < PrototypeDistribution[i]; ++subpi, ++pi)
			labels(pi) = (int)i;
	}

	return make_pair(prototypes,labels);
}


PMatrix LvqModelSettings::pcaTransform() const {
	return Dataset->ComputePcaProjection(Trainingset);
}
