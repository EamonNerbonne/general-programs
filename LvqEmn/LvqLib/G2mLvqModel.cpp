#include "stdafx.h"
#include "G2mLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
using namespace std;
using namespace Eigen;

G2mLvqModel::G2mLvqModel(LvqModelSettings & initSettings)
	: LvqProjectionModelBase(initSettings)
	, m_vJ(initSettings.Dimensions())
	, m_vK(initSettings.Dimensions())
{
	if(initSettings.Dimensionality!=0 && initSettings.Dimensionality!=2)
		throw "Illegal Dimensionality";
	using namespace std;
	initSettings.AssertModelIsOfRightType(this);


	int protoCount = accumulate(initSettings.PrototypeDistribution.begin(),initSettings.PrototypeDistribution.end(),0);
	prototype.resize(protoCount);
	
	int maxProtoCount=0;
	int protoIndex=0;
	for(int label=0; label <(int) initSettings.PrototypeDistribution.size();label++) {
		int labelCount =initSettings.PrototypeDistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = G2mLvqPrototype(initSettings.RngParams, initSettings.RandomInitialBorders, label, initSettings.PerClassMeans.col(label));
			prototype[protoIndex].ComputePP(P);
			protoIndex++;
		}
		maxProtoCount = max(maxProtoCount, labelCount);
	}

	if(initSettings.NgUpdateProtos && maxProtoCount>1) 
		ngMatchCache.resize(maxProtoCount);//otherwise size 0!

	assert(accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0)== protoIndex);
}

typedef Map<VectorXd, Aligned> MVectorXd;

MatchQuality G2mLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	using namespace std;
	double learningRate = stepLearningRate();

	double lr_point = learningRate,
		lr_P = learningRate * settings.LrScaleP,
		lr_B = learningRate * settings.LrScaleB; 

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
	G2mLvqPrototype &J = prototype[matches.matchGood];
	G2mLvqPrototype &K = prototype[matches.matchBad];
	double muJ2 = matches.MuJ() * 2;
	double muK2 = matches.MuK() * 2;

	MVectorXd vJ(m_vJ.data(),m_vJ.size());
	MVectorXd vK(m_vK.data(),m_vK.size());

	Vector2d P_vJ= J.P_point - P_trainPoint;
	Vector2d P_vK = K.P_point - P_trainPoint;
	Vector2d muK2_Bj_P_vJ = muK2 *  (J.B * P_vJ) ;
	Vector2d muJ2_Bk_P_vK = muJ2 *  (K.B * P_vK) ;
	vJ = J.point - trainPoint;
	vK = K.point - trainPoint;
	Vector2d muK2_BjT_Bj_P_vJ =  J.B.transpose() * muK2_Bj_P_vJ ;
	Vector2d muJ2_BkT_Bk_P_vK = K.B.transpose() * muJ2_Bk_P_vK ;

	J.B.noalias() -= lr_B * muK2_Bj_P_vJ * P_vJ.transpose() ;
	K.B.noalias() -= lr_B * muJ2_Bk_P_vK * P_vK.transpose() ;
	double distbadRaw=0.0;
	if(settings.UpdatePointsWithoutB) {
		double distgood = P_vJ.squaredNorm();
		distbadRaw = P_vK.squaredNorm();
		double XmuK2 = 2.0*+2.0*distbadRaw / (sqr(distgood) + sqr(distbadRaw));
		J.point.noalias() -= P.transpose()* (lr_point * XmuK2 *P_vJ);
	} else {
		J.point.noalias() -= P.transpose()* (lr_point * muK2_BjT_Bj_P_vJ);
	}
	K.point.noalias() -= P.transpose() * (settings.LrScaleBad*lr_point * muJ2_BkT_Bk_P_vK) ;
	P.noalias() -= (lr_P * muK2_BjT_Bj_P_vJ) * vJ.transpose() + (lr_P * muJ2_BkT_Bk_P_vK) * vK.transpose() ;
	
	if(ngMatchCache.size()>0) {
		double lrSub = lr_point;
		double lrDelta = exp(-LVQ_NG_FACTOR*settings.LR0/learningRate);//TODO: this is rather ADHOC
		for(int i=1;i<fullmatch.foundOk;++i) {
			lrSub*=lrDelta;
			G2mLvqPrototype &Js = prototype[fullmatch.matchesOk[i].idx];
			if(settings.UpdatePointsWithoutB) {
				double muK2s = 2.0 * +2.0*distbadRaw / (sqr((Js.P_point - P_trainPoint).squaredNorm()) + sqr(distbadRaw));
				Js.point.noalias() -= P.transpose() * (lrSub * muK2s * (Js.P_point - P_trainPoint));
			} else {
				double muK2s = 2.0 * +2.0*fullmatch.distBad / (sqr(fullmatch.matchesOk[i].dist) + sqr(fullmatch.distBad));
				Js.point.noalias() -= P.transpose() * (lrSub * muK2s * (Js.B.transpose() * (Js.B * (Js.P_point - P_trainPoint))));
			}
		}
	}

	for(size_t i=0;i<prototype.size();++i)
		prototype[i].ComputePP(P);
	return matches.LvqQuality();
}


LvqModel* G2mLvqModel::clone() const { return new G2mLvqModel(*this); }

size_t G2mLvqModel::MemAllocEstimate() const {
	return 
		sizeof(G2mLvqModel) +
		sizeof(CorrectAndWorstMatches::MatchOk) * ngMatchCache.size()+
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
void G2mLvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const {
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


void G2mLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const {
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

	for(int pi=0; pi < this->PrototypeCount(); ++pi) {
		auto & current_proto = this->prototype[pi];
		B_diff_x0_y.col(pi).noalias() = current_proto.B * ( Vector2d(xBase,yBase) - current_proto.P_point);
		B_xDelta.col(pi).noalias() = current_proto.B * Vector2d(xDelta,0.0);
		B_yDelta.col(pi).noalias() = current_proto.B * Vector2d(0.0,yDelta);
	}

	for(int yRow=0; yRow < rows; yRow++) {
		PMatrix B_diff_x_y(B_diff_x0_y); //copy that includes changes to X as well.
		for(int xCol=0; xCol < cols; xCol++) {
			// x = xBase + xCol * xDelta;  y = yBase + yCol * yDelta;
			MatrixXd::Index bestProtoI;
			B_diff_x_y.colwise().squaredNorm().minCoeff(&bestProtoI);
			classDiagram(yRow, xCol) = this->prototype[bestProtoI].classLabel;

			B_diff_x_y.noalias() += B_xDelta;
		}
		B_diff_x0_y.noalias() += B_yDelta;
	}
}


void G2mLvqModel::DoOptionalNormalization() {
	 if(settings.NormalizeProjection) 
		 normalizeProjection(P);
	 if(settings.NormalizeBoundaries) {
		 if(settings.GloballyNormalize) {
			 double overallNorm = std::accumulate(prototype.begin(), prototype.end(),0.0,
				 [](double cur, G2mLvqPrototype const & proto) -> double { return cur + projectionSquareNorm(proto.B); } 
			     // (cur, proto) => cur + projectionSquareNorm(proto.B)
			 );
			 double scale = 1.0/sqrt(overallNorm / prototype.size());
			 for(size_t i=0;i<prototype.size();++i) prototype[i].B*=scale;
		 } else {
			 for(size_t i=0;i<prototype.size();++i) normalizeProjection(prototype[i].B);
		 }
	 }
}