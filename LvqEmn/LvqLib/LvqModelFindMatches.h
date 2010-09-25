#pragma once
#include "GoodBadMatch.h"
#define DBG(X) (std::cout<< #X <<":\n"<<(X)<<"\n")

template<typename TDerivedModel, typename TProcessedPoint> class LvqModelFindMatches {
protected:

	EIGEN_STRONG_INLINE GoodBadMatch findMatches(TProcessedPoint const & trainPoint, int trainLabel) const {
		using std::cout;
		GoodBadMatch match;
		TDerivedModel const & self = static_cast<TDerivedModel const &>(*this);
		assert( match.matchBad <0 && match.matchGood <0 );
		assert(trainPoint.sum() == trainPoint.sum());
		for(int i=0;i<self.PrototypeCount();i++) {
			double curDist = self.SqrDistanceTo(i, trainPoint);
			if(self.PrototypeLabel(i) == trainLabel) {
				if(curDist < match.distGood) {
					match.matchGood = i;
					match.distGood = curDist;
				}
			} else {
				if(curDist < match.distBad) {
					match.matchBad = i;
					match.distBad = curDist;
				}
			}
		}

#ifndef NDEBUG
		if(match.matchBad < 0 ||match.matchGood <0) {
			DBG(match.matchBad);
			DBG(match.matchGood);
			DBG(match.distBad);
			DBG(match.distGood);
			DBG(self.PrototypeCount());
			DBG(trainPoint.sum());
			DBG(trainPoint.sum() == trainPoint.sum());
			DBG(trainPoint);
		}
		assert( match.matchBad >= 0 && match.matchGood >=0 );
#endif
		return match;
	}
};