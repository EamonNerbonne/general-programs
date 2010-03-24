#include "StdAfx.h"
#include "GmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"

GmLvqModel::GmLvqModel(boost::mt19937 & rng, bool randInit, std::vector<int> protodistribution, MatrixXd const & means) 
	: lr_scale_P(LVQ_LrScaleP)
	, classCount((int)protodistribution.size())
	, vJ(means.rows())
	, vK(means.rows())
	, dQdwJ(means.rows())
	, dQdwK(means.rows())
	, tmpHelper1(means.rows())
	, tmpHelper2(means.rows())
	, dQdPj(means.rows(), means.rows())
	, dQdPk(means.rows(), means.rows())
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
				projectionRandomizeUniformScaled(rng, P[protoIndex]);

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

	GoodBadMatch matches = findMatches(trainPoint, trainLabel, tmpHelper1, tmpHelper2);

	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0 * matches.distGood / (sqr(matches.distGood) + sqr(matches.distBad));
	double mu_K = +2.0 * matches.distBad / (sqr(matches.distGood) + sqr(matches.distBad));

	int J = matches.matchGood;
	int K = matches.matchBad;

#if EIGEN3
	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	VectorXd & muK2_Pj_vJ = tmpHelper1;
	VectorXd & muJ2_Pk_vK = tmpHelper2;

	muK2_Pj_vJ.noalias() = (mu_K * 2.0) *  (P[J] * vJ);
	muJ2_Pk_vK.noalias() = (mu_J * 2.0) *  (P[K] * vK);

	//dQdwJ.noalias() = P[J].transpose() *  muK2_Pj_vJ; //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	//dQdwK.noalias() = P[K].transpose() * muJ2_Pk_vK;
	prototype[J].noalias() -= lr_point * (P[J].transpose() *  muK2_Pj_vJ);
	prototype[K].noalias() -= lr_point *  (P[K].transpose() * muJ2_Pk_vK);

	muK2_Pj_vJ *= lr_P;
	muJ2_Pk_vK *= lr_P;
	P[J].noalias() -=  muK2_Pj_vJ * vJ.transpose() ;
	P[K].noalias() -= muJ2_Pk_vK * vK.transpose() ;
#else
	//VectorXd
	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	VectorXd & muK2_Pj_vJ = tmpHelper1;
	VectorXd & muJ2_Pk_vK = tmpHelper2;

	muK2_Pj_vJ = (mu_K * 2.0 *  P[J] * vJ ).lazy();
	muJ2_Pk_vK = (mu_J * 2.0 *  P[K] * vK ).lazy();

	dQdwJ = (P[J].transpose() *  muK2_Pj_vJ).lazy(); //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK = (P[K].transpose() * muJ2_Pk_vK).lazy();
	prototype[J] -= ( lr_point * dQdwJ).lazy();
	prototype[K] -= ( lr_point * dQdwK).lazy();

	dQdPj = (muK2_Pj_vJ * vJ.transpose()).lazy();//differential wrt. local projection matrix.
	dQdPk =	(muJ2_Pk_vK * vK.transpose()).lazy(); 
	P[J] -= (lr_P * dQdPj ).lazy();
	P[K] -= (lr_P * dQdPk ).lazy();
#endif
}


size_t GmLvqModel::MemAllocEstimate() const {
	return 
		sizeof(GmLvqModel) + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		sizeof(double) * (dQdPj.size() + dQdPk.size()) + //dyn alloc temp transforms
		sizeof(double) * (dQdPj.size() * P.size()) + //dyn alloc prototype transforms
		sizeof(double) * (vJ.size()*6) + //various vector temps
		sizeof(VectorXd) *prototype.size() +//dyn alloc prototype base overhead
		sizeof(double) * (prototype.size() * vJ.size()) + //dyn alloc prototype data
		(16/2) * (6+prototype.size()*2);//estimate for alignment mucking.
}
