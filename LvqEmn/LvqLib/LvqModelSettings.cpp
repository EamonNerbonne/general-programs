#include "StdAfx.h"
#include "LvqModelSettings.h"
#include "LvqModel.h"

#include "GgmLvqModel.h"
#include "FgmLvqModel.h"
#include "G2mLvqModel.h"
#include "LgmLvqModel.h"
#include "GmLvqModel.h"
#include "GmFullLvqModel.h"
#include "GpqLvqModel.h"
#include "LpqLvqModel.h"

#include "LvqDataset.h"
#include "NeuralGas.h"
#include "shuffle.h"
#include "CovarianceAndMean.h"
#include "randomProjectionMatrix.h"
#include "randomUnscalingMatrix.h"
#include "PCA.h"

#include <numeric>
#include <iterator>

using std::pair;
using std::tuple;
using boost::mt19937;


LvqModel* ConstructLvqModel(LvqModelSettings & initSettings) {
	switch(initSettings.ModelType) {
	case LvqModelSettings::LgmModelType:
		return new LgmLvqModel(initSettings);
		break;
	case LvqModelSettings::GmModelType:

		return initSettings.Dimensionality==0||initSettings.Dimensionality==LVQ_LOW_DIM_SPACE? (LvqModel*)new GmLvqModel(initSettings)
			: new GmFullLvqModel(initSettings);
		break;
	case LvqModelSettings::G2mModelType:
		return new G2mLvqModel(initSettings);
		break;
	case LvqModelSettings::GgmModelType:
		return new GgmLvqModel(initSettings);
		break;
	case LvqModelSettings::FgmModelType:
		return new FgmLvqModel(initSettings);
		break;
	case LvqModelSettings::GpqModelType:
		return new GpqLvqModel(initSettings);
	case LvqModelSettings::LpqModelType:
		return new LpqLvqModel(initSettings);
	default:
		return 0;
		break;
	}
}


LvqModelRuntimeSettings::LvqModelRuntimeSettings(int classCount, boost::mt19937 & rngIter)
	: NoNnErrorRateTracking(false)
	, neiP(false)
	, scP(false)
	, noKP(false)
	, neiB(false)
	, LocallyNormalize(false)
	, wGMu(false)
	, SlowK(false)
	, ClassCount(classCount)
	, MuOffset(0.0)
	, LrScaleP(LVQ_LrScaleP)
	, LrScaleB(LVQ_LrScaleB)
	, LR0(LVQ_LR0)
	, LrScaleBad(LVQ_LrScaleBad)
	, RngIter(rngIter) { }


LvqModelSettings::LvqModelSettings(LvqModelType modelType, boost::mt19937 & rngParams, boost::mt19937 & rngIter, std::vector<int> protodistrib, LvqDataset const * dataset) 
	: Ppca(false)
	, RandomInitialBorders(false) 
	, NGu(false)
	, NGi(false)
	, Popt(false)
	, Bcov(false)
	, decay(1.0)
	, iterScaleFactor(LVQ_ITERFACTOR_PERPROTO)
	, Dimensionality(0)
	, ModelType(modelType)
	, RngParams(rngParams)
	, RuntimeSettings(static_cast<int>(dataset->classCount()), rngIter)
	, PrototypeDistribution(protodistrib)
	, Dataset(dataset)
{ }

size_t LvqModelSettings::InputDimensions() const { return Dataset->dimCount(); }

int LvqModelSettings::PrototypeCount() const {	return accumulate(PrototypeDistribution.begin(), PrototypeDistribution.end(), 0); }


