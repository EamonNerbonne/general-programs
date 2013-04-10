#include "stdafx.h"
#include "NormalLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
#include "MeanMinMax.h"
#include "LvqDataset.h"
#include "PCA.h"
#include "CovarianceAndMean.h"
#include "randomUnscalingMatrix.h"
#include "randomProjectionMatrix.h"

using namespace std;
using namespace Eigen;

#define DBGN(X) 
//#define DBGN(X) (std::cout<< #X <<":\n"<<(X)<<"\n\n")

inline Matrix_NN MakeUpperTriangular(Matrix_NN fullMat) {

	DBGN(fullMat);
	Matrix_NN square = fullMat.transpose()*fullMat;
	DBGN(square);
	//auto decomposition = square.llt();
	Matrix_NN retval = square.llt().matrixL();
	DBGN(retval);
#ifndef NDEBUG
	Matrix_NN alt = square.llt().matrixU();
	DBGN(alt);
	Matrix_NN alt2 = square.llt().matrixLLT();
	DBGN(alt2);
#endif

	Matrix_NN topR = retval.transpose().topRows(fullMat.rows());
	DBGN(topR);
	return topR;
}

double ComputeBias(Matrix_NN const & mat) {
	return - log(sqr(mat.diagonal().prod())); //mat.diagonal().prod() == mat.determinant() due to upper triangular B.
}

NormalLvqModel::NormalLvqModel(LvqModelSettings & initSettings)
	: LvqModel(initSettings)
	, totalMuJLr(0.0)
	, totalMuKLr(0.0)
	, tmpSrcDimsV1(initSettings.InputDimensions())
	, tmpSrcDimsV2(initSettings.InputDimensions())
	, tmpDestDimsV1()
	, tmpDestDimsV2()
{
	using namespace std;
	if(initSettings.Dimensionality ==0)
		initSettings.Dimensionality = (int) initSettings.InputDimensions();
	if(initSettings.Dimensionality < 0 || initSettings.Dimensionality > (int) initSettings.InputDimensions()){
		std::cerr<< "Dimensionality out of range\n";
		std::exit(10);
	}

	tmpDestDimsV1.resize(initSettings.Dimensionality);
	tmpDestDimsV2.resize(initSettings.Dimensionality);
	tmpDestDimsV3.resize(initSettings.Dimensionality);
	tmpDestDimsV4.resize(initSettings.Dimensionality);

	initSettings.AssertModelIsOfRightType(this);


	/*
	for(size_t protoIndex = 0; protoIndex < protoCount; ++protoIndex) {
	prototype[protoIndex].resize(initSettings.InputDimensions());
	P[protoIndex].resize(initP.rows(),initP.cols());
	prototype[protoIndex] = get<1>(projAndProtos).col(protoIndex);

	if(initSettings.Ppca || initSettings.Popt) {
	Matrix_NN rot = Matrix_NN(initP.rows(), initP.rows());
	randomProjectionMatrix(initSettings.RngParams, rot);
	P[protoIndex] = rot * initP;
	}else {
	P[protoIndex] = initP;
	randomProjectionMatrix(initSettings.RngParams, P[protoIndex]);
	}
	normalizeProjection(P[protoIndex]);
	}

	NormalizeP(settings.LocallyNormalize, P);
	*/
	auto InitProtos = initSettings.InitProtosAndProjectionBySetting();
	auto initP = get<0>(InitProtos);

	Matrix_NN prototypes = get<1>(InitProtos);
	pLabel.resizeLike(get<2>(InitProtos));
	pLabel = get<2>(InitProtos);

	prototype.resize(pLabel.size());
	P.resize(pLabel.size());
	pBias.resize(pLabel.size());

	for(int protoIndex=0; protoIndex < pLabel.size(); ++protoIndex) {
		prototype[protoIndex].resize(initSettings.InputDimensions());
		prototype[protoIndex] = prototypes.col(protoIndex);

		P[protoIndex].resize(initP.rows(), initP.cols());

		if(initSettings.Ppca || initSettings.Popt) {
			Matrix_NN rot = Matrix_NN(initP.rows(), initP.rows());
			randomProjectionMatrix(initSettings.RngParams, rot);
			P[protoIndex] = rot * initP;
		} else {
			randomProjectionMatrix(initSettings.RngParams, P[protoIndex]);
		}

		P[protoIndex] = MakeUpperTriangular(P[protoIndex]);
		pBias(protoIndex) = ComputeBias(P[protoIndex]);
	}

	//int maxProtoCount = accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0, [](int a, int b) -> int { return max(a,b); });
}

//typedef Map<Vector_N, Aligned> MVectorXd;

