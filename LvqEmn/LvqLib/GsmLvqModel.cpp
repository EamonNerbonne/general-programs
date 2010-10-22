#include "StdAfx.h"
#include "GsmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
using namespace std;

GsmLvqModel::GsmLvqModel(LvqModelSettings & initSettings)
	: LvqProjectionModelBase(initSettings) 
	, lr_scale_P(LVQ_LrScaleP)
	, vJ(initSettings.Dimensions())
	, vK(initSettings.Dimensions())
{
	initSettings.AssertModelIsOfRightType(this);

	int protoCount = accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0);
	pLabel.resize(protoCount);
	
	prototype.resize(protoCount);
	P_prototype.resize(protoCount);

	int maxProtoCount=0;
	int protoIndex=0;
	for(int label = 0; label <(int) initSettings.PrototypeDistribution.size();label++) {
		int labelCount =initSettings.PrototypeDistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = initSettings.PerClassMeans.col(label);
			pLabel(protoIndex) = label;
			RecomputeProjection(protoIndex);

			protoIndex++;
		
		}
		maxProtoCount = max(maxProtoCount, labelCount);
	}

	if(initSettings.NgUpdateProtos && maxProtoCount>1) 
		ngMatchCache.resize(maxProtoCount);//otherwise size 0!

	assert( accumulate(initSettings.PrototypeDistribution.begin(),initSettings.PrototypeDistribution.end(),0)== protoIndex);
}

GoodBadMatch GsmLvqModel::learnFrom(VectorXd const & trainPoint, int trainLabel) {
	double learningRate = stepLearningRate();

	using namespace std;

	double lr_point = learningRate,
		lr_P = learningRate * this->lr_scale_P;

	assert(lr_P>=0  &&  lr_point>=0);

	Vector2d P_trainPoint(P * trainPoint);

	CorrectAndWorstMatches fullmatch(nullptr);
	GoodBadMatch matches;
	if(ngMatchCache.size()>0) {
		fullmatch = CorrectAndWorstMatches(& (ngMatchCache[0]));
		for(int i=0;i<(int)prototype.size();++i) {
			double curDist = SqrDistanceTo(i, P_trainPoint);

			if(PrototypeLabel(i) == trainLabel) 
				fullmatch.RegisterOk(curDist,i);
			else 
				fullmatch.RegisterBad(curDist,i);
		}
		fullmatch.SortOk();
		matches = fullmatch.ToGoodBadMatch();
	} else {
		matches = findMatches(P_trainPoint, trainLabel);
	}

	//now matches.good is "J" and matches.bad is "K".

	double mu_J = -2.0*matches.distGood / (sqr(matches.distGood) + sqr(matches.distBad));
	double mu_K = +2.0*matches.distBad / (sqr(matches.distGood) + sqr(matches.distBad));

	int J = matches.matchGood;
	int K = matches.matchBad;

	vJ = prototype[J] - trainPoint;
	vK = prototype[K] - trainPoint;

	//VectorXd
	Vector2d muK2_P_vJ(mu_K * 2.0 * (P_prototype[J] - P_trainPoint) );
	Vector2d muJ2_P_vK(mu_J * 2.0 * (P_prototype[K] - P_trainPoint) );

	//differential of cost function Q wrt w_J; i.e. wrt J->point.  Note mu_K(!) for differention wrt J(!)
	prototype[J].noalias() -= P.transpose() * (lr_point * muK2_P_vJ);
	prototype[K].noalias() -= P.transpose() * (LVQ_LrScaleBad*lr_point *muJ2_P_vK);

	//differential wrt. global projection matrix is subtracted...
	P.noalias() -= (lr_P * muK2_P_vJ) * vJ.transpose() + (lr_P * muJ2_P_vK) * vK.transpose();

	if(ngMatchCache.size()>0) {
		double lrSub = lr_point;
		double lrDelta = 0.1;//exp(-2*LVQ_LR0/learningRate);//TODO: this is rather ADHOC
		for(int i=1;i<fullmatch.foundOk;++i) {
			lrSub*=lrDelta;
			VectorXd &Js = prototype[fullmatch.matchesOk[i].idx];
			Vector2d &P_Js = P_prototype[fullmatch.matchesOk[i].idx];;
			double mu_K2s_lrSub = lrSub* 2.0 * +2.0*fullmatch.distBad / (sqr(fullmatch.matchesOk[i].dist) + sqr(fullmatch.distBad));
			Js.noalias() -=  P.transpose() * (mu_K2s_lrSub *  (P_Js - P_trainPoint));
		}
	}


	for(int i=0;i<pLabel.size();++i)
		RecomputeProjection(i);
	return matches;
}

LvqModel* GsmLvqModel::clone() const { return new GsmLvqModel(*this);	}

MatrixXd GsmLvqModel::GetProjectedPrototypes() const {
	MatrixXd retval(LVQ_LOW_DIM_SPACE, static_cast<int>(prototype.size()));
	for(unsigned i=0;i<prototype.size();++i)
		retval.col(i) = P_prototype[i];
	return retval;
}

vector<int> GsmLvqModel::GetPrototypeLabels() const {
	vector<int> retval(prototype.size());
	for(unsigned i=0;i<prototype.size();++i)
		retval[i] = pLabel[i];
	return retval;
}

size_t GsmLvqModel::MemAllocEstimate() const {
	return 
		sizeof(GsmLvqModel) + //base structure size
		sizeof(int)*pLabel.size() + //dyn.alloc labels
		sizeof(double) * (P.size() ) + //dyn alloc transform + temp transform
		sizeof(double) * (vJ.size()*3) + //various vector temps
		sizeof(VectorXd) *prototype.size() +//dyn alloc prototype base overhead
		sizeof(double) * (prototype.size() * vJ.size()) + //dyn alloc prototype data
		sizeof(Vector2d) * P_prototype.size() + //cache of pretransformed prototypes
		(16/2) * (5+prototype.size()*2);//estimate for alignment mucking.
}

void GsmLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const {
	int cols = static_cast<int>(classDiagram.cols());
	int rows = static_cast<int>(classDiagram.rows());
	double xDelta = (x1-x0) / cols;
	double yDelta = (y1-y0) / rows;
	double xBase = x0+xDelta*0.5;
	double yBase = y0+yDelta*0.5;

	PMatrix diff_x0_y(LVQ_LOW_DIM_SPACE,PrototypeCount()); //Contains (testPoint[x, y0] - P*proto_i)  for all proto's i
	//will update to include changes to X.

	for(int pi=0; pi < this->PrototypeCount(); ++pi) 
		diff_x0_y.col(pi).noalias() =  Vector2d(xBase,yBase) - this->P_prototype[pi];
	

	for(int yRow=0;  yRow < rows;  yRow++) {
		PMatrix diff_x_y(diff_x0_y); //copy that includes changes to Y as well.
		for(int xCol=0;  xCol < cols;  xCol++) {

			// x = xBase + xCol * xDelta;  y = yBase + yCol * yDelta;
			MatrixXd::Index bestProtoI;
			diff_x_y.colwise().squaredNorm().minCoeff(&bestProtoI);
			classDiagram(yRow, xCol) = this->pLabel[bestProtoI];

			diff_x_y.row(0).array() += xDelta;
		}
		diff_x0_y.row(1).array() += yDelta;
	}
}

void GsmLvqModel::DoOptionalNormalization() {
	 if(settings.NormalizeProjection) 
		 normalizeProjection(P);
}
