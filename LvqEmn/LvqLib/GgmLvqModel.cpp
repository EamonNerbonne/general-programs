#include "stdafx.h"
#include "GgmLvqModel.h"
#include "utils.h"
#include "RandomMatrix.h"
#include "LvqConstants.h"
#include "MeanMinMax.h"
#include "LvqDataset.h"
#include "PCA.h"
#include "CovarianceAndMean.h"
using namespace std;
using namespace Eigen;

GgmLvqModel::GgmLvqModel(LvqModelSettings & initSettings)
	: LvqProjectionModelBase(initSettings)
	, totalMuLr(0.0)
	, lastAutoPupdate(0.0)
	, m_vJ(initSettings.Dimensions())
	, m_vK(initSettings.Dimensions())
	, m_PpseudoinvT(LVQ_LOW_DIM_SPACE,initSettings.Dimensions())
{
	if(initSettings.Dimensionality!=0 && initSettings.Dimensionality!=2){
		std::cerr<< "Illegal Dimensionality\n";
		std::exit(10);
	}
	using namespace std;
	initSettings.AssertModelIsOfRightType(this);

	auto InitProtos = initSettings.InitProtosProjectionBoundariesBySetting();
	P = get<0>(InitProtos);
	Matrix_NN prototypes = get<1>(InitProtos);
	VectorXi protoLabels = get<2>(InitProtos);

	vector<Matrix_22> initB = get<3>(InitProtos);

	prototype.resize(protoLabels.size());
	for(size_t protoIndex=0; protoIndex < (size_t)protoLabels.size(); ++protoIndex) {
		prototype[protoIndex] = 	GgmLvqPrototype(initSettings.RngParams, initSettings.RandomInitialBorders, protoLabels(protoIndex), prototypes.col(protoIndex), P, initB[protoIndex]);
	}

	int maxProtoCount = accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0, [](int a, int b) -> int { return max(a,b); });

	if(initSettings.NGu && maxProtoCount>1) 
		ngMatchCache.resize(maxProtoCount);//otherwise size 0!

	if(settings.scP) {
		Matrix_2N pPoints = initSettings.Dataset->ProjectPoints(*this);
		VectorXi const & label = initSettings.Dataset->getPointLabels();
		double sumLogScale=0.0;
		for(ptrdiff_t pI = 0; pI<label.size();++pI) {
			auto matches = findMatches(pPoints.col(pI), label(pI));
			double logScale = log((prototype[matches.matchGood].P_point -pPoints.col(pI)).squaredNorm() + (prototype[matches.matchBad].P_point -pPoints.col(pI)).squaredNorm());
			sumLogScale+=logScale;
		}
		double meanLogScale = sumLogScale / label.size();
		/*
		if not zero, we need to "subtract" this mean out from each match
			so E(ln(dJ+dk)) -= mean
			so each ln(dJ+dk) -= mean
			so each dJ+dK /= exp(mean)
			so each dJ+dK *= exp(-mean)
			so P^2*[...] *= exp(-mean);
			so P *= exp(-mean/2)

		*/
		//
		lastAutoPupdate = -0.5 * meanLogScale;
		P *= exp(lastAutoPupdate);
		for(size_t i=0;i<prototype.size();++i)
			prototype[i].ComputePP(P);
	}
}

typedef Map<Vector_N, Aligned> MVectorXd;