MatchQuality NormalLvqModel::learnFrom(Vector_N const & trainPoint, int trainLabel) {
	using namespace std;
//	const size_t protoCount = prototype.size();

	GoodBadMatch matches = findMatches(trainPoint, trainLabel);

	double learningRate = stepLearningRate(matches.matchGood);
	double lr_point = -settings.LR0 * learningRate,
		lr_P = lr_point * settings.LrScaleP,
		lr_bad = (settings.SlowK  ?  sqr(1.0 - learningRate)  :  1.0) * settings.LrScaleBad;

	assert(lr_P<=0 && lr_point<=0);

	//now matches.good is "J" and matches.bad is "K".
	int J = matches.matchGood, K = matches.matchBad;
	MatchQuality retval = matches.GgmQuality();
	double muJ2 = 2*retval.muJ;
	double muK2 = 2*retval.muK;
	double muJ2_alt = settings.MuOffset ==0 ? muJ2 : muJ2 + settings.MuOffset * learningRate * exp(-0.5*retval.distGood);
	//double muK2_alt =  settings.MuOffset ==0 ? muK2 : muK2 - settings.MuOffset * learningRate * exp(-0.5*ggmQuality.distBad);

	Vector_N & vJ = tmpSrcDimsV1;
	Vector_N & vK = tmpSrcDimsV2;
	Vector_N & Pj_vJ = tmpDestDimsV1;
	Vector_N & Pk_vK = tmpDestDimsV2;
	Vector_N & PjInvTdiag = tmpDestDimsV2;
	Vector_N & PkInvTdiag = tmpDestDimsV2;

	vJ.noalias() = prototype[J] - trainPoint;
	vK.noalias() = prototype[K] - trainPoint;

	Pj_vJ.noalias() =P[J] * vJ;
	Pk_vK.noalias() = P[K] * vK;

	PjInvTdiag = P[J].diagonal().cwiseInverse();
	PkInvTdiag = P[K].diagonal().cwiseInverse();

	P[J].triangularView<Eigen::Upper>() += (lr_P * muJ2_alt) * (vJ * Pj_vJ.transpose());
	P[J].diagonal() -= (lr_P * muJ2_alt) * PjInvTdiag;

	P[K].triangularView<Eigen::Upper>() += (lr_bad*lr_P*muK2) * (vK * Pk_vK.transpose()) ;
	P[K].diagonal() -= (lr_bad*lr_P*muK2) * PkInvTdiag;

	pBias(J) = ComputeBias(P[J]);
	pBias(K) = ComputeBias(P[K]);

	prototype[J].noalias() += P[J].transpose()* ((lr_point * muJ2_alt) * Pj_vJ);
	prototype[K].noalias() += P[K].transpose() * ((lr_bad * lr_point * muK2) * Pk_vK) ;


	totalMuJLr -= 0.5* lr_point * muJ2_alt;
	totalMuKLr -= lr_point * retval.muK;

	return retval;
}

size_t NormalLvqModel::MemAllocEstimate() const {
	return 
		sizeof(NormalLvqModel)  + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		(sizeof(double) * P[0].size() + sizeof(Matrix_NN)) * P.size() + //dyn alloc prototype transforms
		sizeof(double) * (tmpSrcDimsV1.size() *2 + tmpDestDimsV1.size()*4) + //various vector temps
		(sizeof(Vector_N) + sizeof(double)*prototype[0].size()) *prototype.size() +//dyn alloc prototypes
		(16/2) * (6+prototype.size()*2);//estimate for alignment mucking.
}

void NormalLvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const {
	LvqModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Projection Norm Maximum!norm!Prototype Matrix");
	retval.push_back(L"Projection Norm Mean!norm!Prototype Matrix");
	retval.push_back(L"Projection Norm Minimum!norm!Prototype Matrix");

	retval.push_back(L"Prototype bias max!bias!Prototype bias");
	retval.push_back(L"Prototype bias mean!bias!Prototype bias");
	retval.push_back(L"Prototype bias min!bias!Prototype bias");

	retval.push_back(L"Cumulative \u03BC-J-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
	retval.push_back(L"Cumulative \u03BC-K-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
}

void NormalLvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const {
	LvqModel::AppendOtherStats(stats,trainingSet,testSet);
	MeanMinMax norm, bias;

	for(size_t i=0;i<P.size();++i) {
		norm.Add(P[i].squaredNorm());
		bias.Add(pBias(i));
	}

	stats.push_back(norm.max());
	stats.push_back(norm.mean());
	stats.push_back(norm.min());

	stats.push_back(bias.max());
	stats.push_back(bias.mean());
	stats.push_back(bias.min());

	stats.push_back(totalMuJLr);
	stats.push_back(totalMuKLr);
}

vector<int> NormalLvqModel::GetPrototypeLabels() const {
	vector<int> retval(prototype.size());
	for(unsigned i=0;i<prototype.size();++i)
		retval[i] = pLabel[i];
	return retval;
}


void NormalLvqModel::DoOptionalNormalization() {
	//THIS IS JUST BAD for GGM (+NormalLvqModel); we normalize each iter.
}

Matrix_NN NormalLvqModel::PrototypeDistances(Matrix_NN const & points) const {
	Matrix_NN tmpPointsDiff(points.rows(), points.cols())
		, tmpPointsDiffProj(P[0].rows(), points.cols());
	Matrix_NN newPoints(prototype.size(), points.cols());
	for(size_t protoI=0; protoI<prototype.size(); ++protoI) {
		tmpPointsDiff.noalias() = points.colwise() - prototype[protoI];
		tmpPointsDiffProj.noalias() = P[protoI] * tmpPointsDiff;
		newPoints.row(protoI).noalias() = tmpPointsDiffProj.colwise().squaredNorm();
	}
	return newPoints;
}

Matrix_NN NormalLvqModel::GetCombinedTransforms() const{
	size_t totalRows = std::accumulate(P.cbegin(),P.cend(),size_t(0), [] (size_t val, Matrix_NN const & p) { return val + p.rows(); });
	Matrix_NN retval(totalRows,P[0].cols());
	size_t rowInit=0;
	std::for_each(P.cbegin(),P.cend(),[&rowInit,&retval] (Matrix_NN const & p) {
		retval.middleRows(rowInit, p.rows()).noalias() = p;
		rowInit += p.rows();
	});
	return retval;
}
