#include "stdafx.h"
#include "LvqModel.h"
#include "utils.h"
#include "LvqMatch.h"

LvqModel::LvqModel(std::vector<int> protodistribution, MatrixXd const & means) 
	: classCount((int)protodistribution.size())
	, P(2,means.rows())
{
	using namespace std;
	P.setIdentity();
	using namespace std;
	protoCount = sum(0,protodistribution);
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
	assert(sum(0, protodistribution) == protoIndex);
}

int LvqModel::classify(VectorXd const & unknownPoint) const{
	using namespace std;

	Vector2d projectedPoint = P * unknownPoint;

	LvqMatch bestMatch= accumulate(prototype.get(), prototype.get() +protoCount, LvqMatch(&P, unknownPoint), LvqMatch::AccumulateHelper);
	assert(bestMatch.match != NULL);
	return bestMatch.match->ClassLabel();
}


void LvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel, double lr_P, double lr_B, double lr_point) {
	using namespace std;
	assert(lr_P>0&& lr_B>0 && lr_point>0);

	Vector2d projectedPoint = P * trainPoint;

	LvqGoodBadMatch matches = accumulate(prototype.get(), prototype.get() +protoCount, LvqGoodBadMatch(&P, trainPoint, trainLabel), LvqGoodBadMatch::AccumulateHelper);

	assert(matches.good !=NULL && matches.bad!=NULL);
	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distanceGood / (sqr( matches.distanceGood) + sqr(matches.distanceBad));
	double mu_K = +2.0*matches.distanceBad / (sqr( matches.distanceGood) + sqr(matches.distanceBad));

	LvqPrototype * J = & prototype[ matches.good->protoIndex];
	LvqPrototype * K = & prototype[ matches.bad->protoIndex];
	assert(J == matches.good && K == matches.bad);

	VectorXd vJ = matches.good->point - trainPoint;
	VectorXd vK = matches.bad->point - trainPoint;

	//TODO:performance: make assignments lazy, via z = (x+y).lazy();  see http://eigen.tuxfamily.org/dox/TopicLazyEvaluation.html

	Vector2d P_vJ = P * vJ;
	Vector2d P_vK = P * vK;

	Vector2d Bj_P_vJ = (*J->B) * P_vJ;
	Vector2d Bk_P_vK = (*K->B) * P_vK;

	Vector2d muK2_BjT_Bj_P_vJ = mu_K * 2.0 * J->B->transpose() * Bj_P_vJ;
	Vector2d muJ2_BkT_Bk_P_vK = mu_J * 2.0 * K->B->transpose() * Bk_P_vK;

	//TODO:performance: J->B, J->point, K->B, and K->point, are write only from hereon forward, so we _could_ fold the differential computation info the update statement (less intermediates, faster).
#if 0
	VectorXd dQdwJ = P.transpose() *  muK2_BjT_Bj_P_vJ; //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	VectorXd dQdwK = P.transpose() * muJ2_BkT_Bk_P_vK; //this line causes errors with vectorization.

	PMatrix dQdP = muK2_BjT_Bj_P_vJ * vJ.transpose() + muJ2_BkT_Bk_P_vK * vK.transpose(); //differential wrt. global projection matrix.

	Matrix2d dQdBj = (mu_K * 2.0 * Bj_P_vJ) * P_vJ.transpose();
	Matrix2d dQdBk = (mu_J * 2.0 * Bk_P_vK) * P_vK.transpose();
	J->point -= lr_point * dQdwJ;
	K->point -= lr_point * dQdwJ;

	*J->B -= lr_B * dQdBj;
	*K->B -= lr_B * dQdBk;

	P -= lr_P * dQdP;
#endif
	//TODO:etc.
}
