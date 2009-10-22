#include "StdAfx.h"
#include "GsmLvqModel.h"
#include "utils.h"

GsmLvqModel::GsmLvqModel(std::vector<int> protodistribution, MatrixXd const & means) 
	: classCount((int)protodistribution.size())
	, lr_scale_P(0.1)
	, P(2,means.rows())
	//, vJ(means.rows())
	//, vK(means.rows())
	//, dQdwJ(means.rows())
	//, dQdwK(means.rows())
	//, dQdP(2,means.rows())
{
	using namespace std;

	P.setIdentity();
	int protoCount = accumulate(protodistribution.begin(), protodistribution.end(), 0);
	pLabel.resize(protoCount);

	prototype.resize(means.rows(), protoCount);

	int protoIndex=0;
	for(int label = 0; label <(int) protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype.col(protoIndex) = means.col(label);
			pLabel(protoIndex) = label;

			protoIndex++;
		}
	}
	assert( accumulate(protodistribution.begin(),protodistribution.end(),0)== protoIndex);
}

int GsmLvqModel::classify(VectorXd const & unknownPoint, VectorXd & tmp) const{
	using namespace std;
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<prototype.cols();i++) {
		double curDist = SqrDistanceTo(i,unknownPoint,tmp);
		if(curDist < distance) {
			match=i;
			distance = curDist;
		}
	}
	assert( match >= 0 );
	return this->pLabel(match);
}


GsmLvqModel::GoodBadMatch GsmLvqModel::findMatches(VectorXd const & trainPoint, int trainLabel, VectorXd & tmp) {
	GoodBadMatch match;

	for(int i=0;i<prototype.cols();i++) {
		double curDist = SqrDistanceTo(i,trainPoint,tmp);
		if(pLabel(i) == trainLabel) {
			if(curDist < match.distGood) {
				match.matchGood = i;
				match.distGood = curDist;
			}
		} else {
			if(curDist < match.distBad) {
				match.matchBad = i;
				match.distBad = curDist;
			}
		}
	}
	
	assert( match.matchBad >= 0 && match.matchGood >=0 );
}

void GsmLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel, double learningRate, VectorXd & tmp) {
	using namespace std;

	double lr_point = learningRate,
		lr_P = learningRate * this->lr_scale_P;

	assert(lr_P>0  &&  lr_point>0);

	GoodBadMatch matches = findMatches(trainPoint, trainLabel, tmp);

	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distGood / (sqr( matches.distGood) + sqr(matches.distBad));
	double mu_K = +2.0*matches.distBad / (sqr( matches.distGood) + sqr(matches.distBad));
	
	int J = matches.matchGood;
	int K = matches.matchBad;

	//VectorXd
	vJ = (prototype.col(J) - trainPoint).lazy();
	vK = (prototype.col(K) - trainPoint).lazy();

	Vector2d muK2_P_vJ = ((mu_K * 2.0) * ( P * vJ ).lazy()).lazy();
	Vector2d muJ2_P_vK = ((mu_J * 2.0) * ( P * vK ).lazy()).lazy();

	//TODO:performance: J->B, J->point, K->B, and K->point, are write only from hereon forward, so we _could_ fold the differential computation info the update statement (less intermediates, faster).
	//VectorXd 
	dQdwJ = (P.transpose().lazy() *  muK2_P_vJ).lazy(); //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK = (P.transpose().lazy() * muJ2_P_vK).lazy();

	dQdP = ((muK2_P_vJ * vJ.transpose()).lazy() + (muJ2_P_vK * vK.transpose()).lazy()).lazy(); //differential wrt. global projection matrix.

	prototype.col(J) = ( prototype.col(J) - (lr_point * dQdwJ).lazy() ).lazy();
	prototype.col(K) = ( prototype.col(K) - (lr_point * dQdwJ).lazy() ).lazy();

	P =( P - (lr_P * dQdP).lazy() ).lazy();
}
