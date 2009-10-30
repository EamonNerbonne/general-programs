#include "StdAfx.h"
#include "GsmLvqModel.h"
#include "utils.h"

GsmLvqModel::GsmLvqModel(std::vector<int> protodistribution, MatrixXd const & means) 
	: classCount((int)protodistribution.size())
	, lr_scale_P(0.1)
	, tmpHelper(means.rows())
	, P(2,means.rows())
	, vJ(means.rows())
	, vK(means.rows())
	, dQdwJ(means.rows())
	, dQdwK(means.rows())
	, dQdP(2,means.rows())
{
	using namespace std;

	P.setIdentity();
	int protoCount = accumulate(protodistribution.begin(), protodistribution.end(), 0);
	pLabel.resize(protoCount);

	prototype.reset(new VectorXd[protoCount]);


	int protoIndex=0;
	for(int label = 0; label <(int) protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = means.col(label);
			pLabel(protoIndex) = label;

			protoIndex++;
		}
	}
	assert( accumulate(protodistribution.begin(),protodistribution.end(),0)== protoIndex);
}

int GsmLvqModel::classify(VectorXd const & unknownPoint) const{
	VectorXd & tmp = const_cast<VectorXd &>(tmpHelper);

	using namespace std;
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<pLabel.size();i++) {
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

	for(int i=0;i<pLabel.size();i++) {
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
	return match;
}

void GsmLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel, double learningRate) {
	using namespace std;

	double lr_point = learningRate,
		lr_P = learningRate * this->lr_scale_P;

	assert(lr_P>0  &&  lr_point>0);

	GoodBadMatch matches = findMatches(trainPoint, trainLabel, tmpHelper);

	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distGood / (sqr( matches.distGood) + sqr(matches.distBad));
	double mu_K = +2.0*matches.distBad / (sqr( matches.distGood) + sqr(matches.distBad));
	
	int J = matches.matchGood;
	int K = matches.matchBad;

	//VectorXd
	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	Vector2d muK2_P_vJ = (mu_K * 2.0 *  P * vJ ).lazy();
	Vector2d muJ2_P_vK = (mu_J * 2.0 *  P * vK ).lazy();

	dQdwJ = (P.transpose() *  muK2_P_vJ).lazy(); //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK = (P.transpose() * muJ2_P_vK).lazy();
	prototype[J] -= lr_point * dQdwJ;
	prototype[K] -= lr_point * dQdwK;

	dQdP = (muK2_P_vJ * vJ.transpose()).lazy() + (muJ2_P_vK * vK.transpose()).lazy(); //differential wrt. global projection matrix.
	P -= lr_P * dQdP ;
}

void GsmLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) {
	int cols = classDiagram.cols();
	int rows = classDiagram.rows();
	for(int xCol=0;  xCol < cols;  xCol++) {
		double x = x0 + (x1-x0) * (xCol+0.5) / cols;
		for(int yRow=0;  yRow < rows;  yRow++) {
			double y = y0+(y1-y0) * (yRow+0.5) / rows;

		}
	}
}
