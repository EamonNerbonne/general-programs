#include "stdafx.h"
#include "GmmLvqModel.h"
#include "utils.h"
#include "RandomMatrix.h"
#include "LvqConstants.h"
#include "MeanMinMax.h"
#include "LvqDataset.h"
#include "PCA.h"
using namespace std;
using namespace Eigen;

GmmLvqModel::GmmLvqModel(LvqModelSettings & initSettings)
	: LvqProjectionModelBase(initSettings)
	, m_vJ(initSettings.Dimensions())
	, m_vK(initSettings.Dimensions())
	, m_PpseudoinvT(LVQ_LOW_DIM_SPACE,initSettings.Dimensions())
{
	if(initSettings.Dimensionality!=0 && initSettings.Dimensionality!=2)
		throw "Illegal Dimensionality";
	using namespace std;
	initSettings.AssertModelIsOfRightType(this);
	Vector2d eigVal;
	Matrix2d pca2d;
	PcaLowDim::DoPca(P * initSettings.Dataset->ExtractPoints(initSettings.Trainingset),pca2d,eigVal);
	Matrix2d toUnitDist=eigVal.cwiseSqrt().asDiagonal();

	int protoCount = accumulate(initSettings.PrototypeDistribution.begin(),initSettings.PrototypeDistribution.end(),0);
	prototype.resize(protoCount);

	int maxProtoCount=0;
	int protoIndex=0;
	auto PerClassMeans = initSettings.PerClassMeans();
	for(int label=0; label <(int) initSettings.PrototypeDistribution.size();label++) {
		int labelCount =initSettings.PrototypeDistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = GmmLvqPrototype(initSettings.RngParams, initSettings.RandomInitialBorders, label, PerClassMeans.col(label), P, toUnitDist);
			protoIndex++;
		}
		maxProtoCount = max(maxProtoCount, labelCount);
	}

	if(initSettings.NgUpdateProtos && maxProtoCount>1) 
		ngMatchCache.resize(maxProtoCount);//otherwise size 0!

	assert(accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0)== protoIndex);
}

typedef Map<VectorXd, Aligned> MVectorXd;


