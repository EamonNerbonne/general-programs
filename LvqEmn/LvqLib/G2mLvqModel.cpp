#include "stdafx.h"
#include "G2mLvqModel.h"
#include "utils.h"
#include "G2mLvqMatch.h"

G2mLvqModel::G2mLvqModel(std::vector<int> protodistribution, MatrixXd const & means) 
	: classCount((int)protodistribution.size())
	, lr_scale_P(0.1)
	, lr_scale_B(0.01)
	, P(2,means.rows())
	, vJ(means.rows())
	, vK(means.rows())
	, dQdwJ(means.rows())
	, dQdwK(means.rows())
	, dQdP(2,means.rows())
{
	using namespace std;

	P.setIdentity();
	protoCount = accumulate(protodistribution.begin(),protodistribution.end(),0);
	prototype.reset(new G2mLvqPrototype[protoCount]);
	int protoIndex=0;
	for(int label=0; label <(int) protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = G2mLvqPrototype(label, protoIndex, means.col(label) );

			protoIndex++;
		}
	}
	assert( accumulate(protodistribution.begin(),protodistribution.end(),0)== protoIndex);
}

int G2mLvqModel::classify(VectorXd const & unknownPoint, VectorXd & tmp) const{
	using namespace std;

	G2mLvqMatch matches(&P, &unknownPoint);
	for(int i=0;i<protoCount;i++)
		matches.AccumulateMatch(prototype[i], tmp);

	assert(matches.match != NULL);
	return matches.match->ClassLabel();
}


void G2mLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel, double learningRate, VectorXd & tmp) {
	using namespace std;

	double lr_point = learningRate,
		lr_P = learningRate * this->lr_scale_P,
		lr_B = learningRate * this->lr_scale_B; 

	assert(lr_P>=0  &&  lr_B>=0  &&  lr_point>=0);


	G2mLvqGoodBadMatch matches(&P, &trainPoint, trainLabel);
	for(int i=0;i<protoCount;i++)
		matches.AccumulateMatch(prototype[i],tmp);

	assert(matches.good !=NULL && matches.bad!=NULL);
	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distanceGood / (sqr( matches.distanceGood) + sqr(matches.distanceBad));
	double mu_K = +2.0*matches.distanceBad / (sqr( matches.distanceGood) + sqr(matches.distanceBad));

	G2mLvqPrototype *J = &prototype[matches.good->protoIndex];
	G2mLvqPrototype *K = &prototype[matches.bad->protoIndex];
	assert(J == matches.good && K == matches.bad);

	//VectorXd
	vJ = (matches.good->point - trainPoint).lazy();
	vK = (matches.bad->point - trainPoint).lazy();

	Vector2d P_vJ = ( P * vJ ).lazy();
	Vector2d P_vK = ( P * vK ).lazy();

	Vector2d muK2_Bj_P_vJ = mu_K * 2.0 * ( (*J->B) * P_vJ ).lazy();
	Vector2d muJ2_Bk_P_vK = mu_J * 2.0 * ( (*K->B) * P_vK ).lazy();

	Vector2d muK2_BjT_Bj_P_vJ =  (J->B->transpose() * muK2_Bj_P_vJ).lazy();
	Vector2d muJ2_BkT_Bk_P_vK = (K->B->transpose() * muJ2_Bk_P_vK).lazy();

	//performance: J->B, J->point, K->B, and K->point, are write only from hereon forward, so we _could_ fold the differential computation info the update statement (less intermediates, but strangely not faster).

	
	//*
	Matrix2d dQdBj = (muK2_Bj_P_vJ * P_vJ.transpose()).lazy();
	Matrix2d dQdBk = (muJ2_Bk_P_vK * P_vK.transpose()).lazy();
	*J->B = *J->B - lr_B * dQdBj ;
	*K->B = *K->B - lr_B * dQdBk ;
	/*/
	*J->B = *J->B - lr_B * (muK2_Bj_P_vJ * P_vJ.transpose()).lazy() ;
	*K->B = *K->B - lr_B * (muJ2_Bk_P_vK * P_vK.transpose()).lazy() ;
	/**/


	//*
	dQdwJ = (P.transpose() *  muK2_BjT_Bj_P_vJ).lazy(); //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK = (P.transpose() * muJ2_BkT_Bk_P_vK).lazy();
	J->point = J->point - lr_point * dQdwJ ;
	K->point = K->point - lr_point * dQdwK ;
	/*/
	J->point -=  lr_point * (P.transpose() *  muK2_BjT_Bj_P_vJ).lazy();
	K->point -=  lr_point * (P.transpose() * muJ2_BkT_Bk_P_vK).lazy();
	/**/


	//*
	dQdP = (muK2_BjT_Bj_P_vJ * vJ.transpose()).lazy() + (muJ2_BkT_Bk_P_vK * vK.transpose()).lazy(); //differential wrt. global projection matrix.
	P = P - lr_P * dQdP ;
	/*/
	P = P - lr_P * ( (muK2_BjT_Bj_P_vJ * vJ.transpose()).lazy() + (muJ2_BkT_Bk_P_vK * vK.transpose()).lazy()) ;
	/**/
}
