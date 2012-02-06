#include "stdafx.h"
#include "GpqLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
#include "MeanMinMax.h"
using namespace std;
using namespace Eigen;

GpqLvqModel::GpqLvqModel(LvqModelSettings & initSettings)
	: LvqProjectionModelBase(initSettings)
	, totalMuJLr(0.0)
	, totalMuKLr(0.0)
	, lastAutoPupdate(0.0)
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

	for(size_t protoIndex=0; protoIndex < (size_t) protoLabels.size(); ++protoIndex) {
		prototype[protoIndex] = GpqLvqPrototype(initB[protoIndex], protoLabels(protoIndex), P*prototypes.col(protoIndex));
	}

	normalizeProjection(P);
	NormalizeBoundaries();

	int maxProtoCount = accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0, [](int a, int b) -> int { return max(a,b); });

	if(initSettings.NGu && maxProtoCount>1) 
		ngMatchCache.resize(maxProtoCount);//otherwise size 0!
}

MatchQuality GpqLvqModel::learnFrom(Vector_N const & trainPoint, int trainLabel) {
	using namespace std;
	double learningRate = stepLearningRate();

	double lr_point = settings.LR0 * learningRate,
		lr_bad = (settings.SlowK  ?  sqr(1.0 - learningRate)  :  1.0) * settings.LrScaleBad;


	Vector_2 P_trainPoint( P * trainPoint );

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
	GpqLvqPrototype &J = prototype[matches.matchGood];
	GpqLvqPrototype &K = prototype[matches.matchBad];
	MatchQuality retval = matches.LvqQuality();
	double muK2lr = max(-2.0, 2 * retval.muK*lr_point),
		muJ2lr =min(2.0, 2 * retval.muJ*lr_point);

	//if(!isfinite_emn(muK2) || !isfinite_emn(muJ2) || muK2 > 1e30 || muJ2>1e30)		return retval;

	Vector_2 pJ= J.P_point - P_trainPoint;
	Vector_2 pK = K.P_point - P_trainPoint;
	Vector_2 muJ2lr_Bj_pJ = muJ2lr *  (J.B * pJ) ;
	Vector_2 muK2lr_Bk_pK = muK2lr *  (K.B * pK) ;
	Vector_2 muJ2lr_BjT_Bj_pJ =  J.B.transpose() * muJ2lr_Bj_pJ ;
	Vector_2 muK2lr_BkT_Bk_pK = K.B.transpose() * muK2lr_Bk_pK ;

	J.B.noalias() -= settings.LrScaleB * muJ2lr_Bj_pJ * pJ.transpose() ;
	K.B.noalias() -= lr_bad * settings.LrScaleB * muK2lr_Bk_pK * pK.transpose() ;
	double distbadRaw=0.0;
	if(settings.wGMu) {
		double distgood = pJ.squaredNorm();
		distbadRaw = pK.squaredNorm();
		double XmuJ2lr = lr_point* 2.0*+2.0*distbadRaw / (sqr(distgood) + sqr(distbadRaw));
		J.P_point.noalias() -= XmuJ2lr * pJ;
	} else {
		J.P_point.noalias() -=  muJ2lr_BjT_Bj_pJ;
	}
	K.P_point.noalias() -= lr_bad * muK2lr_BkT_Bk_pK;

	if(ngMatchCache.size()>0) {
		double lrSub = lr_point;
		double lrDelta = exp(-LVQ_NG_FACTOR/learningRate);//this is rather ad hoc
		for(int i=1;i<fullmatch.foundOk;++i) {
			lrSub*=lrDelta;
			GpqLvqPrototype &Js = prototype[fullmatch.matchesOk[i].idx];
			if(settings.wGMu) {
				double muJ2s =min(2.0, lrSub *2.0 * 2.0*distbadRaw / (sqr((Js.P_point - P_trainPoint).squaredNorm()) + sqr(distbadRaw)));
				Js.P_point.noalias() -=  muJ2s * (Js.P_point - P_trainPoint);
			} else {
				double muJ2s = min(2.0, lrSub *2.0 * 2.0*fullmatch.distBad / (sqr(fullmatch.matchesOk[i].dist) + sqr(fullmatch.distBad)));
				Js.P_point.noalias() -=  muJ2s * (Js.B.transpose() * (Js.B * (Js.P_point - P_trainPoint)));
			}
		}
	}

	if(settings.scP) {
		//double scale = -log(matches.distBad+matches.matchGood)*4*learningRate;
		//P *= exp(scale);P *= 1+x;
		lastAutoPupdate = 0.9 * lastAutoPupdate -log(matches.distBad+matches.distGood);
		double thisupdate = lastAutoPupdate*4*learningRate*0.00001;

		P *= exp(thisupdate);
	}


	Vector_2 lrScaled;
 	if(settings.noKP) {
		lrScaled.noalias() = settings.LrScaleP * (muJ2lr_BjT_Bj_pJ);
	} else {
		lrScaled.noalias() = settings.LrScaleP * (muJ2lr_BjT_Bj_pJ + muK2lr_BkT_Bk_pK);
	}
	//really I should be updating all points now ala P_point += Pdelta * P_pseudoInverse * P_point;
	// alternatively P_point = Pnew * P_T * inv(P*P_T) * P_point
	//but the following code isn't so hot.

	/*
	Pnew.noalias() = P + lrScaled * trainPoint.transpose();

	Matrix_22 P_PT_inv = (P * P.transpose()).inverse();
	Matrix_22 remap = (Pnew * P.transpose()) * P_PT_inv;

	P.noalias() = Pnew;

	for_each(prototype.begin(), prototype.end(), [&remap](GpqLvqPrototype & proto) {	proto.P_point = 0.5*proto.P_point + 0.5 * (remap * proto.P_point);	});
	/*/
	P.noalias() += lrScaled * trainPoint.transpose();
	/**/
	if(!settings.scP && (settings.neiP || settings.noKP)) {
		normalizeProjection(P);//returns scale
		//for(size_t i=0;i<prototype.size();++i) prototype[i].P_point *= scale; //not necessary if you rescale _every_ iter anyhow.
	}
	if(settings.neiB) {
		NormalizeBoundaries();	
	}


	totalMuJLr += lr_point * retval.muJ;
	totalMuKLr -= lr_point * retval.muK;

	return retval;
}


