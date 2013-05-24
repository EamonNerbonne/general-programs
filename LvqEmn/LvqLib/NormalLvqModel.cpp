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

#ifdef NDEBUG
#define DBGN(X) 
#else
#define DBGN(X) (std::cout<< #X <<":\n"<<(X)<<"\n\n")
#endif



double ComputeBias(Matrix_NN const & mat) {
	return - 2*log(fabs(mat.diagonal().prod())); //mat.diagonal().prod() == mat.determinant() due to upper triangular B.
}

NormalLvqModel::NormalLvqModel(LvqModelSettings & initSettings)
	: LvqModel(initSettings)
	, totalMuJLr(0.0)
	, totalMuKLr(0.0)
	, sumUpdateSize(0.0)
	, updatesOverOne(0)
	, tmpSrcDimsV1(initSettings.InputDimensions())
	, tmpSrcDimsV2(initSettings.InputDimensions())
	, tmpSrcDimsV3(initSettings.InputDimensions())
	, tmpSrcDimsV4(initSettings.InputDimensions())
	, tmpDestDimsV1()
	, tmpDestDimsV2()
	, tmpDestDimsV3()
	, tmpDestDimsV4()
{
	using namespace std;
	initSettings.Dimensionality = (int) initSettings.InputDimensions();

	tmpDestDimsV1.resize(initSettings.Dimensionality);
	tmpDestDimsV2.resize(initSettings.Dimensionality);
	tmpDestDimsV3.resize(initSettings.Dimensionality);
	tmpDestDimsV4.resize(initSettings.Dimensionality);

	initSettings.AssertModelIsOfRightType(this);
	auto InitProtos = initSettings.InitProjectionProtosBySetting();
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

		P[protoIndex].resizeLike(initP[protoIndex] );
		P[protoIndex] = initP[protoIndex];

		pBias(protoIndex) = ComputeBias(P[protoIndex]);
	}
}

inline bool hasNoMuOffset(double trainIter, double scale) { return trainIter*scale >= 65.0; }
inline double scaleMuOffset(double trainIter, double scale) { return sqr(sqr(sqr(1.0 - trainIter*scale/65.0))); }

