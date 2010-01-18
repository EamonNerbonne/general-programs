#include "stdafx.h"
#include "G2mLvqModel.h"
#include "utils.h"
#include "G2mLvqMatch.h"
#include "LvqConstants.h"

G2mLvqModel::G2mLvqModel(boost::mt19937 & rng,  bool randInit, std::vector<int> protodistribution, MatrixXd const & means) 
	: AbstractProjectionLvqModel(means.rows()) 
	, classCount((int)protodistribution.size())
	, lr_scale_P(LVQ_LrScaleP)
	, lr_scale_B(LVQ_LrScaleB)
	, vJ(means.rows())
	, vK(means.rows())
	, dQdwJ(means.rows())
	, dQdwK(means.rows())
	, dQdP(LVQ_LOW_DIM_SPACE,means.rows())
{
	using namespace std;

	if(randInit)
		projectionRandomizeUniformScaled(rng, P);
	else
		P.setIdentity();

	protoCount = accumulate(protodistribution.begin(),protodistribution.end(),0);
	prototype.resize(protoCount);
	int protoIndex=0;
	for(int label=0; label <(int) protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = G2mLvqPrototype(rng,randInit, label, protoIndex, means.col(label) );
			prototype[protoIndex].ComputePP(P);

			protoIndex++;
		}
	}
	assert( accumulate(protodistribution.begin(), protodistribution.end(), 0)== protoIndex);
}

int G2mLvqModel::classify(VectorXd const & unknownPoint) const{
	using namespace std;
	Vector2d P_unknownPoint = (P * unknownPoint).lazy();
	G2mLvqMatch matches(&P_unknownPoint);

	for(int i=0;i<protoCount;i++)
		matches.AccumulateMatch(prototype[i]);

	assert(matches.match != NULL);
	return matches.match->ClassLabel();
}

int G2mLvqModel::classifyProjectedInternal(Vector2d const & P_unknownPoint) const{
	using namespace std;
	G2mLvqMatch matches(&P_unknownPoint);

	for(int i=0;i<protoCount;i++)
		matches.AccumulateMatch(prototype[i]);

	assert(matches.match != NULL);
	return matches.match->ClassLabel();
}


void G2mLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	using namespace std;
	double learningRate = getLearningRate();
	incLearningIterationCount();

	double lr_point = learningRate,
		lr_P = learningRate * this->lr_scale_P,
		lr_B = learningRate * this->lr_scale_B; 

	assert(lr_P>=0  &&  lr_B>=0  &&  lr_point>=0);

	Vector2d projectedTrainPoint = (P * trainPoint).lazy();

	G2mLvqGoodBadMatch matches(&projectedTrainPoint, trainLabel);

	for(int i=0;i<protoCount;i++)
		matches.AccumulateMatch(prototype[i]);

	assert(matches.good !=NULL && matches.bad!=NULL);
	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distanceGood / (sqr( matches.distanceGood) + sqr(matches.distanceBad));
	double mu_K = +2.0*matches.distanceBad / (sqr( matches.distanceGood) + sqr(matches.distanceBad));

	G2mLvqPrototype *J = const_cast<G2mLvqPrototype *>(matches.good);
	G2mLvqPrototype *K = const_cast<G2mLvqPrototype *>(matches.bad);
	assert(J == matches.good && K == matches.bad);

	//VectorXd
	vJ = (J->point - trainPoint).lazy();
	vK = (K->point - trainPoint).lazy();

#ifdef BPROJ
	Vector2d P_vJ = ( P * vJ ).lazy();
	Vector2d P_vK =( P * vK ).lazy();
#else
	Vector2d P_vJ = J->P_point - projectedTrainPoint;
	Vector2d P_vK = K->P_point - projectedTrainPoint;
#endif

	Vector2d muK2_Bj_P_vJ = mu_K * 2.0 * ( J->B * P_vJ ).lazy();
	Vector2d muJ2_Bk_P_vK = mu_J * 2.0 * ( K->B * P_vK ).lazy();

	Vector2d muK2_BjT_Bj_P_vJ =  (J->B.transpose() * muK2_Bj_P_vJ).lazy();
	Vector2d muJ2_BkT_Bk_P_vK = (K->B.transpose() * muJ2_Bk_P_vK).lazy();

	//performance: J->B, J->point, K->B, and K->point, are write only from hereon forward, so we _could_ fold the differential computation info the update statement (less intermediates, but strangely not faster).

	
	//*
	Matrix2d dQdBj = (muK2_Bj_P_vJ * P_vJ.transpose()).lazy();
	Matrix2d dQdBk = (muJ2_Bk_P_vK * P_vK.transpose()).lazy();
	J->B = J->B - lr_B * dQdBj ;
	K->B = K->B - lr_B * dQdBk ;
	//double jBnormScale =1.0 / ( (J->B.transpose() * J->B).lazy().diagonal().sum());
	//J->B *= jBnormScale;
	//double kBnormScale =1.0 / ( (K->B.transpose() * K->B).lazy().diagonal().sum());
	//K->B *= kBnormScale;

	/*/
	J->B = J->B - lr_B * (muK2_Bj_P_vJ * P_vJ.transpose()).lazy();
	K->B = K->B - lr_B * (muJ2_Bk_P_vK * P_vK.transpose()).lazy();
	/**/


	//*
	dQdwJ = (P.transpose() *  muK2_BjT_Bj_P_vJ).lazy(); //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK = (P.transpose() * muJ2_BkT_Bk_P_vK).lazy();
	J->point = J->point - lr_point * dQdwJ ;
	K->point = K->point - lr_point * dQdwK ;
	/*/
	J->point = J->point - lr_point * (P.transpose() *  muK2_BjT_Bj_P_vJ).lazy();
	K->point = K->point - lr_point * (P.transpose() * muJ2_BkT_Bk_P_vK).lazy();
	/**/

	/*
	dQdP = (muK2_BjT_Bj_P_vJ * vJ.transpose()).lazy() + (muJ2_BkT_Bk_P_vK * vK.transpose()).lazy(); //differential wrt. global projection matrix.
	P = P - lr_P * dQdP ;
	/*/
	P = P - lr_P * ( (muK2_BjT_Bj_P_vJ * vJ.transpose()).lazy() + (muJ2_BkT_Bk_P_vK * vK.transpose()).lazy()) ;
	/**/
	double pNormScale =1.0 / ( (P.transpose() * P).lazy().diagonal().sum());
	P *= pNormScale;

	for(int i=0;i<protoCount;i++)
		prototype[i].ComputePP(P);
}

void G2mLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const {
	int cols = classDiagram.cols();
	int rows = classDiagram.rows();
	for(int xCol=0;  xCol < cols;  xCol++) {
		double x = x0 + (x1-x0) * (xCol+0.5) / cols;
		for(int yRow=0;  yRow < rows;  yRow++) {
			double y = y0+(y1-y0) * (yRow+0.5) / rows;
			Vector2d vec(x,y);
			classDiagram(yRow, xCol) = classifyProjectedInternal(vec);
		}
	}
}

AbstractLvqModel* G2mLvqModel::clone() { return new G2mLvqModel(*this); }