#include "StdAfx.h"
#include "GsmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"

GsmLvqModel::GsmLvqModel(boost::mt19937 & rng,  bool randInit, std::vector<int> protodistribution, MatrixXd const & means) 
	: AbstractProjectionLvqModel(means.rows()) 
	, lr_scale_P(LVQ_LrScaleP)
	, classCount((int)protodistribution.size())
	, vJ(means.rows())
	, vK(means.rows())
	, dQdwJ(means.rows())
	, dQdwK(means.rows())
	, tmpHelper(means.rows())
	, dQdP(LVQ_LOW_DIM_SPACE, means.rows())
{
	using namespace std;

	if(randInit)
		projectionRandomizeUniformScaled(rng, P);
	else
		P.setIdentity();


	int protoCount = accumulate(protodistribution.begin(), protodistribution.end(), 0);
	pLabel.resize(protoCount);

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

void GsmLvqModel::RecomputeProjection(int protoIndex) {
#if EIGEN3
	P_prototype[protoIndex].noalias() = P * prototype[protoIndex];
#else
	P_prototype[protoIndex] = (P * prototype[protoIndex]).lazy();
#endif
}

int GsmLvqModel::classify(VectorXd const & unknownPoint) const{
	Vector2d P_otherPoint;
#if EIGEN3
	P_otherPoint.noalias() = P * unknownPoint;
#else
	P_otherPoint = (P * unknownPoint).lazy();
#endif

	using namespace std;
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<pLabel.size();i++) {
		double curDist = SqrDistanceTo(i,P_otherPoint);
		if(curDist < distance) {
			match=i;
			distance = curDist;
		}
	}
	assert( match >= 0 );
	return this->pLabel(match);
}

int GsmLvqModel::classifyProjectedInternal(Vector2d const & P_otherPoint) const{
	using namespace std;
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<pLabel.size();i++) {
		double curDist = SqrDistanceTo(i, P_otherPoint);
		if(curDist < distance) {
			match=i;
			distance = curDist;
		}
	}
	assert( match >= 0 );
	return this->pLabel(match);
}


GsmLvqModel::GoodBadMatch GsmLvqModel::findMatches(Vector2d const & P_trainPoint, int trainLabel) {
	GoodBadMatch match;

	for(int i=0;i<pLabel.size();i++) {
		double curDist = SqrDistanceTo(i,P_trainPoint);
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

void GsmLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	double learningRate = getLearningRate();
	incLearningIterationCount();

	using namespace std;

	double lr_point = learningRate,
		lr_P = learningRate * this->lr_scale_P;

	assert(lr_P>=0  &&  lr_point>=0);

	Vector2d P_trainPoint;
#if EIGEN3
	P_trainPoint.noalias() = P * trainPoint;
#else
	P_trainPoint = (P * trainPoint).lazy();
#endif
	GoodBadMatch matches = findMatches(P_trainPoint, trainLabel);

	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distGood / (sqr(matches.distGood) + sqr(matches.distBad));
	double mu_K = +2.0*matches.distBad / (sqr(matches.distGood) + sqr(matches.distBad));
	
	int J = matches.matchGood;
	int K = matches.matchBad;

	//VectorXd
#if EIGEN3
	Vector2d muK2_P_vJ, muJ2_P_vK;
	muK2_P_vJ.noalias() = mu_K * 2.0 * (P_prototype[J] - P_trainPoint) ;
	muJ2_P_vK.noalias() = mu_J * 2.0 * (P_prototype[K] - P_trainPoint);

	dQdwJ.noalias() = P.transpose() * muK2_P_vJ; //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK.noalias() = P.transpose() * muJ2_P_vK;
	prototype[J].noalias() -= lr_point * dQdwJ;
	prototype[K].noalias() -= lr_point * dQdwK;

	dQdP.noalias() = muK2_P_vJ * vJ.transpose() + muJ2_P_vK * vK.transpose(); //differential wrt. global projection matrix.
	P.noalias() -= lr_P * dQdP;
#else
	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	Vector2d muK2_P_vJ = (mu_K * 2.0 * (P_prototype[J] - P_trainPoint) ).lazy();
	Vector2d muJ2_P_vK = (mu_J * 2.0 * (P_prototype[K] - P_trainPoint) ).lazy();

	dQdwJ = (P.transpose() * muK2_P_vJ).lazy(); //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK = (P.transpose() * muJ2_P_vK).lazy();
	prototype[J] -= lr_point * dQdwJ;
	prototype[K] -= lr_point * dQdwK;

	dQdP = (muK2_P_vJ * vJ.transpose()).lazy() + (muJ2_P_vK * vK.transpose()).lazy(); //differential wrt. global projection matrix.
	P -= lr_P * dQdP;
#endif

#if EIGEN3
	double pNormScale =1.0 / ( (P.transpose() * P).diagonal().sum());
#else
	double pNormScale =1.0 / ( (P.transpose() * P).lazy().diagonal().sum());
#endif
	P *= pNormScale;

	for(int i=0;i<pLabel.size();++i)
		RecomputeProjection(i);
}

void GsmLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const {
	int cols = classDiagram.cols();
	int rows = classDiagram.rows();
	for(int xCol=0;  xCol < cols;  xCol++) {
		double x = x0 + (x1-x0) * (xCol+0.5) / cols;
		for(int yRow=0;  yRow < rows;  yRow++) {
			double y = y0+(y1-y0) * (yRow+0.5) / rows;
			Vector2d vec(x,y);
			classDiagram(yRow,xCol) = classifyProjectedInternal(vec);
		}
	}
}

AbstractLvqModel* GsmLvqModel::clone() { return new GsmLvqModel(*this);	}