MatchQuality GgmLvqModel::learnFrom(Vector_N const & trainPoint, int trainLabel) {
	using namespace std;
	const size_t protoCount = prototype.size();

	Vector_2 P_trainPoint( P * trainPoint );

	CorrectAndWorstMatches fullmatch(0);
	GoodBadMatch matches;

	if(ngMatchCache.size()>0) {
		fullmatch = CorrectAndWorstMatches(& (ngMatchCache[0]));
		for(size_t i=0;i<protoCount;++i) 
			fullmatch.Register(SqrDistanceTo(i, P_trainPoint),(int)i, PrototypeLabel(i) == trainLabel);
		fullmatch.SortOk();

		matches = fullmatch.ToGoodBadMatch();
	} else {
		matches = findMatches(P_trainPoint, trainLabel);
	}

	double learningRate = stepLearningRate(matches.matchGood);
	double lr_point = -settings.LR0 * learningRate,
		lr_P = lr_point * settings.LrScaleP,
		lr_B = lr_point * settings.LrScaleB,// * (1.0 - learningRate),
		lr_bad = (settings.SlowK  ?  sqr(1.0 - learningRate)  :  1.0) * settings.LrScaleBad;

	assert(lr_P<=0 && lr_B<=0 && lr_point<=0);

	//now matches.good is "J" and matches.bad is "K".
	GgmLvqPrototype &J = prototype[matches.matchGood];
	GgmLvqPrototype &K = prototype[matches.matchBad];
	MatchQuality ggmQuality = matches.GgmQuality();
	double muJ2 = 2*ggmQuality.muJ;
	double muK2 = 2*ggmQuality.muK;
	double muJ2_alt = muJ2 + settings.MuOffset * learningRate;

	MVectorXd vJ(m_vJ.data(),m_vJ.size());
	MVectorXd vK(m_vK.data(),m_vK.size());

	Vector_2 P_vJ= J.P_point - P_trainPoint;
	Vector_2 P_vK = K.P_point - P_trainPoint;
	Vector_2 Bj_P_vJ = J.B * P_vJ;
	Vector_2 Bk_P_vK = K.B * P_vK;
	vJ = J.point - trainPoint;
	vK = K.point - trainPoint;
	Vector_2 BjT_Bj_P_vJ =  J.B.transpose() * Bj_P_vJ ;
	Vector_2 BkT_Bk_P_vK = K.B.transpose() * Bk_P_vK ;

	Matrix_22 JBinvT =  J.B.inverse().transpose();
	Matrix_22 KBinvT =  K.B.inverse().transpose();

	if(settings.scP) {
		double logScale = log((prototype[matches.matchGood].P_point -P_trainPoint).squaredNorm() + (prototype[matches.matchBad].P_point - P_trainPoint).squaredNorm());
		lastAutoPupdate = LVQ_AutoScaleP_Momentum * lastAutoPupdate - logScale;
	}


	J.B.noalias() += (lr_B * (muJ2_alt)) * (Bj_P_vJ * P_vJ.transpose() - JBinvT );
	K.B.noalias() += (lr_bad*lr_B*muK2) * (Bk_P_vK * P_vK.transpose() - KBinvT) ;
	J.RecomputeBias();
	K.RecomputeBias();

	J.point.noalias() += P.transpose()* ((lr_point * (muJ2_alt)) * BjT_Bj_P_vJ);
	K.point.noalias() += P.transpose() * ((lr_bad * lr_point * muK2) * BkT_Bk_P_vK) ;

	if(ngMatchCache.size()>0) {
		double lrSub = 1.0;
		double lrDelta = exp(-LVQ_NG_FACTOR/learningRate);//this is rather ad hoc
		for(int i=1;i<fullmatch.foundOk;++i) {
			lrSub*=lrDelta;
			GgmLvqPrototype &Js = prototype[fullmatch.matchesOk[i].idx];
			double muJ2_s =  (1.0/4.0) * (1.0 - sqr(std::tanh((fullmatch.matchesOk[i].dist - fullmatch.distBad)/4.0)));
			Vector_2 P_vJs = Js.P_point - P_trainPoint;
			Vector_2 muJ2_Bj_P_vJs = (muJ2_s + settings.MuOffset*learningRate) * (Js.B * P_vJs);

			Js.point.noalias() += P.transpose() * (lrSub * lr_point * (Js.B.transpose() * muJ2_Bj_P_vJs));
			Matrix_22 neg_muJ2_JBinvTs = -muJ2_s* Js.B.inverse().transpose();

			Js.B.noalias() += lrSub*lr_B * (muJ2_Bj_P_vJs * P_vJs.transpose() + neg_muJ2_JBinvTs);
		}
	}
	
	if(settings.scP) {
		P *= exp(lastAutoPupdate*4*learningRate*LVQ_AutoScaleP_Lr);
	}


	if(settings.noKP) {
		P.noalias() += ((lr_P * muJ2) * BjT_Bj_P_vJ) * vJ.transpose();
	} else {
		P.noalias() += ((lr_P * muK2) * BkT_Bk_P_vK) * vK.transpose() + ((lr_P * muJ2) * BjT_Bj_P_vJ) * vJ.transpose();
	}

	if(!settings.scP)
		normalizeProjection(P);

	for(size_t i=0;i < protoCount;++i)
		prototype[i].ComputePP(P);
	
	totalMuLr+= -lr_point*ggmQuality.muJ;
	return ggmQuality;
}


LvqModel* GgmLvqModel::clone() const { return new GgmLvqModel(*this); }

size_t GgmLvqModel::MemAllocEstimate() const {
	return 
		sizeof(GgmLvqModel) +
		sizeof(CorrectAndWorstMatches::MatchOk) * ngMatchCache.size()+
		sizeof(double) * P.size() +
		sizeof(double) * (m_vJ.size() +m_vK.size()) + //various temps
		sizeof(GgmLvqPrototype)*prototype.size() + //prototypes; part statically allocated
		sizeof(double) * (prototype.size() * m_vJ.size()) + //prototypes; part dynamically allocated
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
}

Matrix_2N GgmLvqModel::GetProjectedPrototypes() const {
	Matrix_2N retval(LVQ_LOW_DIM_SPACE, static_cast<int>(prototype.size()));
	for(unsigned i=0;i<prototype.size();++i)
		retval.col(i) = prototype[i].projectedPosition();
	return retval;
}

vector<int> GgmLvqModel::GetPrototypeLabels() const {
	vector<int> retval(prototype.size());
	for(unsigned i=0;i<prototype.size();++i)
		retval[i] = prototype[i].label();
	return retval;
}

void GgmLvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const {
	LvqProjectionModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Maximum norm(B)!norm!Border Matrix norm");
	retval.push_back(L"Mean norm(B)!norm!Border Matrix norm");
	retval.push_back(L"Minimum norm(B)!norm!Border Matrix norm");
	retval.push_back(L"Maximum |B|!determinant!Border Matrix absolute determinant");
	retval.push_back(L"Mean |B|!determinant!Border Matrix absolute determinant");
	retval.push_back(L"Minimum |B|!determinant!Border Matrix absolute determinant");
	retval.push_back(L"Prototype bias max!bias!Prototype bias");
	retval.push_back(L"Prototype bias mean!bias!Prototype bias");
	retval.push_back(L"Prototype bias min!bias!Prototype bias");
	retval.push_back(L"Cumulative \u03BC-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
}
void GgmLvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const {
	LvqProjectionModel::AppendOtherStats(stats,trainingSet,testSet);
	MeanMinMax norm, det, bias;
	std::for_each(prototype.begin(),prototype.end(), [&](GgmLvqPrototype const & proto) {
		norm.Add(proto.B.squaredNorm());
		det.Add(abs(proto.B.determinant()));
		bias.Add(proto.bias);
	});

	stats.push_back(norm.max());
	stats.push_back(norm.mean());
	stats.push_back(norm.min());

	stats.push_back(det.max());
	stats.push_back(det.mean());
	stats.push_back(det.min());

	stats.push_back(bias.max());
	stats.push_back(bias.mean());
	stats.push_back(bias.min());
	stats.push_back(totalMuLr);
}

void GgmLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const {
	int cols = static_cast<int>(classDiagram.cols());
	int rows = static_cast<int>(classDiagram.rows());
	double xDelta = (x1-x0) / cols;
	double yDelta = (y1-y0) / rows;
	double xBase = x0+xDelta*0.5;
	double yBase = y0+yDelta*0.5;

	Matrix_P B_diff_x0_y(LVQ_LOW_DIM_SPACE,PrototypeCount()); //Contains B_i * (testPoint[x, y0] - P*proto_i)  for all proto's i
	//will update to include changes to X.
	Matrix_P B_xDelta(LVQ_LOW_DIM_SPACE,PrototypeCount());//Contains B_i * (xDelta, 0)  for all proto's i
	Matrix_P B_yDelta(LVQ_LOW_DIM_SPACE,PrototypeCount());//Contains B_i * (0 , yDelta)  for all proto's i
	Vector_N pBias(PrototypeCount());//Contains prototype[i].bias for all proto's i

	for(int pi=0; pi < this->PrototypeCount(); ++pi) {
		auto & current_proto = this->prototype[pi];
		B_diff_x0_y.col(pi).noalias() = current_proto.B * ( Vector_2(xBase,yBase) - current_proto.P_point);
		B_xDelta.col(pi).noalias() = current_proto.B * Vector_2(xDelta,0.0);
		B_yDelta.col(pi).noalias() = current_proto.B * Vector_2(0.0,yDelta);
		pBias(pi) = current_proto.bias;
	}

	for(int yRow=0; yRow < rows; yRow++) {
		Matrix_P B_diff_x_y(B_diff_x0_y); //copy that includes changes to X as well.
		for(int xCol=0; xCol < cols; xCol++) {
			// x = xBase + xCol * xDelta;  y = yBase + yCol * yDelta;
			Matrix_NN::Index bestProtoI;
			(B_diff_x_y.colwise().squaredNorm() + pBias.transpose()).minCoeff(&bestProtoI);
			classDiagram(yRow, xCol) =(unsigned char) this->prototype[bestProtoI].classLabel;

			B_diff_x_y.noalias() += B_xDelta;
		}
		B_diff_x0_y.noalias() += B_yDelta;
	}
}

void GgmLvqModel::DoOptionalNormalization() {
	//THIS IS JUST BAD for GGM; we normalize each iter.
}

 void GgmLvqModel::compensateProjectionUpdate(Matrix_22 U, double /*scale*/) {
	for(size_t i=0;i < prototype.size();++i) {
		prototype[i].B *= U;
		prototype[i].ComputePP(P);
	}
}

GgmLvqPrototype::GgmLvqPrototype() : classLabel(-1) {}

GgmLvqPrototype::GgmLvqPrototype(boost::mt19937 & rng, bool randInit, int protoLabel, Vector_N const & initialVal,Matrix_P const & P, Matrix_22 const & scaleB) 
	: B(scaleB)//randInit?randomUnscalingMatrix<Matrix_22>(rng, LVQ_LOW_DIM_SPACE)*scaleB: 
	, P_point(P*initialVal)
	, classLabel(protoLabel)
	, point(initialVal) 
	, bias(0.0)
{
	auto rndmat = randomUnscalingMatrix<Matrix_22>(rng, LVQ_LOW_DIM_SPACE);
	if(randInit)
		B = rndmat*B;
	RecomputeBias();
}


Matrix_NN GgmLvqModel::PrototypeDistances(Matrix_NN const & points) const {
	Matrix_2N P_points = P*points;
	Matrix_NN newPoints(prototype.size(), points.cols());
	for(size_t protoI=0;protoI<prototype.size();++protoI) {
		newPoints.row(protoI).noalias() = (prototype[protoI].B * (P_points.colwise() - prototype[protoI].P_point)).colwise().squaredNorm();
	}
	return newPoints;
}