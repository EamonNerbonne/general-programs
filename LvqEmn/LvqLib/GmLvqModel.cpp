#include "StdAfx.h"
#include "GmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
using namespace std;

GmLvqModel::GmLvqModel(LvqModelSettings & initSettings)
	: LvqProjectionModelBase(initSettings) 
	, m_vJ(initSettings.Dimensions())
	, m_vK(initSettings.Dimensions()) {
	initSettings.AssertModelIsOfRightType(this);

	auto InitProtos = initSettings.InitProtosBySetting(); 
	pLabel = InitProtos.second;
	size_t protoCount = pLabel.size();
	prototype.resize(protoCount);
	P_prototype.resize(protoCount);

	for(int protoIndex=0; protoIndex < protoCount; ++protoIndex) {
		prototype[protoIndex] = InitProtos.first.col(protoIndex);
		RecomputeProjection(protoIndex);
	}

	int maxProtoCount = accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0, [](int a, int b) -> int { return max(a,b); });

	if(initSettings.NgUpdateProtos && maxProtoCount>1) 
		ngMatchCache.resize(maxProtoCount);//otherwise size 0!
}

MatchQuality GmLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	double learningRate = stepLearningRate();

	double lr_point = settings.LR0 * learningRate,
		lr_P = lr_point * settings.LrScaleP,
		lr_bad = (settings.SlowStartLrBad? 1.0-learningRate : 1.0) * settings.LrScaleBad;


	assert(lr_P>=0 && lr_point>=0);

	Vector2d P_trainPoint(P * trainPoint);

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

	double muK2 = 2.0*matches.MuK();
	double muJ2 = 2.0*matches.MuJ();

	int J = matches.matchGood;
	int K = matches.matchBad;

	Vector2d muJ2_P_vJ(muJ2 * (P_prototype[J] - P_trainPoint) );
	Vector2d muK2_P_vK(muK2 * (P_prototype[K] - P_trainPoint) );

	auto vJ(VectorXd::MapAligned(m_vJ.data(),m_vJ.size()));
	auto vK(VectorXd::MapAligned(m_vK.data(),m_vK.size()));

	vJ = prototype[K] - trainPoint;
	vK = prototype[K] - trainPoint;

	prototype[J].noalias() -= P.transpose() * (lr_point * muJ2_P_vJ);
	prototype[K].noalias() -= P.transpose() * (lr_bad*lr_point *muK2_P_vK);

	P.noalias() -= (lr_P * muJ2_P_vJ) * vJ.transpose() + (lr_P * muK2_P_vK) * vK.transpose();

	if(ngMatchCache.size()>0) {
		double lrSub = lr_point;
		double lrDelta = exp(-LVQ_NG_FACTOR/learningRate);//TODO: this is rather ADHOC
		for(int i=1;i<fullmatch.foundOk;++i) {
			lrSub*=lrDelta;
			VectorXd &Js = prototype[fullmatch.matchesOk[i].idx];
			Vector2d &P_Js = P_prototype[fullmatch.matchesOk[i].idx];;
			double muJ2s_lrSub = lrSub* 2.0 * +2.0*fullmatch.distBad / (sqr(fullmatch.matchesOk[i].dist) + sqr(fullmatch.distBad));
			Js.noalias() -= P.transpose() * (muJ2s_lrSub * (P_Js - P_trainPoint));
		}
	}

	for(int i=0;i<pLabel.size();++i)
		RecomputeProjection(i);
	return matches.LvqQuality();
}

LvqModel* GmLvqModel::clone() const { return new GmLvqModel(*this);	}

MatrixXd GmLvqModel::GetProjectedPrototypes() const {
	MatrixXd retval(LVQ_LOW_DIM_SPACE, static_cast<int>(prototype.size()));
	for(unsigned i=0;i<prototype.size();++i)
		retval.col(i) = P_prototype[i];
	return retval;
}

vector<int> GmLvqModel::GetPrototypeLabels() const {
	vector<int> retval(prototype.size());
	for(unsigned i=0;i<prototype.size();++i)
		retval[i] = pLabel[i];
	return retval;
}

size_t GmLvqModel::MemAllocEstimate() const {
	return 
		sizeof(GmLvqModel) + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		sizeof(double) * (P.size() ) + //dyn alloc transform + temp transform
		sizeof(double) * (m_vJ.size() + m_vK.size()) + //various vector temps
		sizeof(VectorXd) *prototype.size() +//dyn alloc prototype base overhead
		sizeof(double) * (prototype.size() * prototype[0].size()) + //dyn alloc prototype data
		sizeof(Vector2d) * P_prototype.size() + //cache of pretransformed prototypes
		(16/2) * (5+prototype.size()*2);//estimate for alignment mucking.
}

void GmLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const {
	int cols = static_cast<int>(classDiagram.cols());
	int rows = static_cast<int>(classDiagram.rows());
	double xDelta = (x1-x0) / cols;
	double yDelta = (y1-y0) / rows;
	double xBase = x0+xDelta*0.5;
	double yBase = y0+yDelta*0.5;

	PMatrix diff_x0_y(LVQ_LOW_DIM_SPACE,PrototypeCount()); //Contains (testPoint[x, y0] - P*proto_i)  for all proto's i
	//will update to include changes to X.

	for(int pi=0; pi < this->PrototypeCount(); ++pi) 
		diff_x0_y.col(pi).noalias() = Vector2d(xBase,yBase) - this->P_prototype[pi];


	for(int yRow=0; yRow < rows; yRow++) {
		PMatrix diff_x_y(diff_x0_y); //copy that includes changes to Y as well.
		for(int xCol=0; xCol < cols; xCol++) {

			// x = xBase + xCol * xDelta;  y = yBase + yCol * yDelta;
			MatrixXd::Index bestProtoI;
			diff_x_y.colwise().squaredNorm().minCoeff(&bestProtoI);
			classDiagram(yRow, xCol) = this->pLabel[bestProtoI];

			diff_x_y.row(0).array() += xDelta;
		}
		diff_x0_y.row(1).array() += yDelta;
	}
}

void GmLvqModel::DoOptionalNormalization() {
	if(settings.NormalizeProjection) {
		normalizeProjection(P);
		for(size_t i=0;i<prototype.size();++i)
			RecomputeProjection((int)i);
	}
}