pair<Matrix_NN, VectorXi> LvqModelSettings::InitByClassMeans() const {
	using std::make_pair;
	int prototypecount = PrototypeCount();
	Matrix_NN  prototypes(Dataset->dimCount(),prototypecount);
	VectorXi labels(prototypecount);
	Matrix_NN classmeans = Dataset->ComputeClassMeans();
	int pi=0;
	for(size_t i = 0; i < PrototypeDistribution.size(); ++i) {
		for(int subpi =0; subpi < PrototypeDistribution[i]; ++subpi, ++pi){
			prototypes.col(pi).noalias() = classmeans.col(i);
			labels(pi) = (int)i;
		}
	}

	return make_pair(prototypes,labels);
}


pair<Matrix_NN, VectorXi> LvqModelSettings::InitByNg() {
	using std::partial_sum;
	using std::back_inserter;
	using std::all_of;
	using std::for_each;
	using std::make_pair;

	if(all_of(PrototypeDistribution.begin(), PrototypeDistribution.end(), [](int count) {return count == 1;}))
		return InitByClassMeans();

	vector<vector<int> > setsByClass(ClassCount());
	for(ptrdiff_t pointIndex=0;pointIndex<Dataset->pointCount();++pointIndex) {
		int label = Dataset->getPointLabels()(pointIndex);
		setsByClass[label].push_back((int)pointIndex);
	}

	int prototypecount = PrototypeCount();
	Matrix_NN prototypes(InputDimensions(), prototypecount);
	VectorXi labels(prototypecount);

	int pi=0;
	for(size_t i = 0; i < PrototypeDistribution.size(); ++i) {
		Matrix_NN classPoints(Dataset->dimCount(),setsByClass[i].size());
		for(size_t si=0;si<setsByClass[i].size();++si)
			classPoints.col(si) = Dataset->getPoints().col(setsByClass[i][si]);
		NeuralGas ng(RngParams, PrototypeDistribution[i], classPoints);
		ng.do_training(RngParams, classPoints);
		prototypes.block(0, pi, InputDimensions(), PrototypeDistribution[i]).noalias() = ng.Prototypes();
		for(int subpi =0; subpi < PrototypeDistribution[i]; ++subpi, ++pi)
			labels(pi) = (int)i;
	}

	return make_pair(prototypes,labels);
}

pair<Matrix_NN, VectorXi> LvqModelSettings::InitProtosBySetting()  {
	return NGi ? InitByNg() : InitByClassMeans();
}

void LvqModelSettings::ProjInit(Matrix_NN const& prototypes, Matrix_NN & P){
	size_t protocount = prototypes.cols();
	vector<int> classOffsets;
	classOffsets.push_back(0);
	partial_sum(PrototypeDistribution.begin(),PrototypeDistribution.end(), back_inserter(classOffsets));
	size_t iter=0;
	const size_t finalIter = 10000;

	vector<int> shuffledset(Dataset->GetTestSubset(0,1));
	Vector_N  dists(protocount), vJ(InputDimensions()), vK(InputDimensions()), point(InputDimensions());
	//auto dims = Dimensions();

	while(iter < finalIter) {
		shuffle(RngParams, shuffledset, (unsigned)shuffledset.size());
		for(size_t tI=0;tI < shuffledset.size() && iter < finalIter; ++tI, ++iter) {
			int classLabel = Dataset->getPointLabels()[shuffledset[tI]];
			double lr = 0.01 * (finalIter/100.0) / (finalIter/100.0 + iter);
			Matrix_P::Index wJi=-1, wKi=-1;
			double bestJd(std::numeric_limits<double>::infinity()), bestKd(std::numeric_limits<double>::infinity());
			point = Dataset->getPoints().col(shuffledset[tI]);
			dists = (P * (prototypes.colwise() - point)).colwise().squaredNorm();

			for(int i=classOffsets[classLabel];i<classOffsets[classLabel+1];i++)
				if(bestJd > dists(i)) {
					wJi = i;
					bestJd = dists(i);
				}
				for(int i=0; i < (int)protocount; i ++) 
					if(i == classOffsets[classLabel])
						i = classOffsets[classLabel+1];
					else if(bestKd > dists(i)) {
						wKi = i;
						bestKd = dists(i);
					}

					vJ = prototypes.col(wJi) - point;
					vK = prototypes.col(wKi) - point;

					P +=  lr * (1.0/sqrt(bestKd)* (P * vK) * vK.transpose() - 1.0/sqrt(bestJd) * (P * vJ) * vJ.transpose());
//					P +=  (-lr ) * P * vJ * vJ.transpose();

					normalizeProjection(P);
		}
	}
}

