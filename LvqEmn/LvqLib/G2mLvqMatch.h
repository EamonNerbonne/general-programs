#pragma once
#include "stdafx.h"
#include "G2mLvqPrototype.h"
USING_PART_OF_NAMESPACE_EIGEN
struct G2mLvqGoodBadMatch {
	PMatrix const * P;
	VectorXd const * unknownPoint;
	int actualClassLabel;

	double distanceGood,distanceBad;
	G2mLvqPrototype const *good;
	G2mLvqPrototype const *bad;

	G2mLvqGoodBadMatch(PMatrix const * Pmat, VectorXd const * p, int classLabel);
	
	void AccumulateMatch(G2mLvqPrototype const & option, VectorXd & tmp);
};


struct G2mLvqMatch {
	PMatrix const * P;
	VectorXd const * unknownPoint;

	double distance;
	G2mLvqPrototype const * match;

	G2mLvqMatch(PMatrix const * Pmat ,VectorXd const * p);
	void AccumulateMatch(G2mLvqPrototype const & option, VectorXd & tmp);
};