MatchQuality GmmLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	using namespace std;
	double learningRate = stepLearningRate();

	double lr_point = settings.LR0 * learningRate,
		lr_P = lr_point * settings.LrScaleP,
		lr_B = lr_point * settings.LrScaleB*(1.0-learningRate); 
	double lr_bad_scale = settings.LrScaleBad*(1.0-learningRate);

	assert(lr_P>=0 && lr_B>=0 && lr_point>=0);

	Vector2d P_trainPoint( P * trainPoint );

	CorrectAndWorstMatches fullmatch(0);
	GoodBadMatch matches;

	if(ngMatchCache.size()>0) {
		fullmatch = CorrectAndWorstMatches(& (ngMatchCache[0]));
		for(int i=0;i<(int)prototype.size();++i) 
			fullmatch.Register(SqrDistanceTo(i, P_trainPoint),i, PrototypeLabel(i) == trainLabel);
		fullmatch.SortOk();

		matches = fullmatch.ToGoodBadMatch();
	} else {
		matches = findMatches(P_trainPoint, trainLabel);
	}

	//now matches.good is "J" and matches.bad is "K".
	GmmLvqPrototype &J = prototype[matches.matchGood];
	GmmLvqPrototype &K = prototype[matches.matchBad];
	double muJ2 = 2*matches.MuGmm();
	double muK2 = 2*matches.MuGmm();

	if(!isfinite(muJ2) || !isfinite(muK2)) {
		DBG(matches.matchBad);
		DBG(matches.matchGood);

		DBG(matches.distBad);
		DBG(matches.distGood);

		DBG(muJ2);
		DBG(muK2);

		DBG(prototype[matches.matchBad].bias);
		DBG(prototype[matches.matchGood].bias);

		DBG(prototype[matches.matchBad].B);
		DBG(prototype[matches.matchGood].B);

		DBG(prototype[matches.matchBad].B.determinant());
		DBG(prototype[matches.matchGood].B.determinant());

		muJ2 = 0.0;
		muK2 = 0.0;
	} else {
		MVectorXd vJ(m_vJ.data(),m_vJ.size());
		MVectorXd vK(m_vK.data(),m_vK.size());

		Vector2d P_vJ= J.P_point - P_trainPoint;
		Vector2d P_vK = K.P_point - P_trainPoint;
		Vector2d muJ2_Bj_P_vJ = muJ2 *  (J.B * P_vJ) ;
		Vector2d muK2_Bk_P_vK = muK2 *  (K.B * P_vK) ;
		vJ = J.point - trainPoint;
		vK = K.point - trainPoint;
		Vector2d muJ2_BjT_Bj_P_vJ =  J.B.transpose() * muJ2_Bj_P_vJ ;
		Vector2d muK2_BkT_Bk_P_vK = K.B.transpose() * muK2_Bk_P_vK ;

#ifdef AUTO_BIAS
		Matrix2d muJ2_JBinvT = muJ2* J.B.inverse().transpose();
		Matrix2d muK2_KBinvT = muK2* K.B.inverse().transpose();

		J.B.noalias() -= lr_B * (muJ2_Bj_P_vJ * P_vJ.transpose() - muJ2_JBinvT );
		K.B.noalias() += (lr_bad_scale*lr_B) * (muK2_Bk_P_vK * P_vK.transpose() - muK2_KBinvT) ;
		J.RecomputeBias();
		K.RecomputeBias();
#else
		J.B.noalias() -= lr_B * muJ2_Bj_P_vJ * P_vJ.transpose() ;
		K.B.noalias() += lr_B * muK2_Bk_P_vK * P_vK.transpose() ;
		//J.bias -= mu2*lr_B;
		//K.bias += mu2*lr_B;
#endif


		J.point.noalias() -= P.transpose()* (lr_point * muJ2_BjT_Bj_P_vJ);
		K.point.noalias() += P.transpose() * (lr_bad_scale * lr_point * muK2_BkT_Bk_P_vK) ;

		//Matrix2d PPTinv = (P* P.transpose()).inverse();
		//m_PpseudoinvT.noalias() = (P.transpose() * (lr_P *(-muK2-muJ2) * PPTinv)).transpose();
		P.noalias() += (lr_P * muK2_BkT_Bk_P_vK) * vK.transpose() - (lr_P * muJ2_BjT_Bj_P_vJ) * vJ.transpose();//+ m_PpseudoinvT;

		//double pBias = -log((P* P.transpose()).determinant());


		if(ngMatchCache.size()>0) {
			double lrSub = lr_point;
			double lrDelta = exp(-LVQ_NG_FACTOR/learningRate);//TODO: this is rather ADHOC
			for(int i=1;i<fullmatch.foundOk;++i) {
				lrSub*=lrDelta;
				GmmLvqPrototype &Js = prototype[fullmatch.matchesOk[i].idx];
				double pMargin_s = exp(-fabs(fullmatch.distBad - fullmatch.matchesOk[i].dist));
				double muJ2_s = 2*2* pMargin_s /((1 + pMargin_s)*(1+pMargin_s));
				Vector2d P_vJs = Js.P_point - P_trainPoint;
				Vector2d muJ2_Bj_P_vJs = muJ2 * (Js.B * P_vJs);

				Js.point.noalias() -= P.transpose() * (lrSub * (Js.B.transpose() * muJ2_Bj_P_vJs));
#ifdef AUTO_BIAS
				Matrix2d muJ2_JBinvTs = muJ2_s* Js.B.inverse().transpose();

				Js.B.noalias() -= (lrSub*lr_B) * (muJ2_Bj_P_vJs * P_vJs.transpose() - muJ2_JBinvTs);
#else
				Js.B.noalias() -= lrSub*lr_B * muJ2_Bj_P_vJs * P_vJs.transpose() ;
#endif
			}
		}

		for(size_t i=0;i<prototype.size();++i)
			prototype[i].ComputePP(P);
	}
	return matches.GmmQuality();
}


LvqModel* GmmLvqModel::clone() const { return new GmmLvqModel(*this); }

size_t GmmLvqModel::MemAllocEstimate() const {
	return 
		sizeof(GmmLvqModel) +
		sizeof(CorrectAndWorstMatches::MatchOk) * ngMatchCache.size()+
		sizeof(double) * P.size() +
		sizeof(double) * (m_vJ.size() +m_vK.size()) + //various temps
		sizeof(GmmLvqPrototype)*prototype.size() + //prototypes; part statically allocated
		sizeof(double) * (prototype.size() * m_vJ.size()) + //prototypes; part dynamically allocated
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
}

MatrixXd GmmLvqModel::GetProjectedPrototypes() const {
	MatrixXd retval(LVQ_LOW_DIM_SPACE, static_cast<int>(prototype.size()));
	for(unsigned i=0;i<prototype.size();++i)
		retval.col(i) = prototype[i].projectedPosition();
	return retval;
}

vector<int> GmmLvqModel::GetPrototypeLabels() const {
	vector<int> retval(prototype.size());
	for(unsigned i=0;i<prototype.size();++i)
		retval[i] = prototype[i].label();
	return retval;
}

void GmmLvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const {
	LvqProjectionModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Border matrix norm min|norm|Border Matrix");
	retval.push_back(L"Border matrix norm mean|norm|Border Matrix");
	retval.push_back(L"Border matrix norm max|norm|Border Matrix");
	retval.push_back(L"Prototype bias min|bias|Prototype bias");
	retval.push_back(L"Prototype bias mean|bias|Prototype bias");
	retval.push_back(L"Prototype bias max|bias|Prototype bias");
}
void GmmLvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const {
	LvqProjectionModel::AppendOtherStats(stats,trainingSet,trainingSubset,testSet,testSubset);
	MeanMinMax norm, bias;
	std::for_each(prototype.begin(),prototype.end(), [&](GmmLvqPrototype const & proto) {
		norm.Add(projectionSquareNorm( proto.B));
		bias.Add(proto.bias);
	});

	stats.push_back(norm.min());
	stats.push_back(norm.mean());
	stats.push_back(norm.max());
	stats.push_back(bias.min());
	stats.push_back(bias.mean());
	stats.push_back(bias.max());
}


void GmmLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const {
	int cols = static_cast<int>(classDiagram.cols());
	int rows = static_cast<int>(classDiagram.rows());
	double xDelta = (x1-x0) / cols;
	double yDelta = (y1-y0) / rows;
	double xBase = x0+xDelta*0.5;
	double yBase = y0+yDelta*0.5;

	PMatrix B_diff_x0_y(LVQ_LOW_DIM_SPACE,PrototypeCount()); //Contains B_i * (testPoint[x, y0] - P*proto_i)  for all proto's i
	//will update to include changes to X.
	PMatrix B_xDelta(LVQ_LOW_DIM_SPACE,PrototypeCount());//Contains B_i * (xDelta, 0)  for all proto's i
	PMatrix B_yDelta(LVQ_LOW_DIM_SPACE,PrototypeCount());//Contains B_i * (0 , yDelta)  for all proto's i
	VectorXd pBias(PrototypeCount());//Contains prototype[i].bias for all proto's i

	for(int pi=0; pi < this->PrototypeCount(); ++pi) {
		auto & current_proto = this->prototype[pi];
		B_diff_x0_y.col(pi).noalias() = current_proto.B * ( Vector2d(xBase,yBase) - current_proto.P_point);
		B_xDelta.col(pi).noalias() = current_proto.B * Vector2d(xDelta,0.0);
		B_yDelta.col(pi).noalias() = current_proto.B * Vector2d(0.0,yDelta);
		pBias(pi) = current_proto.bias;
	}

	for(int yRow=0; yRow < rows; yRow++) {
		PMatrix B_diff_x_y(B_diff_x0_y); //copy that includes changes to X as well.
		for(int xCol=0; xCol < cols; xCol++) {
			// x = xBase + xCol * xDelta;  y = yBase + yCol * yDelta;
			MatrixXd::Index bestProtoI;
			(B_diff_x_y.colwise().squaredNorm() + pBias.transpose()).minCoeff(&bestProtoI);
			classDiagram(yRow, xCol) = this->prototype[bestProtoI].classLabel;

			B_diff_x_y.noalias() += B_xDelta;
		}
		B_diff_x0_y.noalias() += B_yDelta;
	}
}


void GmmLvqModel::DoOptionalNormalization() {
	if(settings.NormalizeProjection) {
		normalizeProjection(P);
		for(size_t i=0;i<prototype.size();++i)
			prototype[i].ComputePP(P);
	}

	if(settings.NormalizeBoundaries) {
		if(settings.GloballyNormalize) {
			double overallNorm = std::accumulate(prototype.begin(), prototype.end(),0.0,
				[](double cur, GmmLvqPrototype const & proto) -> double { return cur + projectionSquareNorm(proto.B); } 
			// (cur, proto) => cur + projectionSquareNorm(proto.B)
			);
			double scale = 1.0/sqrt(overallNorm / prototype.size());
			for(size_t i=0;i<prototype.size();++i) prototype[i].B*=scale;
		} else {
			for(size_t i=0;i<prototype.size();++i) normalizeProjection(prototype[i].B);
		}
#ifdef AUTO_BIAS
		for(size_t i=0;i<prototype.size();++i) prototype[i].RecomputeBias();
#endif
	}
}

GmmLvqPrototype::GmmLvqPrototype() : classLabel(-1) {}

GmmLvqPrototype::GmmLvqPrototype(boost::mt19937 & rng, bool randInit, int protoLabel, VectorXd const & initialVal,PMatrix const & P, Matrix2d const & scaleB) 
	: point(initialVal) 
	, classLabel(protoLabel)
	, bias(0.0)
{
	B = scaleB;
	if(randInit)
		B = randomUnscalingMatrix<Matrix2d>(rng, LVQ_LOW_DIM_SPACE) * scaleB;	
	else 
		B = scaleB;
	ComputePP(P);
#ifdef AUTO_BIAS
	RecomputeBias();
#endif
}