LvqModel* GpqLvqModel::clone() const { return new GpqLvqModel(*this); }

size_t GpqLvqModel::MemAllocEstimate() const {
	return 
		sizeof(GpqLvqModel) +
		sizeof(CorrectAndWorstMatches::MatchOk) * ngMatchCache.size()+
		sizeof(double) * P.size() +
		//sizeof(double) * (m_vJ.size() +m_vK.size()) + //various temps
		sizeof(GpqLvqPrototype)*prototype.size() + //prototypes; part statically allocated
		//sizeof(double) * (prototype.size() * m_vJ.size()) + //prototypes; part dynamically allocated
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
}

Matrix_2N GpqLvqModel::GetProjectedPrototypes() const {
	Matrix_2N retval(LVQ_LOW_DIM_SPACE, static_cast<int>(prototype.size()));
	for(unsigned i=0;i<prototype.size();++i)
		retval.col(i) = prototype[i].projectedPosition();
	return retval;
}

vector<int> GpqLvqModel::GetPrototypeLabels() const {
	vector<int> retval(prototype.size());
	for(unsigned i=0;i<prototype.size();++i)
		retval[i] = prototype[i].label();
	return retval;
}

void GpqLvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const {
	LvqProjectionModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Maximum norm(B)!norm!Border Matrix norm");
	retval.push_back(L"Mean norm(B)!norm!Border Matrix norm");
	retval.push_back(L"Minimum norm(B)!norm!Border Matrix norm");
	retval.push_back(L"Maximum |B|!determinant!Border Matrix absolute determinant");
	retval.push_back(L"Mean |B|!determinant!Border Matrix absolute determinant");
	retval.push_back(L"Minimum |B|!determinant!Border Matrix absolute determinant");
	retval.push_back(L"Cumulative \u03BC-J-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
	retval.push_back(L"Cumulative \u03BC-K-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
}
void GpqLvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const {
	LvqProjectionModel::AppendOtherStats(stats,trainingSet,testSet);
	MeanMinMax norm;
	MeanMinMax det;
	std::for_each(prototype.begin(),prototype.end(), [&](GpqLvqPrototype const & proto) {
		norm.Add(projectionSquareNorm(proto.B));
		det.Add(abs(proto.B.determinant()));
	});
	stats.push_back(norm.max());
	stats.push_back(norm.mean());
	stats.push_back(norm.min());

	stats.push_back(det.max());
	stats.push_back(det.mean());
	stats.push_back(det.min());

	stats.push_back(totalMuJLr);
	stats.push_back(totalMuKLr);
}


void GpqLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const {
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

	for(int pi=0; pi < this->PrototypeCount(); ++pi) {
		auto & current_proto = this->prototype[pi];
		B_diff_x0_y.col(pi).noalias() = current_proto.B * ( Vector_2(xBase,yBase) - current_proto.P_point);
		B_xDelta.col(pi).noalias() = current_proto.B * Vector_2(xDelta,0.0);
		B_yDelta.col(pi).noalias() = current_proto.B * Vector_2(0.0,yDelta);
	}

	for(int yRow=0; yRow < rows; yRow++) {
		Matrix_P B_diff_x_y(B_diff_x0_y); //copy that includes changes to X as well.
		for(int xCol=0; xCol < cols; xCol++) {
			// x = xBase + xCol * xDelta;  y = yBase + yCol * yDelta;
			Matrix_NN::Index bestProtoI;
			B_diff_x_y.colwise().squaredNorm().minCoeff(&bestProtoI);
			classDiagram(yRow, xCol) = (unsigned char)this->prototype[bestProtoI].classLabel;

			B_diff_x_y.noalias() += B_xDelta;
		}
		B_diff_x0_y.noalias() += B_yDelta;
	}
}

void GpqLvqModel::NormalizeBoundaries() {
		if(!settings.LocallyNormalize) {
			double overallNorm = std::accumulate(prototype.begin(), prototype.end(),0.0,
				[](double cur, GpqLvqPrototype const & proto) -> double { return cur + projectionSquareNorm(proto.B); } 
			// (cur, proto) => cur + projectionSquareNorm(proto.B)
			);
			double scale = 1.0/sqrt(overallNorm / prototype.size());
			for(size_t i=0;i<prototype.size();++i) prototype[i].B*=scale;
		} else {
			for(size_t i=0;i<prototype.size();++i) normalizeProjection(prototype[i].B);
		}
}

void GpqLvqModel::DoOptionalNormalization() {
	if(!(settings.noKP||settings.neiP||settings.scP)) {
		LvqFloat scale = normalizeProjection(P);
		for(size_t i=0;i<prototype.size();++i)
			prototype[i].P_point *= scale;
	}
	if(!settings.neiB) {
		NormalizeBoundaries();	
	}
}

void GpqLvqModel::compensateProjectionUpdate(Matrix_22 U, double scale) {
	for(size_t i=0;i < prototype.size();++i) {
		prototype[i].B *= U;
		prototype[i].P_point = U.transpose()*prototype[i].P_point * scale;
	}
}


Matrix_NN GpqLvqModel::PrototypeDistances(Matrix_NN const & points) const {
	Matrix_2N P_points = P*points;
	Matrix_NN newPoints(prototype.size(), points.cols());
	for(size_t protoI=0;protoI<prototype.size();++protoI) {
		newPoints.row(protoI).noalias() = (prototype[protoI].B * (P_points.colwise() - prototype[protoI].P_point)).colwise().squaredNorm();
	}
	return newPoints;
}