MatchQuality NormalLvqModel::learnFrom(Vector_N const & trainPoint, int trainLabel) {
	using namespace std;
	GoodBadMatch matches = findMatches(trainPoint, trainLabel);

	double learningRate = stepLearningRate(matches.matchGood);
	double lr_point = -settings.LR0 * learningRate,
		lr_P = lr_point * settings.LrScaleP,
		lr_bad = (settings.SlowK  ?  sqr(1.0 - learningRate)  :  1.0) * settings.LrScaleBad;

	assert(lr_P<=0 && lr_point<=0 && lr_bad>=0);

	//now matches.good is "J" and matches.bad is "K".
	int J = matches.matchGood, K = matches.matchBad;
	MatchQuality retval = matches.GgmQuality();
#if (1==1)
	double muJ2 = 2*retval.muJ;
	assert(muJ2 >=0 );

	double muJ2_alt = settings.MuOffset == 0 || hasNoMuOffset(trainIter, iterationScaleFactor) ? muJ2 : muJ2 + settings.MuOffset *scaleMuOffset(trainIter,iterationScaleFactor);//  * exp(-0.5*retval.distGood);
	assert(muJ2_alt >=0 );
	double muK2 = 2*retval.muK;
	assert(muK2 <=0 );
#else
	double muJ2_alt = settings.MuOffset ;// * exp(-0.5*retval.distGood);
	double muK2 = 0.0;//settings.MuOffset * exp(-0.5*retval.distGood);
#endif

	Vector_N & vJ = tmpSrcDimsV1;
	Vector_N & vK = tmpSrcDimsV2;
	Vector_N & dpJ = tmpSrcDimsV3;
	Vector_N & dpK = tmpSrcDimsV4;
	Vector_N & Pj_vJ = tmpDestDimsV1;
	Vector_N & Pk_vK = tmpDestDimsV2;
	Vector_N & PjInvTdiag = tmpDestDimsV3;
	Vector_N & PkInvTdiag = tmpDestDimsV4;

	vJ.noalias() = prototype[J] - trainPoint;
	vK.noalias() = prototype[K] - trainPoint;


	Pj_vJ.noalias() =P[J] * vJ;
	Pk_vK.noalias() = P[K] * vK;

	PjInvTdiag.noalias() = P[J].diagonal().cwiseInverse();
	PkInvTdiag.noalias() = P[K].diagonal().cwiseInverse();

	dpJ.noalias() = P[J].transpose()* Pj_vJ;
	dpK.noalias() = P[K].transpose() *  Pk_vK;

	LvqFloat vJ_abssum = vJ.cwiseAbs().sum();
	LvqFloat vK_abssum = vK.cwiseAbs().sum();
	LvqFloat Pj_vJ_abssum = Pj_vJ.cwiseAbs().sum();
	LvqFloat Pk_vK_abssum = Pk_vK.cwiseAbs().sum();
	LvqFloat dpJ_abssum = dpJ.cwiseAbs().sum();
	LvqFloat dpK_abssum = dpK.cwiseAbs().sum();

	LvqFloat updateSizeJ = 
		fabs(muJ2_alt * dpJ_abssum * lr_point  
		+muJ2_alt * (
		//dpJ_abssum * lr_point  // change due to pJ
		+ Pj_vJ_abssum * vJ_abssum * lr_P  //change due to P_J
		+PjInvTdiag.cwiseAbs().sum() * lr_P )
		);

	LvqFloat updateSizeK = 
		fabs(muK2 * lr_bad * (
		dpK_abssum *  lr_point // change due to pK
		+Pk_vK_abssum * vK_abssum * lr_P //change due to P_J
		+PkInvTdiag.cwiseAbs().sum() *lr_P
		)
		);
	LvqFloat updateSize = updateSizeJ + updateSizeK;

	if(updateSize > 1.0) {
		//cout<< trainIter<<": "<<updateSize<<"!\n";
		muJ2_alt/= updateSize;
		muK2 /= updateSize;
		updatesOverOne++;
		sumUpdateSize += 1.0;
	} else 
		sumUpdateSize += updateSize;
	prototype[J].noalias() += (lr_point * muJ2_alt) * dpJ;// P[J].transpose()* ((lr_point * muJ2_alt) * Pj_vJ);
	prototype[K].noalias() += (lr_bad * lr_point * muK2) * dpK;// P[K].transpose() * ((lr_bad * lr_point * muK2) * Pk_vK) ;
#ifndef NDEBUG
	if(!isfinite_emn(prototype[J].norm())) {
		DBGN(prototype[J]);
		DBGN(P[J]);
		DBGN(Pj_vJ);
	}
	if(!isfinite_emn(prototype[K].norm())) {
		DBGN(prototype[K]);
		DBGN(P[K]);
		DBGN(Pj_vJ);
	}

#endif

	P[J].triangularView<Eigen::Upper>() += (lr_P * muJ2_alt)* (Pj_vJ * vJ.transpose());

	//P[J].selfadjointView<Eigen::Upper>().rankUpdate(Pj_vJ ,vJ, lr_P * muJ2_alt);

	P[J].diagonal().noalias() -= (lr_P * muJ2_alt) * PjInvTdiag;

	P[K].triangularView<Eigen::Upper>() += (lr_bad*lr_P*muK2) * (Pk_vK * vK.transpose()) ;
	P[K].diagonal().noalias() -= (lr_bad*lr_P*muK2) * PkInvTdiag;

	pBias(J) = ComputeBias(P[J]);
	pBias(K) = ComputeBias(P[K]);

	totalMuJLr -= 0.5* lr_point * muJ2_alt;
	totalMuKLr += 0.5 * lr_point * muK2;

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

	retval.push_back(L"Cumulative update size!!Cumulative update size");
	retval.push_back(L"Updates over 1!!Updates over 1");

	retval.push_back(L"mu offset scaling!!MuJ Offset");
	retval.push_back(L"base learning rate!!Learning Rate");
	retval.push_back(L"Mean update size!!Mean update size");


	for(size_t i=0;i<prototype.size();++i) {
		wstring name =wstring( L"#pos-norm ") + to_wstring(pLabel(i));
		retval.push_back(name+ L"!pos-norm!Per-prototype: ||w||");
	}
	for(size_t i=0;i<prototype.size();++i) {
		wstring name =wstring( L"#bias ") + to_wstring(pLabel(i));
		retval.push_back(name+ L"!bias!Per-prototype: -log(|P|^2)");
	};

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

	//if(trainIter > lastStatIter) 
	stats.push_back(sumUpdateSize );// / (trainIter - lastStatIter)
	stats.push_back(updatesOverOne );// / (trainIter - lastStatIter)
	stats.push_back(hasNoMuOffset(trainIter, iterationScaleFactor)?0.0:scaleMuOffset(trainIter, iterationScaleFactor) );// / (trainIter - lastStatIter)
	stats.push_back(meanUnscaledLearningRate() );// / (trainIter - lastStatIter)

	stats.push_back(sumUpdateSize / trainIter);
	//	else
	//stats.push_back(numeric_limits<double>::quiet_NaN());
	//logSumUpdateSize = 0.0;
	//lastStatIter=trainIter;

	for(size_t i=0;i<prototype.size();++i) {
		stats.push_back(prototype[i].norm());
	};
	for(size_t i=0;i<prototype.size();++i) {
		stats.push_back(pBias(i));
	};
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
