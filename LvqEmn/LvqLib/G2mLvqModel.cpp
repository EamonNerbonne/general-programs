#include "stdafx.h"
#include "G2mLvqModel.h"
#include "utils.h"
#include "G2mLvqMatch.h"
#include "LvqConstants.h"


G2mLvqModel::G2mLvqModel(boost::mt19937 & rng,  bool randInit, std::vector<int> protodistribution, MatrixXd const & means) 
	: AbstractProjectionLvqModel<G2mLvqModel>(means.rows()) 
	, lr_scale_P(LVQ_LrScaleP)
	, lr_scale_B(LVQ_LrScaleB)
	, classCount((int)protodistribution.size())
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



EIGEN_DONT_INLINE void G2mLvqModel::learnFromImpl(VectorXd const & trainPoint, int trainLabel) {

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

	for(int i=0;i<prototype.size();i++)
		matches.AccumulateMatch(prototype[i]);

	assert(matches.good !=NULL && matches.bad!=NULL);
	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distanceGood / (sqr( matches.distanceGood) + sqr(matches.distanceBad));
	double mu_K = +2.0*matches.distanceBad / (sqr( matches.distanceGood) + sqr(matches.distanceBad));

	G2mLvqPrototype *J = const_cast<G2mLvqPrototype *>(matches.good);
	G2mLvqPrototype *K = const_cast<G2mLvqPrototype *>(matches.bad);
	assert(J == matches.good && K == matches.bad);

	//VectorXd
#if EIGEN3
	vJ = J->point - trainPoint;
	vK = K->point - trainPoint;
#else
	vJ = J->point - trainPoint;
	vK = K->point - trainPoint;

#endif

#ifdef BPROJ
	Vector2d P_vJ = ( P * vJ ).lazy();
	Vector2d P_vK =( P * vK ).lazy();
#else
#if EIGEN3
	Vector2d P_vJ= J->P_point - projectedTrainPoint;
	Vector2d P_vK = K->P_point - projectedTrainPoint;
#else
	Vector2d P_vJ = J->P_point - projectedTrainPoint;
	Vector2d P_vK = K->P_point - projectedTrainPoint;
#endif
#endif

	Vector2d muK2_Bj_P_vJ, muJ2_Bk_P_vK,muK2_BjT_Bj_P_vJ,muJ2_BkT_Bk_P_vK;
#if EIGEN3
	muK2_Bj_P_vJ.noalias() = (mu_K * 2.0) *  (J->B * P_vJ) ;
	muJ2_Bk_P_vK.noalias() = (mu_J * 2.0) *  (K->B * P_vK) ;
	muK2_BjT_Bj_P_vJ.noalias() =  J->B.transpose() * muK2_Bj_P_vJ;
	muJ2_BkT_Bk_P_vK.noalias() = K->B.transpose() * muJ2_Bk_P_vK;
#else
	muK2_Bj_P_vJ = mu_K * 2.0 * ( J->B * P_vJ ).lazy();
	muJ2_Bk_P_vK = mu_J * 2.0 * ( K->B * P_vK ).lazy();
	muK2_BjT_Bj_P_vJ =  (J->B.transpose() * muK2_Bj_P_vJ).lazy();
	muJ2_BkT_Bk_P_vK = (K->B.transpose() * muJ2_Bk_P_vK).lazy();
#endif
	//performance: J->B, J->point, K->B, and K->point, are write only from hereon forward, so we _could_ fold the differential computation info the update statement (less intermediates, but strangely not faster).


	Matrix2d dQdBj, dQdBk;
#if EIGEN3
	dQdBj.noalias() = muK2_Bj_P_vJ * P_vJ.transpose();
	dQdBk.noalias() = muJ2_Bk_P_vK * P_vK.transpose();
	J->B -= lr_B * dQdBj ;
	K->B -= lr_B * dQdBk ;
#else
	dQdBj = (muK2_Bj_P_vJ * P_vJ.transpose()).lazy();
	dQdBk = (muJ2_Bk_P_vK * P_vK.transpose()).lazy();
	J->B -= lr_B * dQdBj;
	K->B -= lr_B * dQdBk;
#endif

	//double jBnormScale =1.0 / ( (J->B.transpose() * J->B).lazy().diagonal().sum());
	//J->B *= jBnormScale;
	//double kBnormScale =1.0 / ( (K->B.transpose() * K->B).lazy().diagonal().sum());
	//K->B *= kBnormScale;


#if EIGEN3
	dQdwJ.noalias() = P.transpose() *  muK2_BjT_Bj_P_vJ; //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK.noalias() = P.transpose() * muJ2_BkT_Bk_P_vK;
	J->point -=  lr_point * dQdwJ ;
	K->point -=  lr_point * dQdwK ;
#else
	dQdwJ = (P.transpose() *  muK2_BjT_Bj_P_vJ).lazy(); //differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	dQdwK = (P.transpose() * muJ2_BkT_Bk_P_vK).lazy();
	J->point = J->point - lr_point * dQdwJ ;
	K->point = K->point - lr_point * dQdwK ;
#endif

	//dQdP = (muK2_BjT_Bj_P_vJ * vJ.transpose()).lazy() + (muJ2_BkT_Bk_P_vK * vK.transpose()).lazy(); //differential wrt. global projection matrix.
	//P = P - lr_P * dQdP ;
#if EIGEN3
	P.noalias() -= lr_P * ( muK2_BjT_Bj_P_vJ * vJ.transpose() + muJ2_BkT_Bk_P_vK * vK.transpose()) ;
//	double pNormScale =1.0 /  (P.transpose() * P).diagonal().sum();
#else
	P =P-  lr_P * ( (muK2_BjT_Bj_P_vJ * vJ.transpose()).lazy() + (muJ2_BkT_Bk_P_vK * vK.transpose()).lazy()) ;
//	double pNormScale =1.0 /  (P.transpose() * P).lazy().diagonal().sum();
#endif
//	P *= pNormScale;

	

	for(int i=0;i<prototype.size();++i)
		prototype[i].ComputePP(P);
}



size_t G2mLvqModel::MemAllocEstimateImpl() const {
	return 
		sizeof(G2mLvqModel) +
		sizeof(double) * (P.size() + dQdP.size()) +
		sizeof(double) * (vJ.size()*4) + //various temps
		sizeof(G2mLvqPrototype)*prototype.size() + //prototypes; part statically allocated
		sizeof(double) * (prototype.size() * vJ.size()) + //prototypes; part dynamically allocated
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
}

