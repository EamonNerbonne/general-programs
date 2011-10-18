#include "StdAfx.h"
#include "LgmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
#include "SmartSum.h"

LgmLvqModel::LgmLvqModel( LvqModelSettings & initSettings)
	: LvqModel(initSettings)
	, totalMuJLr(0.0)
	, totalMuKLr(0.0)
	, tmpSrcDimsV1(initSettings.Dimensions())
	, tmpSrcDimsV2(initSettings.Dimensions())
	, tmpDestDimsV1()
	, tmpDestDimsV2()
{
	if(initSettings.Dimensionality ==0)
		initSettings.Dimensionality = (int) initSettings.Dimensions();
	if(initSettings.Dimensionality < 0 || initSettings.Dimensionality > (int) initSettings.Dimensions()){
		std::cerr<< "Dimensionality out of range\n";
		std::exit(10);
	}

	tmpDestDimsV1.resize(initSettings.Dimensionality);
	tmpDestDimsV2.resize(initSettings.Dimensionality);

	initSettings.AssertModelIsOfRightType(this);

	using namespace std;
	auto InitProto = initSettings.InitProtosBySetting();

	pLabel = InitProto.second;
	size_t protoCount = pLabel.size();
	prototype.resize(protoCount);
	P.resize(protoCount);

	for(size_t protoIndex = 0; protoIndex < protoCount; ++protoIndex) {
		prototype[protoIndex] = InitProto.first.col(protoIndex);
		P[protoIndex].setIdentity(initSettings.Dimensionality, initSettings.Dimensions());
		if(initSettings.RandomInitialProjection)
			projectionRandomizeUniformScaled(initSettings.RngParams, P[protoIndex]);
	}
}


MatchQuality LgmLvqModel::learnFrom(Vector_N const & trainPoint, int trainLabel) {
	double learningRate = stepLearningRate();
	double lr_point = settings.LR0 * learningRate;

	using namespace std;

	GoodBadMatch matches = findMatches(trainPoint, trainLabel);

	//now matches.good is "J" and matches.bad is "K".
	MatchQuality retval = matches.LvqQuality();

	double lr_mu_K2 = lr_point * 2.0*retval.muK;
	double lr_mu_J2 = lr_point * 2.0*retval.muJ;
	double lr_bad = (settings.SlowStartLrBad  ?  sqr(1.0 - learningRate)  :  1.0) * settings.LrScaleBad;

	int J = matches.matchGood;
	int K = matches.matchBad;

	Vector_N & vJ = tmpSrcDimsV1;
	Vector_N & vK = tmpSrcDimsV2;
	Vector_N & Pj_vJ = tmpDestDimsV1;
	Vector_N & Pk_vK = tmpDestDimsV2;

	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	Pj_vJ.noalias() =P[J] * vJ;
	Pk_vK.noalias() = P[K] * vK;

	prototype[J].noalias() -= (lr_mu_J2)* (P[J].transpose() * Pj_vJ);
	prototype[K].noalias() -= (lr_bad * lr_mu_K2) * (P[K].transpose() * Pk_vK);

	P[J].noalias() -= (settings.LrScaleP *  lr_mu_J2) * (Pj_vJ * vJ.transpose() );
	P[K].noalias() -=(settings.LrScaleP * lr_mu_K2) * (Pk_vK * vK.transpose() );

	totalMuJLr += lr_point * retval.muJ;
	totalMuKLr -= lr_point * retval.muK;

	return retval;
}

MatchQuality LgmLvqModel::ComputeMatches(Vector_N const & unknownPoint, int pointLabel) const { return findMatches(unknownPoint,pointLabel).LvqQuality();}

size_t LgmLvqModel::MemAllocEstimate() const {
	return 
		sizeof(LgmLvqModel) + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		(sizeof(double) * P[0].size() + sizeof(Matrix_NN)) * P.size() + //dyn alloc prototype transforms
		sizeof(double) * (tmpSrcDimsV1.size() + tmpSrcDimsV2.size() + tmpDestDimsV1.size() + tmpDestDimsV2.size()) + //various vector temps
		(sizeof(Vector_N) + sizeof(double)*prototype[0].size()) *prototype.size() +//dyn alloc prototypes
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
}

void LgmLvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const {
	LvqModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Projection Norm Maximum!norm!Prototype Matrix");
	retval.push_back(L"Projection Norm Mean!norm!Prototype Matrix");
	retval.push_back(L"Projection Norm Minimum!norm!Prototype Matrix");

	retval.push_back(L"Cumulative \u03BC-J-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
	retval.push_back(L"Cumulative \u03BC-K-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");

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

	stats.push_back(maxNorm);
	stats.push_back(normSum / P.size());
	stats.push_back(minNorm);

	stats.push_back(totalMuJLr);
	stats.push_back(totalMuKLr);
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
			double overallNorm = std::accumulate(P.begin(), P.end(),0.0,[](double cur, Matrix_NN const & mat)->double { return cur + projectionSquareNorm(mat); });
			double scale = 1.0/sqrt(overallNorm / P.size());
			for(size_t i=0;i<P.size();++i) P[i]*=scale;
		} else {
			for(size_t i=0;i<P.size();++i) normalizeProjection(P[i]);
		}
	}
}


Matrix_NN LgmLvqModel::PrototypeDistances(Matrix_NN const & points) const {
	Matrix_NN tmpPointsDiff, tmpPointsDiffProj, tmpDists;
	Matrix_NN newPoints(prototype.size(), points.cols());
	for(size_t protoI=0;protoI<prototype.size();++protoI) {
		tmpPointsDiff.noalias() = points.colwise() - prototype[protoI];
		tmpPointsDiffProj.noalias() = P[protoI] * tmpPointsDiff;
		newPoints.row(protoI).noalias() = (tmpPointsDiffProj.colwise().norm().array() + 0.01).inverse().matrix();//not squaredNorm?
	}
	return newPoints;
}

Matrix_NN LgmLvqModel::GetCombinedTransforms() const{
	size_t totalRows = std::accumulate(P.cbegin(),P.cend(),size_t(0), [] (size_t val, Matrix_NN const & p) { return val + p.rows(); });
	Matrix_NN retval(totalRows,P[0].cols());
	size_t rowInit=0;
	std::for_each(P.cbegin(),P.cend(),[&rowInit,&retval] (Matrix_NN const & p) {
		retval.middleRows(rowInit, p.rows()).noalias() = p;
		rowInit += p.rows();
	});
	return retval;
}
