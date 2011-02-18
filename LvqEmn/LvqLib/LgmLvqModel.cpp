#include "StdAfx.h"
#include "LgmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"

LgmLvqModel::LgmLvqModel( LvqModelSettings & initSettings)
	: LvqModel(initSettings)
	, tmpSrcDimsV1(initSettings.Dimensions())
	, tmpSrcDimsV2(initSettings.Dimensions())
	, tmpDestDimsV2()
	, tmpDestDimsV1()
{
	if(initSettings.Dimensionality ==0)
		initSettings.Dimensionality = (int) initSettings.Dimensions();
	if(initSettings.Dimensionality < 0 || initSettings.Dimensionality > (int) initSettings.Dimensions())
		throw "Dimensionality out of range";

	tmpDestDimsV1.resize(initSettings.Dimensionality);
	tmpDestDimsV2.resize(initSettings.Dimensionality);

	initSettings.AssertModelIsOfRightType(this);

	using namespace std;
	auto InitProto = initSettings.InitProtosBySetting();

	pLabel = InitProto.second;
	size_t protoCount = pLabel.size();
	prototype.resize(protoCount);
	P.resize(protoCount);

	for(int protoIndex = 0; protoIndex < protoCount; ++protoIndex) {
		prototype[protoIndex] = InitProto.first.col(protoIndex);
		P[protoIndex].setIdentity(initSettings.Dimensionality, initSettings.Dimensions());
		if(initSettings.RandomInitialProjection)
			projectionRandomizeUniformScaled(initSettings.RngParams, P[protoIndex]);
	}
}


MatchQuality LgmLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	double learningRate = stepLearningRate();
	double lr_point = settings.LR0 * learningRate;

	using namespace std;

	GoodBadMatch matches = findMatches(trainPoint, trainLabel);

	//now matches.good is "J" and matches.bad is "K".

	double lr_mu_J2 = lr_point * 2.0*matches.MuK();
	double lr_mu_K2 = lr_point * 2.0*matches.MuJ();

	int J = matches.matchGood;
	int K = matches.matchBad;

	VectorXd & vJ = tmpSrcDimsV1;
	VectorXd & vK = tmpSrcDimsV2;
	VectorXd & Pj_vJ = tmpDestDimsV1;
	VectorXd & Pk_vK = tmpDestDimsV2;

	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	Pj_vJ.noalias() =P[J] * vJ;
	Pk_vK.noalias() = P[K] * vK;

	prototype[J].noalias() -= (lr_mu_K2)* (P[J].transpose() * Pj_vJ);
	prototype[K].noalias() -= (settings.LrScaleBad*lr_mu_J2) * (P[K].transpose() * Pk_vK);

	P[J].noalias() -= (settings.LrScaleP *  lr_mu_K2) * (Pj_vJ * vJ.transpose() );
	P[K].noalias() -=(settings.LrScaleP *lr_mu_J2) * (Pk_vK * vK.transpose() );
	return matches.LvqQuality();
}

MatchQuality LgmLvqModel::ComputeMatches(VectorXd const & unknownPoint, int pointLabel) const {return findMatches(unknownPoint,pointLabel).LvqQuality();}

size_t LgmLvqModel::MemAllocEstimate() const {
	return 
		sizeof(LgmLvqModel) + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		(sizeof(double) * P[0].size() + sizeof(MatrixXd)) * P.size() + //dyn alloc prototype transforms
		sizeof(double) * (tmpSrcDimsV1.size() + tmpSrcDimsV2.size() + tmpDestDimsV1.size() + tmpDestDimsV2.size()) + //various vector temps
		(sizeof(VectorXd) + sizeof(double)*prototype[0].size()) *prototype.size() +//dyn alloc prototypes
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
}

void LgmLvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const {
	LvqModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Projection Norm Minimum|norm|Prototype Matrix");
	retval.push_back(L"Projection Norm Mean|norm|Prototype Matrix");
	retval.push_back(L"Projection Norm Maximum|norm|Prototype Matrix");
}
void LgmLvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const {
	LvqModel::AppendOtherStats(stats,trainingSet,trainingSubset,testSet,testSubset);
	double minNorm=std::numeric_limits<double>::max();
	double maxNorm=0.0;
	double normSum=0.0;

	for(size_t i=0;i<P.size();++i) {
		double norm = projectionSquareNorm(P[i]);
		if(norm <minNorm) minNorm = norm;
		if(norm > maxNorm) maxNorm = norm;
		normSum+=norm;
	}

	stats.push_back(minNorm);
	stats.push_back(normSum / P.size());
	stats.push_back(maxNorm);
}

vector<int> LgmLvqModel::GetPrototypeLabels() const {
	vector<int> retval(prototype.size());
	for(unsigned i=0;i<prototype.size();++i)
		retval[i] = pLabel[i];
	return retval;
}

void LgmLvqModel::DoOptionalNormalization() {
	if(settings.NormalizeProjection) {
		if(settings.GloballyNormalize) {
			double overallNorm = std::accumulate(P.begin(), P.end(),0.0,[](double cur, MatrixXd const & mat)->double { return cur + projectionSquareNorm(mat); });
			double scale = 1.0/sqrt(overallNorm / P.size());
			for(size_t i=0;i<P.size();++i) P[i]*=scale;
		} else {
			for(size_t i=0;i<P.size();++i) normalizeProjection(P[i]);
		}
	}
}