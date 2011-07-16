#include "StdAfx.h"
#include "LvqModelSettings.h"
#include "LvqModel.h"

#include "GgmLvqModel.h"
#include "G2mLvqModel.h"
#include "LgmLvqModel.h"
#include "GmLvqModel.h"

#include "LvqDataset.h"
#include "NeuralGas.h"
#include "shuffle.h"

#include <numeric>
#include <iterator>
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
	, GloballyNormalize(true)
	, UpdatePointsWithoutB(false)
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
	, NgInitializeProtos(false)
	, ProjOptimalInit(false)
	, Dimensionality(0)
	, ModelType(modelType)
	, RngParams(rngParams)
	, RuntimeSettings(static_cast<int>(dataset->getClassCount()), rngIter)
	, PrototypeDistribution(protodistrib)
	, Dataset(dataset)
	, Trainingset(trainingset)
{ }

size_t LvqModelSettings::Dimensions() const { return Dataset->dimensions(); }

int LvqModelSettings::PrototypeCount() const {	return accumulate(PrototypeDistribution.begin(), PrototypeDistribution.end(), 0); }

using std::pair;
using std::tuple;
using std::make_tuple;
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
	using std::partial_sum;
	using std::back_inserter;
	using std::all_of;
	using std::for_each;

	if(all_of(PrototypeDistribution.begin(), PrototypeDistribution.end(), [](int count) {return count == 1;}))
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

tuple<Matrix_P,Matrix_NN, VectorXi> LvqModelSettings::InitProtosAndProjectionBySetting() {
	auto protos = InitProtosBySetting();
	auto prototypes = protos.first;
	auto labels = protos.second;
	auto P = initTransform();
	size_t protocount = prototypes.cols();

	if(ProjOptimalInit) {
		vector<int> classOffsets;
		classOffsets.push_back(0);
		partial_sum(PrototypeDistribution.begin(),PrototypeDistribution.end(), back_inserter(classOffsets));
		size_t iter=0;
		const size_t finalIter = 10000;

		vector<int> shuffledset(Trainingset);
		Vector_N  dists, vJ, vK, point;
		//auto dims = Dimensions();

		while(iter < finalIter) {
			shuffle(RngParams, shuffledset, shuffledset.size());
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

				P +=  lr * (1.0/sqrt(bestKd)* P * vK * vK.transpose() - 1.0/sqrt(bestJd)*P * vJ * vJ.transpose());
				normalizeProjection(P);
			}
		}
	}

	return make_tuple(P, prototypes, labels);
}

Matrix_P LvqModelSettings::pcaTransform() const {
	return Dataset->ComputePcaProjection(Trainingset);
}




inline void randomProjectionMatrix(boost::mt19937 & rngParams, Matrix_P & mat) {
	RandomMatrixInit(rngParams,mat,0.0,1.0);
	Eigen::JacobiSVD<Matrix_P> svd(mat, Eigen::ComputeThinU | Eigen::ComputeThinV);
	if(mat.rows()>mat.cols())
		mat.noalias() = svd.matrixU();
	else
		mat.noalias() = svd.matrixV().transpose();
#ifndef NDEBUG
	for(int r=0;r<mat.rows();r++){
		for(int r0=0;r0<mat.rows();r0++){
			double dotprod = mat.row(r).dot(mat.row(r0));
			if(r==r0)
				assert(fabs(dotprod-1.0) <= std::numeric_limits<LvqFloat>::epsilon()*mat.cols());
			else 
				assert(fabs(dotprod) <= std::numeric_limits<LvqFloat>::epsilon()*mat.cols());
		}
	}
#endif
}

Matrix_P LvqModelSettings::initTransform() {
	Matrix_P P(LVQ_LOW_DIM_SPACE, Dimensions());
	if(!RandomInitialProjection)
		P = pcaTransform();
	else
		randomProjectionMatrix(RngParams, P);
	normalizeProjection(P);
	return P;
}
