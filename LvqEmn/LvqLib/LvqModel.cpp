#include "stdafx.h"
#include "LvqModel.h"
#include "utils.h"
#include "LvqMatch.h"

LvqModel::LvqModel( std::vector<int> protodistribution) : classCount((int)protodistribution.size()) {
	using namespace std;
	//		for (vector<int>::iterator it = protodistribution.begin(); it!=protodistribution.end(); ++it) {
	for(int label=0; label< protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++)
			prototype.push_back(LvqPrototype(label,(int)prototype.size()));
	}
	assert(sum(0, protodistribution) == prototype.size());
}


int LvqModel::classify(VectorXd unknownPoint) const{
	using namespace std;
	
	Vector2d projectedPoint = P * unknownPoint;

	LvqMatch bestMatch= accumulate(prototype.begin(), prototype.end(), LvqMatch(&P, unknownPoint), LvqMatch::AccumulateHelper);
	assert(bestMatch.match != NULL);
	return bestMatch.match->ClassLabel();
}


void LvqModel::learnFrom(VectorXd newPoint, int classLabel, double lr_P, double lr_B, double lr_point) {
	using namespace std;
	assert(lr_P>0&& lr_B>0 && lr_point>0);
	Vector2d projectedPoint = P * newPoint;
	
	LvqGoodBadMatch matches = accumulate(prototype.begin(), prototype.end(), LvqGoodBadMatch(&P, newPoint, classLabel), LvqGoodBadMatch::AccumulateHelper);
	
	assert(matches.good !=NULL && matches.bad!=NULL);
	//now matches.good is "J" and matches.bad is "K".

	VectorXd vJ = matches.good->point - newPoint;

	//TODO:etc.
}
