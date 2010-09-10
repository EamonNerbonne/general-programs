#include "StdAfx.h"
#include "GsmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
#include "LvqDataset.h"
using namespace std;

GsmLvqModel::GsmLvqModel(boost::mt19937 & rngParams, boost::mt19937 & rngIter, bool randInit, std::vector<int> protodistribution, MatrixXd const & means)
	: LvqProjectionModelBase(rngIter, static_cast<int>(means.rows()),static_cast<int>(protodistribution.size())) 
	, lr_scale_P(LVQ_LrScaleP)
	, vJ(means.rows())
	, vK(means.rows())
{
	if(randInit)
		projectionRandomizeUniformScaled(rngParams, P);
	else
		P.setIdentity();


	int protoCount = accumulate(protodistribution.begin(), protodistribution.end(), 0);
	pLabel.resize(protoCount);
	iterationScaleFactor/=protoCount;

	prototype.resize(protoCount);
	P_prototype.resize(protoCount);

	int protoIndex=0;
	for(int label = 0; label <(int) protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = means.col(label);
			pLabel(protoIndex) = label;
			RecomputeProjection(protoIndex);

			protoIndex++;
		}
	}
	assert( accumulate(protodistribution.begin(),protodistribution.end(),0)== protoIndex);
}

GoodBadMatch GsmLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	//double learningRate = getLearningRate();
	//incLearningIterationCount();
	double learningRate = stepLearningRate();

	using namespace std;

	double lr_point = learningRate,
		lr_P = learningRate * this->lr_scale_P;

	assert(lr_P>=0  &&  lr_point>=0);

#if EIGEN3
	Vector2d P_trainPoint;
	P_trainPoint.noalias() = P * trainPoint;
#else
	Vector2d P_trainPoint = (P * trainPoint).lazy();
#endif
	GoodBadMatch matches = findMatches(P_trainPoint, trainLabel);

	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distGood / (sqr(matches.distGood) + sqr(matches.distBad));
	double mu_K = +2.0*matches.distBad / (sqr(matches.distGood) + sqr(matches.distBad));

	int J = matches.matchGood;
	int K = matches.matchBad;

	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	//VectorXd
#if EIGEN3
	Vector2d muK2_P_vJ = mu_K * 2.0 * (P_prototype[J] - P_trainPoint) ;
	Vector2d muJ2_P_vK = mu_J * 2.0 * (P_prototype[K] - P_trainPoint);

	//differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	prototype[J].noalias() -= P.transpose() * (lr_point * muK2_P_vJ);
	prototype[K].noalias() -= P.transpose() * (LVQ_LrScaleBad*lr_point *muJ2_P_vK);

	//differential wrt. global projection matrix is subtracted...
	P.noalias() -= (lr_P * muK2_P_vJ) * vJ.transpose() + (lr_P * muJ2_P_vK) * vK.transpose();
#else
	Vector2d muK2_P_vJ = (mu_K * 2.0 * (P_prototype[J] - P_trainPoint) ).lazy();
	Vector2d muJ2_P_vK = (mu_J * 2.0 * (P_prototype[K] - P_trainPoint) ).lazy();

	prototype[J] -= ( P.transpose() * (lr_point * muK2_P_vJ) ).lazy();
	prototype[K] -= ( P.transpose() * (LVQ_LrScaleBad*lr_point *muJ2_P_vK) ).lazy();

	P -= ((lr_P * muK2_P_vJ) * vJ.transpose()).lazy() + ((lr_P * muJ2_P_vK) * vK.transpose()).lazy();
#endif

	//double pNormScale =1.0 / projectionNorm();
	//P *= pNormScale;

	for(int i=0;i<pLabel.size();++i)
		RecomputeProjection(i);
	return matches;
}

LvqModel* GsmLvqModel::clone() const { return new GsmLvqModel(*this);	}

MatrixXd GsmLvqModel::GetProjectedPrototypes() const {
	MatrixXd retval(LVQ_LOW_DIM_SPACE, static_cast<int>(prototype.size()));
	for(unsigned i=0;i<prototype.size();++i)
		retval.col(i) = P_prototype[i];
	return retval;
}

vector<int> GsmLvqModel::GetPrototypeLabels() const {
	vector<int> retval(prototype.size());
	for(unsigned i=0;i<prototype.size();++i)
		retval[i] = pLabel[i];
	return retval;
}

size_t GsmLvqModel::MemAllocEstimate() const {
	return 
		sizeof(GsmLvqModel) + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		sizeof(double) * (P.size() ) + //dyn alloc transform + temp transform
		sizeof(double) * (vJ.size()*3) + //various vector temps
		sizeof(VectorXd) *prototype.size() +//dyn alloc prototype base overhead
		sizeof(double) * (prototype.size() * vJ.size()) + //dyn alloc prototype data
		sizeof(Vector2d) * P_prototype.size() + //cache of pretransformed prototypes
		(16/2) * (5+prototype.size()*2);//estimate for alignment mucking.
}

VectorXd GsmLvqModel::otherStats(LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset) const {
	VectorXd stats = VectorXd::Zero(LvqTrainingStats::Extra+1);
	if(trainingSet)
		stats(LvqTrainingStats::Extra) = trainingSet->NearestNeighborErrorRate(trainingSubset,testSet,testSubset,this->P);
	return stats;

	//double minNorm=std::numeric_limits<double>::max();
	//double maxNorm=0.0;
	//double sumNorm=0.0;

	//for(size_t i=0;i<prototype.size();++i) {
	//	double norm = projectionSquareNorm(prototype[i].B);
	//	sumNorm +=norm;
	//	if(norm <minNorm) minNorm = norm;
	//	if(norm > maxNorm) maxNorm = norm;
	//}
	//VectorXd stats = VectorXd::Zero(LvqTrainingStats::Extra+3);
	//stats(LvqTrainingStats::Extra+0) = minNorm;
	//stats(LvqTrainingStats::Extra+1) = sumNorm/prototype.size();
	//stats(LvqTrainingStats::Extra+2) = maxNorm;
	//return stats;
}
