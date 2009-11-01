#pragma once
#include "stdafx.h"
#include "G2mLvqPrototype.h"
USING_PART_OF_NAMESPACE_EIGEN
struct G2mLvqGoodBadMatch {
	Vector2d const* projectedPoint;
	int actualClassLabel;

	double distanceGood, distanceBad;
	G2mLvqPrototype const *good;
	G2mLvqPrototype const *bad;

	G2mLvqGoodBadMatch(Vector2d const * projectedTestPoint, int classLabel)
		: actualClassLabel(classLabel)
		, distanceGood(std::numeric_limits<double>::infinity()) 
		, distanceBad(std::numeric_limits<double>::infinity()) 
		, good(NULL)
		, bad(NULL)
		, projectedPoint(projectedTestPoint)
	{ }

	void AccumulateMatch(G2mLvqPrototype const & option) {
		double optionDist = option.SqrDistanceTo(*projectedPoint);
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
};


struct G2mLvqMatch {
	Vector2d const * P_testPoint;

	double distance;
	G2mLvqPrototype const * match;

	G2mLvqMatch(Vector2d const * P_testPoint)
		: distance(std::numeric_limits<double>::infinity()) 
		, match(NULL)
		, P_testPoint(P_testPoint)
	{ }

	void AccumulateMatch(G2mLvqPrototype const & option) {
		double optionDist = option.SqrDistanceTo(*P_testPoint);
		assert(optionDist > 0);
		assert(optionDist < std::numeric_limits<double>::infinity());
		if(optionDist < distance) {
			match = &option;
			distance = optionDist;
		}
	}


};

