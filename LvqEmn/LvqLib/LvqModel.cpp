#include "stdafx.h"
#include "LvqModel.h"
#include "utils.h"
#include "LvqMatch.h"

LvqModel::LvqModel(std::vector<int> protodistribution, MatrixXd const & means) 
	: classCount((int)protodistribution.size())
	, P(2,means.rows())
	, tmp(means.rows())
	, vJ(means.rows())
	, vK(means.rows())
	, dQdwJ(means.rows())
	, dQdwK(means.rows())
	, dQdP(2,means.rows())
{
	using namespace std;
	
	P.setIdentity();
	protoCount = accumulate(protodistribution.begin(),protodistribution.end(),0);
	prototype.reset(new LvqPrototype[protoCount]);
	int protoIndex=0;
	//		for (vector<int>::iterator it = protodistribution.begin(); it!=protodistribution.end(); ++it) {
	for(int label=0; label <(int) protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = LvqPrototype(label, protoIndex, means.col(i) );
			prototype[protoIndex].point = means.col(label);

			protoIndex++;
		}
	}
	assert( accumulate(protodistribution.begin(),protodistribution.end(),0)== protoIndex);
}

int LvqModel::classify(VectorXd const & unknownPoint) const{
	using namespace std;
	VectorXd & tmp = const_cast<LvqModel*>(this)->tmp;

	LvqMatch matches(&P, &unknownPoint);
	for(int i=0;i<protoCount;i++)
		matches.AccumulateMatch(prototype[i],tmp);

	assert(matches.match != NULL);
	return matches.match->ClassLabel();
}


void LvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel, double lr_P, double lr_B, double lr_point) {
	using namespace std;
	assert(lr_P>0&& lr_B>0 && lr_point>0);

	LvqGoodBadMatch matches(&P, &trainPoint, trainLabel);
	for(int i=0;i<protoCount;i++)
		matches.AccumulateMatch(prototype[i],tmp);

	assert(matches.good !=NULL && matches.bad!=NULL);
	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distanceGood / (sqr( matches.distanceGood) + sqr(matches.distanceBad));
	double mu_K = +2.0*matches.distanceBad / (sqr( matches.distanceGood) + sqr(matches.distanceBad));

	LvqPrototype *J = &prototype[matches.good->protoIndex];
	LvqPrototype *K = &prototype[matches.bad->protoIndex];
	assert(J == matches.good && K == matches.bad);

	//VectorXd
	vJ = (matches.good->point - trainPoint).lazy();
	vK = (matches.bad->point - trainPoint).lazy();

	//TODO:performance: make assignments lazy, via z = (x+y).lazy();  see http://eigen.tuxfamily.org/dox/TopicLazyEvaluation.html

	Vector2d P_vJ = ( P * vJ ).lazy();
	Vector2d P_vK = ( P * vK ).lazy();

	Vector2d Bj_P_vJ = ( (*J->B) * P_vJ ).lazy();
	Vector2d Bk_P_vK = ( (*K->B) * P_vK ).lazy();

	Vector2d muK2_BjT_Bj_P_vJ = ((mu_K * 2.0) * (J->B->transpose() * Bj_P_vJ).lazy()).lazy();
	Vector2d muJ2_BkT_Bk_P_vK = ((mu_J * 2.0) * (K->B->transpose() * Bk_P_vK).lazy()).lazy();

	//TODO:performance: J->B, J->point, K->B, and K->point, are write only from hereon forward, so we _could_ fold the differential computation info the update statement (less intermediates, faster).
	//VectorXd 
	dQdwJ = (P.transpose().lazy() *  muK2_BjT_Bj_P_vJ).lazy(); //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK = (P.transpose().lazy() * muJ2_BkT_Bk_P_vK).lazy();

	dQdP = ((muK2_BjT_Bj_P_vJ * vJ.transpose()).lazy() + (muJ2_BkT_Bk_P_vK * vK.transpose()).lazy()).lazy(); //differential wrt. global projection matrix.

	Matrix2d dQdBj = (((mu_K * 2.0) * Bj_P_vJ).lazy() * P_vJ.transpose()).lazy();
	Matrix2d dQdBk = (((mu_J * 2.0) * Bk_P_vK).lazy() * P_vK.transpose()).lazy();

	J->point =( J->point - (lr_point * dQdwJ).lazy() ).lazy();
	K->point =( K->point - (lr_point * dQdwJ).lazy() ).lazy();

	*J->B =( *J->B - (lr_B * dQdBj).lazy() ).lazy();
	*K->B =( *K->B - (lr_B * dQdBk).lazy() ).lazy();

	P =( P - (lr_P * dQdP).lazy() ).lazy();
	//TODO:etc.
}