tuple<Matrix_NN,Matrix_NN, VectorXi> LvqModelSettings::InitProtosAndProjectionBySetting() {
	using std::make_tuple;

	auto P = initTransform();
	auto protos = InitProtosBySetting();
	auto prototypes = protos.first;
	auto labels = protos.second;

	if(Popt) 
		ProjInit(prototypes, P);

	

	return make_tuple(P, prototypes, labels);
}


Matrix_22 normalizingB(Matrix_22 const & cov) {
	Vector_2 eigVal;
	Matrix_22 pca2d;
	PcaLowDim::DoPcaFromCov(cov,pca2d,eigVal);
	return eigVal.array().sqrt().inverse().matrix().asDiagonal()*pca2d;
}

Matrix_22 BinitByPca(Matrix_P const & lowdimpoints) {
	return normalizingB(Covariance::ComputeWithMean(lowdimpoints));
}


vector<Matrix_22> BinitByProtos(Matrix_P const & lowdimpoints, VectorXi const & pointLabels, Matrix_P const & lowdimProtos, VectorXi const & protoLabels) {
	Matrix_22 globalCov = Covariance::ComputeWithMean(lowdimpoints);

	int classCount=protoLabels.maxCoeff() + 1;
	vector<Matrix_P> protosByClass;
	vector<vector<size_t>> protoIdxesByClass;
	for(int label=0;label < classCount; ++label) {
		vector<size_t> lblProtoIdxs;
		for(size_t i=0; i < (size_t)protoLabels.size(); ++i) 
			if (protoLabels(i) == (int)label) 
				lblProtoIdxs.push_back(i);
		Matrix_P lblProtos(LVQ_LOW_DIM_SPACE, lblProtoIdxs.size());
		for(size_t i=0;i< lblProtoIdxs.size(); ++i) 
			lblProtos.col(i) = lowdimProtos.col(lblProtoIdxs[i]);

		protoIdxesByClass.push_back(lblProtoIdxs);
		protosByClass.push_back(lblProtos);
	}

	vector<vector<Vector_2> > pointsNearestToProto(protoLabels.size());

	for(ptrdiff_t pointI = 0; pointI < pointLabels.size(); ++pointI) {
		Matrix_P::Index protoI;
		(protosByClass[ pointLabels[pointI] ].colwise() - lowdimpoints.col(pointI)).colwise().squaredNorm().minCoeff(&protoI);
		pointsNearestToProto[protoIdxesByClass[pointLabels[pointI]][protoI]].push_back(lowdimpoints.col(pointI));
	}

	vector<Matrix_22> invCov(protoLabels.size());

	for(size_t protoI = 0; protoI < (size_t)protoLabels.size(); ++protoI) {
		if( pointsNearestToProto[protoI].size() < 3 ) {
			invCov[protoI] = normalizingB(globalCov);
		} else{
			Matrix_P nearestPoints(LVQ_LOW_DIM_SPACE, pointsNearestToProto[protoI].size());

			for(size_t pointI = 0; pointI < pointsNearestToProto[protoI].size(); ++pointI) 
				nearestPoints.col(pointI) = pointsNearestToProto[protoI][pointI];

			Vector_2 protoPosition = lowdimProtos.col(protoI);
			Matrix_22 cov = Covariance::Compute<Matrix_P>(nearestPoints, protoPosition);

			invCov[protoI] = normalizingB(cov);
		}
	}
	return invCov;
}


