#include "stdafx.h"
#include "LvqMatch.h"

LvqGoodBadMatch::LvqGoodBadMatch(PMatrix const * Pmat,VectorXd const * p, int classLabel) 
	: P(Pmat)
	, unknownPoint(p)
	, actualClassLabel(classLabel)
	, distanceGood(std::numeric_limits<double>::infinity()) 
	, distanceBad(std::numeric_limits<double>::infinity()) 
	, good(NULL)
	, bad(NULL)
{ }

void LvqGoodBadMatch::AccumulateMatch(LvqPrototype const & option) {
	double optionDist = option.SqrDistanceTo(*unknownPoint,*P);

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


LvqGoodBadMatch & LvqGoodBadMatch::AccumulateHelper(LvqGoodBadMatch & best, LvqPrototype const & option) {
	best.AccumulateMatch(option);
	return best;
}

LvqMatch::LvqMatch(PMatrix const * Pmat,VectorXd p) 
	: P(Pmat)
	, unknownPoint(p)
	, distance(std::numeric_limits<double>::infinity()) 
	, match(NULL)
{ }

void LvqMatch::AccumulateMatch(LvqPrototype const & option) {
	double optionDist = option.SqrDistanceTo(unknownPoint,*P);

	assert(optionDist > 0);
	assert(optionDist < std::numeric_limits<double>::infinity());
	if(optionDist < distance) {
		match = &option;
		distance = optionDist;
	}
}

LvqMatch & LvqMatch::AccumulateHelper(LvqMatch & best, LvqPrototype const & option) {
	best.AccumulateMatch(option);
	return best;
}
