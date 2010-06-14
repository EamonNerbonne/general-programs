#include "StdAfx.h"
#include "GmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"

GmLvqModel::GmLvqModel(boost::mt19937 & rngParams, boost::mt19937 & rngIter,  bool randInit, std::vector<int> protodistribution, MatrixXd const & means)
	: AbstractLvqModel(rngIter,(int)protodistribution.size())
	, lr_scale_P(LVQ_LrScaleP)
	, vJ(means.rows())
	, vK(means.rows())
	, tmpHelper1(means.rows())
	, tmpHelper2(means.rows())
{
	using namespace std;

	int protoCount = accumulate(protodistribution.begin(), protodistribution.end(), 0);
	pLabel.resize(protoCount);
	iterationScaleFactor/=protoCount;

	prototype.resize(protoCount);
	P.resize(protoCount);

	int protoIndex=0;
	for(int label = 0; label <(int) protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = means.col(label);
			P[protoIndex].setIdentity(means.rows(), means.rows());
			if(randInit)
				projectionRandomizeUniformScaled(rngParams, P[protoIndex]);

			pLabel(protoIndex) = label;

			protoIndex++;
		}
	}
	assert( accumulate(protodistribution.begin(),protodistribution.end(),0)== protoIndex);
}



void GmLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	//double learningRate = getLearningRate();
	//incLearningIterationCount();
	double learningRate = stepLearningRate();
	using namespace std;

	double lr_point = learningRate,
		lr_P = learningRate * this->lr_scale_P;

	assert(lr_P>=0 && lr_point>=0);

	GoodBadMatch matches = findMatches(trainPoint, trainLabel);

	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0 * matches.distGood / (sqr(matches.distGood) + sqr(matches.distBad));
	double mu_K = +2.0 * matches.distBad / (sqr(matches.distGood) + sqr(matches.distBad));

	int J = matches.matchGood;
	int K = matches.matchBad;

#if EIGEN3
	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	VectorXd & lrX_muK2_Pj_vJ = tmpHelper1;
	VectorXd & lrX_muJ2_Pk_vK = tmpHelper2;

	lrX_muK2_Pj_vJ.noalias() =P[J] * vJ;
	lrX_muK2_Pj_vJ *= lr_point * mu_K * 2.0;
	lrX_muJ2_Pk_vK.noalias() = P[K] * vK;
	lrX_muJ2_Pk_vK *= LVQ_LrScaleBad*lr_point * mu_J * 2.0;

	prototype[J].noalias() -= P[J].transpose() *  lrX_muK2_Pj_vJ;
	prototype[K].noalias() -= P[K].transpose() * lrX_muJ2_Pk_vK;

	lrX_muK2_Pj_vJ *= lr_P / lr_point;
	lrX_muJ2_Pk_vK *= lr_P / lr_point/LVQ_LrScaleBad;
	P[J].noalias() -=  lrX_muK2_Pj_vJ * vJ.transpose() ;
	P[K].noalias() -= lrX_muJ2_Pk_vK * vK.transpose() ;
#else
	//VectorXd
	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	VectorXd & lrX_muK2_Pj_vJ = tmpHelper1;
	VectorXd & lrX_muJ2_Pk_vK = tmpHelper2;

	lrX_muK2_Pj_vJ = ( P[J] * vJ ).lazy();
	lrX_muK2_Pj_vJ *=lr_point* mu_K * 2.0;
	lrX_muJ2_Pk_vK = ( P[K] * vK ).lazy();
	lrX_muJ2_Pk_vK *= LVQ_LrScaleBad*lr_point* mu_J * 2.0;

	prototype[J] -= (  P[J].transpose() *  lrX_muK2_Pj_vJ).lazy();
	prototype[K] -= (  P[K].transpose() * lrX_muJ2_Pk_vK).lazy();

	lrX_muK2_Pj_vJ *= lr_P / lr_point;
	lrX_muJ2_Pk_vK *= lr_P / lr_point/LVQ_LrScaleBad;

	P[J] -= ( lrX_muK2_Pj_vJ * vJ.transpose() ).lazy();
	P[K] -= ( lrX_muJ2_Pk_vK * vK.transpose() ).lazy();
#endif
}

double GmLvqModel::costFunction(VectorXd const & unknownPoint, int pointLabel) const {
	GoodBadMatch matches = findMatches(unknownPoint, pointLabel);
	return (matches.distGood - matches.distBad)/(matches.distGood+matches.distBad);
}


size_t GmLvqModel::MemAllocEstimate() const {
	return 
		sizeof(GmLvqModel) + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		(sizeof(double) * P[0].size() + sizeof(MatrixXd)) * P.size() + //dyn alloc prototype transforms
		sizeof(double) * (vJ.size() + vK.size() + tmpHelper1.size() + tmpHelper2.size()) + //various vector temps
		(sizeof(VectorXd) + sizeof(double)*prototype[0].size()) *prototype.size() +//dyn alloc prototypes
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
}
double GmLvqModel::meanProjectionNorm() const {
	double normsum=0.0;
	for(size_t i=0;i<P.size();++i)
		normsum+=projectionSquareNorm(P[i]);
	return normsum/P.size();
}

VectorXd GmLvqModel::otherStats() const {
	double minNorm=std::numeric_limits<double>::max();
	double maxNorm=0.0;

	for(size_t i=0;i<P.size();++i) {
		double norm = projectionSquareNorm(P[i]);
		if(norm <minNorm) minNorm = norm;
		if(norm > maxNorm) maxNorm = norm;
	}
	VectorXd stats = VectorXd::Zero(LvqTrainingStats::Extra+2);
	stats(LvqTrainingStats::Extra+0) = minNorm;
	stats(LvqTrainingStats::Extra+1) = maxNorm;
	return stats;
}