vector<Matrix_22> BinitByLastProto(Matrix_P const & lowdimpoints, VectorXi const & pointLabels, Matrix_P const & lowdimProtos, VectorXi const & protoLabels) {
	int classCount=protoLabels.maxCoeff() + 1;
	Matrix_P classMeans(LVQ_LOW_DIM_SPACE, classCount);
	for(int i = 0; i < protoLabels.size(); ++i) 
		classMeans.col(protoLabels(i)) = lowdimProtos.col(i);
	VectorXi protoSubLabels(classCount);
	for(int i = 0; i < classCount; ++i) 
		protoSubLabels(i) = i;

	vector<Matrix_22> initClassB = BinitByProtos(lowdimpoints,  pointLabels, classMeans, protoSubLabels);
	vector<Matrix_22> initB;
	for(int i = 0; i < protoLabels.size(); ++i) 
		initB.push_back(initClassB[protoLabels(i)]);
	return initB;
}

vector<Matrix_22> BinitPerProto(Matrix_P const & P, LvqModelSettings & initSettings,	Matrix_NN const & prototypes,	VectorXi const & protoLabels) {
	Matrix_P const lowdimpoints = P * initSettings.Dataset->getPoints();

	vector<Matrix_22> initB;
	if(!initSettings.Bcov) {
		if(initSettings.ModelType == LvqModelSettings::GgmModelType || initSettings.ModelType == LvqModelSettings::FgmModelType) {
			auto globalB = BinitByPca(lowdimpoints);
			for(size_t protoIndex=0; protoIndex < (size_t)protoLabels.size(); ++protoIndex)
				initB.push_back(globalB);
		} else { //G2mModelType
			for(size_t protoIndex=0; protoIndex < (size_t)protoLabels.size(); ++protoIndex)
				initB.push_back(Matrix_22::Identity());
		}
	} else if(initSettings.NGi) {
		initB = BinitByProtos(lowdimpoints,  initSettings.Dataset->getPointLabels()  , P * prototypes, protoLabels);
	} else {
		initB = BinitByLastProto(lowdimpoints,  initSettings.Dataset->getPointLabels(), P * prototypes, protoLabels);
	}

	if(initSettings.RandomInitialBorders) {
		for(size_t protoIndex=0; protoIndex < (size_t)protoLabels.size(); ++protoIndex){
			Matrix_22 rndMat;
			if(initSettings.ModelType == LvqModelSettings::GgmModelType || initSettings.ModelType == LvqModelSettings::FgmModelType) 
				rndMat = randomUnscalingMatrix<Matrix_22>(initSettings.RngParams, LVQ_LOW_DIM_SPACE);
			else  //G2mModelType
				projectionRandomizeUniformScaled(initSettings.RngParams, rndMat);
			initB[protoIndex] =rndMat * initB[protoIndex];
		}
	}

	return initB;
}


tuple<Matrix_P,Matrix_NN, VectorXi, vector<Matrix_22> > LvqModelSettings::InitProtosProjectionBoundariesBySetting() {
	using std::get;
	using std::make_tuple;


	auto protosAndProjection = InitProtosAndProjectionBySetting();
	auto boundaries = BinitPerProto(get<0>(protosAndProjection),*this,get<1>(protosAndProjection),get<2>(protosAndProjection));

	return make_tuple(get<0>(protosAndProjection), get<1>(protosAndProjection), get<2>(protosAndProjection), boundaries);
}


Matrix_NN LvqModelSettings::pcaTransform() const {
	return Dataset->ComputePcaProjection(Dimensionality==0?LVQ_LOW_DIM_SPACE:Dimensionality);
}

Matrix_NN LvqModelSettings::initTransform() {
	Matrix_NN P(Dimensionality == 0 ? LVQ_LOW_DIM_SPACE : Dimensionality, InputDimensions());
	if(Ppca)
		P = pcaTransform();
	else
		randomProjectionMatrix(RngParams, P);
	normalizeProjection(P);
	return P;
}
