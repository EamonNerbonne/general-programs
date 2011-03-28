#include "StdAfx.h"
#include "LvqModelSettings.h"
#include "LvqModel.h"

#include "GgmLvqModel.h"
#include "G2mLvqModel.h"
#include "LgmLvqModel.h"
#include "GmLvqModel.h"

#include "LvqDataset.h"
#include "NeuralGas.h"
LvqModel* ConstructLvqModel(LvqModelSettings & initSettings) {
	switch(initSettings.ModelType) {
	case LvqModelSettings::LgmModelType:
		return new LgmLvqModel(initSettings);
		break;
	case LvqModelSettings::GmModelType:
		return new GmLvqModel(initSettings);
		break;
	case LvqModelSettings::G2mModelType:
		return new G2mLvqModel(initSettings);
		break;
	case LvqModelSettings::GgmModelType:
		return new GgmLvqModel(initSettings);
		break;
	default:
		return 0;
		break;
	}
}


LvqModelRuntimeSettings::LvqModelRuntimeSettings(int classCount, boost::mt19937 & rngIter)
	: TrackProjectionQuality(true)
	, NormalizeProjection(true)
	, NormalizeBoundaries(true)
	, UpdatePointsWithoutB(false)
	, GloballyNormalize(true)
	, SlowStartLrBad(false)
	, ClassCount(classCount)
	, LrScaleP(LVQ_LrScaleP)
	, LrScaleB(LVQ_LrScaleB)
	, LR0(LVQ_LR0)
	, LrScaleBad(LVQ_LrScaleBad)
	, RngIter(rngIter) { }


LvqModelSettings::LvqModelSettings(LvqModelType modelType, boost::mt19937 & rngParams, boost::mt19937 & rngIter, std::vector<int> protodistrib, LvqDataset const * dataset, std::vector<int> trainingset) 
	: RandomInitialProjection(true)
	, RandomInitialBorders(false) 
	, NgUpdateProtos(false)
	, NgInitializeProtos(true)
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
pair<Matrix_NN, VectorXi> LvqModelSettings::InitByClassMeans() const {
	int prototypecount = PrototypeCount();
	Matrix_NN  prototypes(Dataset->dimensions(),prototypecount);
	VectorXi labels(prototypecount);
	Matrix_NN classmeans = Dataset->ComputeClassMeans(Trainingset);
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
pair<Matrix_NN, VectorXi> LvqModelSettings::InitByNg() {
	if(std::all_of(PrototypeDistribution.begin(), PrototypeDistribution.end(), [](int count) {return count==1;}))
		return InitByClassMeans();

	vector<vector<int> > setsByClass(ClassCount());
	for(size_t si=0;si<Trainingset.size();++si) {
		int pointIndex = Trainingset[si];
		int label = Dataset->getPointLabels()[pointIndex];
		setsByClass[label].push_back(pointIndex);
	}

	int prototypecount = PrototypeCount();
	Matrix_NN prototypes(Dimensions(), prototypecount);
	VectorXi labels(prototypecount);

	int pi=0;
	for(size_t i = 0; i < PrototypeDistribution.size(); ++i) {
		NeuralGas ng(RngParams, PrototypeDistribution[i], Dataset, setsByClass[i]);
		ng.do_training(RngParams, Dataset, setsByClass[i]);
		prototypes.block(0, pi, Dimensions(), PrototypeDistribution[i]).noalias() = ng.Prototypes();
		for(int subpi =0; subpi < PrototypeDistribution[i]; ++subpi, ++pi)
			labels(pi) = (int)i;
	}

	return make_pair(prototypes,labels);
}

pair<Matrix_NN, VectorXi> LvqModelSettings::InitProtosBySetting()  {
	return NgInitializeProtos ? InitByNg() : InitByClassMeans();
}

Matrix_P LvqModelSettings::pcaTransform() const {
	return Dataset->ComputePcaProjection(Trainingset);
}
