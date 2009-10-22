#include "stdafx.h"
#include "G2mLvqMatch.h"

G2mLvqGoodBadMatch::G2mLvqGoodBadMatch(PMatrix const * Pmat,VectorXd const * p, int classLabel) 
	: P(Pmat)
	, unknownPoint(p)
	, actualClassLabel(classLabel)
	, distanceGood(std::numeric_limits<double>::infinity()) 
	, distanceBad(std::numeric_limits<double>::infinity()) 
	, good(NULL)
	, bad(NULL)
{ }

void G2mLvqGoodBadMatch::AccumulateMatch(G2mLvqPrototype const & option, VectorXd & tmp) {
	double optionDist = option.SqrDistanceTo(*unknownPoint,*P,tmp);

	assert(optionDist > 0);
	assert(optionDist < std::numeric_limits<double>::infinity());
	if(option.ClassLabel() == actualClassLabel) {
		if(optionDist < distanceGood) {
			good = &option;
			distanceGood = optionDist;
		}
	} else {
		if(optionDist < distanceBad) {
			bad = &option;
			distanceBad = optionDist;
		}
	}
}

G2mLvqMatch::G2mLvqMatch(PMatrix const * Pmat,VectorXd const * p) 
	: P(Pmat)
	, unknownPoint(p)
	, distance(std::numeric_limits<double>::infinity()) 
	, match(NULL)
{ }

void G2mLvqMatch::AccumulateMatch(G2mLvqPrototype const & option, VectorXd & tmp) {
	double optionDist = option.SqrDistanceTo(*unknownPoint,*P,tmp);

	assert(optionDist > 0);
	assert(optionDist < std::numeric_limits<double>::infinity());
	if(optionDist < distance) {
		match = &option;
		distance = optionDist;
	}
}

