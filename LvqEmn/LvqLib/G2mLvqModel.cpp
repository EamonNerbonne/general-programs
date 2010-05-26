#include "stdafx.h"
#include "G2mLvqModel.h"
#include "utils.h"
#include "G2mLvqMatch.h"
#include "LvqConstants.h"

using namespace std;
using namespace Eigen;

G2mLvqModel::G2mLvqModel(boost::mt19937 & rng,  bool randInit, vector<int> protodistribution, MatrixXd const & means) 
	: AbstractProjectionLvqModel(means.rows(),(int)protodistribution.size()) 
	, lr_scale_P(LVQ_LrScaleP)
	, lr_scale_B(LVQ_LrScaleB)
	, m_vJ(means.rows())
	, m_vK(means.rows())
{
	using namespace std;

	if(randInit)
		projectionRandomizeUniformScaled(rng, P);
	else
		P.setIdentity();

	int protoCount = accumulate(protodistribution.begin(),protodistribution.end(),0);
	iterationScaleFactor/=protoCount;
	prototype.resize(protoCount);

	int protoIndex=0;
	for(int label=0; label <(int) protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = G2mLvqPrototype(rng,false, label, means.col(label) );//TODO:experiment with random projection initialization.
			prototype[protoIndex].ComputePP(P);

			protoIndex++;
		}
	}
	assert( accumulate(protodistribution.begin(), protodistribution.end(), 0)== protoIndex);
}


typedef Map<VectorXd,  Aligned> MVectorXd;

void G2mLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {

	using namespace std;
	//double learningRate = getLearningRate();
	//incLearningIterationCount();
	double learningRate = stepLearningRate();

	double lr_point = learningRate,
		lr_P = learningRate * this->lr_scale_P,
		lr_B = learningRate * this->lr_scale_B; 

	assert(lr_P>=0  &&  lr_B>=0  &&  lr_point>=0);

#if EIGEN3
	Vector2d projectedTrainPoint = P * trainPoint;
	//projectedTrainPoint.noalias() = P * trainPoint;
#else
	Vector2d projectedTrainPoint = (P * trainPoint).lazy();
#endif

	G2mLvqGoodBadMatch matches(&projectedTrainPoint, trainLabel);

	for(size_t i=0;i<prototype.size();i++)
		matches.AccumulateMatch(prototype[i]);

	assert(matches.good !=NULL && matches.bad!=NULL);
	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distanceGood / (sqr( matches.distanceGood) + sqr(matches.distanceBad));
	double mu_K = +2.0*matches.distanceBad / (sqr( matches.distanceGood) + sqr(matches.distanceBad));

	G2mLvqPrototype *J = const_cast<G2mLvqPrototype *>(matches.good);
	G2mLvqPrototype *K = const_cast<G2mLvqPrototype *>(matches.bad);
	assert(J == matches.good && K == matches.bad);
	
	MVectorXd vJ(m_vJ.data(),m_vJ.size());
	MVectorXd vK(m_vK.data(),m_vK.size());

	vJ = J->point - trainPoint;
	vK = K->point - trainPoint;
	Vector2d P_vJ= J->P_point - projectedTrainPoint;
	Vector2d P_vK = K->P_point - projectedTrainPoint;

	Vector2d muK2_Bj_P_vJ, muJ2_Bk_P_vK,muK2_BjT_Bj_P_vJ,muJ2_BkT_Bk_P_vK;

#if EIGEN3
	muK2_Bj_P_vJ.noalias() = (mu_K * 2.0) *  (J->B * P_vJ) ;
	muJ2_Bk_P_vK.noalias() = (mu_J * 2.0) *  (K->B * P_vK) ;
	muK2_BjT_Bj_P_vJ.noalias() =  J->B.transpose() * muK2_Bj_P_vJ ;
	muJ2_BkT_Bk_P_vK.noalias() = K->B.transpose() * muJ2_Bk_P_vK ;
	J->B.noalias() -= lr_B * muK2_Bj_P_vJ * P_vJ.transpose() ;
	K->B.noalias() -= lr_B * muJ2_Bk_P_vK * P_vK.transpose() ;
	J->point.noalias() -=  P.transpose() * (lr_point * muK2_BjT_Bj_P_vJ) ;
	K->point.noalias() -=   P.transpose() * (lr_point * muJ2_BkT_Bk_P_vK) ;
	P.noalias() -= (lr_P * muK2_BjT_Bj_P_vJ) * vJ.transpose() + (lr_P * muJ2_BkT_Bk_P_vK) * vK.transpose() ;
	//	double pNormScale =1.0 /  (P.transpose() * P).diagonal().sum();
	//	P *= pNormScale;
#else
	muK2_Bj_P_vJ = mu_K * 2.0 * ( J->B * P_vJ ).lazy();
	muJ2_Bk_P_vK = mu_J * 2.0 * ( K->B * P_vK ).lazy();
	muK2_BjT_Bj_P_vJ =  (J->B.transpose() * muK2_Bj_P_vJ).lazy();
	muJ2_BkT_Bk_P_vK = (K->B.transpose() * muJ2_Bk_P_vK).lazy();
	Matrix2d dQdBj = (muK2_Bj_P_vJ * P_vJ.transpose()).lazy();
	Matrix2d dQdBk = (muJ2_Bk_P_vK * P_vK.transpose()).lazy();
	J->B -= lr_B * dQdBj;
	K->B -= lr_B * dQdBk;
	J->point -= (P.transpose() * (lr_point * muK2_BjT_Bj_P_vJ )).lazy();
	K->point -= (P.transpose() * (lr_point * muJ2_BkT_Bk_P_vK )).lazy();
	P -=  ((lr_P * muK2_BjT_Bj_P_vJ) * vJ.transpose()).lazy() + ((lr_P * muJ2_BkT_Bk_P_vK) * vK.transpose()).lazy();
    //double pNormScale =1.0 /  (P.transpose() * P).lazy().diagonal().sum();
	//	P *= pNormScale;
#endif

	for(size_t i=0;i<prototype.size();++i)
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

size_t G2mLvqModel::MemAllocEstimate() const {
	return 
		sizeof(G2mLvqModel) +
		sizeof(double) * P.size() +
		sizeof(double) * (m_vJ.size() +m_vK.size()) + //various temps
		sizeof(G2mLvqPrototype)*prototype.size() + //prototypes; part statically allocated
		sizeof(double) * (prototype.size() * m_vJ.size()) + //prototypes; part dynamically allocated
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
}

MatrixXd G2mLvqModel::GetProjectedPrototypes() const {
	MatrixXd retval(LVQ_LOW_DIM_SPACE, static_cast<int>(prototype.size()));
	for(unsigned i=0;i<prototype.size();++i)
		retval.col(i) = prototype[i].projectedPosition();
	return retval;
}

vector<int> G2mLvqModel::GetPrototypeLabels() const {
	vector<int> retval(prototype.size());
	for(unsigned i=0;i<prototype.size();++i)
		retval[i] = prototype[i].label();
	return retval;
}
