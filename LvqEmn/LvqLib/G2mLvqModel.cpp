#include "stdafx.h"
#include "G2mLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
using namespace std;
using namespace Eigen;

G2mLvqModel::G2mLvqModel(LvqModelInitSettings & initSettings)
	: LvqProjectionModelBase(initSettings)
	, lr_scale_P(LVQ_LrScaleP)
	, lr_scale_B(LVQ_LrScaleB)
	, m_vJ(initSettings.Dimensions())
	, m_vK(initSettings.Dimensions())
{
	using namespace std;
	initSettings.AssertModelIsOfRightType(this);


	int protoCount = accumulate(initSettings.PrototypeDistribution.begin(),initSettings.PrototypeDistribution.end(),0);
	iterationScaleFactor/=protoCount;
	prototype.resize(protoCount);

	int protoIndex=0;
	for(int label=0; label <(int) initSettings.PrototypeDistribution.size();label++) {
		int labelCount =initSettings.PrototypeDistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = G2mLvqPrototype(initSettings.RngParams, initSettings.RandomInitialBorders, label, initSettings.PerClassMeans.col(label));//TODO:experiment with random projection initialization.
			prototype[protoIndex].ComputePP(P);
			protoIndex++;
		}
	}
	assert( accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0)== protoIndex);
}
typedef Map<VectorXd,  Aligned> MVectorXd;

GoodBadMatch G2mLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	using namespace std;
	double learningRate = stepLearningRate();

	double lr_point = learningRate,
		lr_P = learningRate * lr_scale_P,
		lr_B = learningRate * lr_scale_B; 

	assert(lr_P>=0 && lr_B>=0 && lr_point>=0);

	Vector2d projectedTrainPoint( P * trainPoint );

	GoodBadMatch matches = findMatches(projectedTrainPoint, trainLabel);

	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distGood / (sqr(matches.distGood) + sqr(matches.distBad));
	double mu_K = +2.0*matches.distBad / (sqr(matches.distGood) + sqr(matches.distBad));

	G2mLvqPrototype &J = prototype[matches.matchGood];
	G2mLvqPrototype &K = prototype[matches.matchBad];
	
	MVectorXd vJ(m_vJ.data(),m_vJ.size());
	MVectorXd vK(m_vK.data(),m_vK.size());
	vJ = J.point - trainPoint;
	vK = K.point - trainPoint;
	Vector2d P_vJ= J.P_point - projectedTrainPoint;
	Vector2d P_vK = K.P_point - projectedTrainPoint;

	Vector2d muK2_Bj_P_vJ, muJ2_Bk_P_vK,muK2_BjT_Bj_P_vJ,muJ2_BkT_Bk_P_vK;

	muK2_Bj_P_vJ.noalias() = (mu_K * 2.0) *  (J.B * P_vJ) ;
	muJ2_Bk_P_vK.noalias() = (mu_J * 2.0) *  (K.B * P_vK) ;
	muK2_BjT_Bj_P_vJ.noalias() =  J.B.transpose() * muK2_Bj_P_vJ ;
	muJ2_BkT_Bk_P_vK.noalias() = K.B.transpose() * muJ2_Bk_P_vK ;
	J.B.noalias() -= lr_B * muK2_Bj_P_vJ * P_vJ.transpose() ;
	K.B.noalias() -= lr_B * muJ2_Bk_P_vK * P_vK.transpose() ;
	J.point.noalias() -=  P.transpose() * (lr_point * muK2_BjT_Bj_P_vJ) ;
	K.point.noalias() -=   P.transpose() * (LVQ_LrScaleBad*lr_point * muJ2_BkT_Bk_P_vK) ;
	P.noalias() -= (lr_P * muK2_BjT_Bj_P_vJ) * vJ.transpose() + (lr_P * muJ2_BkT_Bk_P_vK) * vK.transpose() ;

	//normalizeProjection(P);
	for(size_t i=0;i<prototype.size();++i)
		prototype[i].ComputePP(P);
	return matches;
}


LvqModel* G2mLvqModel::clone() const { return new G2mLvqModel(*this); }

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

void G2mLvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const {
	LvqProjectionModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Border matrix norm min|norm|Border Matrix");
	retval.push_back(L"Border matrix norm mean|norm|Border Matrix");
	retval.push_back(L"Border matrix norm max|norm|Border Matrix");
}
void G2mLvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset) const {
	LvqProjectionModel::AppendOtherStats(stats,trainingSet,trainingSubset,testSet,testSubset);

	double minNorm=std::numeric_limits<double>::max();
	double maxNorm=0.0;
	double sumNorm=0.0;

	for(size_t i=0;i<prototype.size();++i) {
		double norm = projectionSquareNorm(prototype[i].B);
		sumNorm +=norm;
		if(norm <minNorm) minNorm = norm;
		if(norm > maxNorm) maxNorm = norm;
	}
	stats.push_back(minNorm);
	stats.push_back(sumNorm/prototype.size());
	stats.push_back(maxNorm);
}
