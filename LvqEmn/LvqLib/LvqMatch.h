#pragma once
#include "stdafx.h"
#include "LvqPrototype.h"
USING_PART_OF_NAMESPACE_EIGEN
struct LvqGoodBadMatch {
	PMatrix const * P;
	VectorXd const * unknownPoint;
	int actualClassLabel;

	double distanceGood,distanceBad;
	LvqPrototype const *good;
	LvqPrototype const *bad;

	LvqGoodBadMatch(PMatrix const * Pmat, VectorXd const * p, int classLabel);
	
	void AccumulateMatch(LvqPrototype const & option);
	static LvqGoodBadMatch & AccumulateHelper(LvqGoodBadMatch & best, LvqPrototype const & option); 
};


struct LvqMatch {
	PMatrix const * P;
	VectorXd unknownPoint;

	double distance;
	LvqPrototype const * match;

	LvqMatch(PMatrix const * Pmat ,VectorXd p);
	void AccumulateMatch(LvqPrototype const & option);
	static LvqMatch & AccumulateHelper(LvqMatch & best, LvqPrototype const & option); 
};
