#pragma once
#include "stdafx.h"

struct GoodBadMatch {
	double distGood, distBad;
	int matchGood, matchBad;
	inline GoodBadMatch()
		: distGood(std::numeric_limits<double>::infinity())
		, distBad(std::numeric_limits<double>::infinity())
#ifndef NDEBUG
		, matchGood(-1)
		, matchBad(-1)
#endif
	{}
	double CostFunc() const { return (distGood - distBad)/(distGood+distBad); }
	bool IsErr()const{return distGood > distBad;}
};


template<typename TDerivedModel, typename TProcessedPoint> class LvqModelFindMatches {
protected:
#pragma warning (disable: 4127)
#define ASSTRING(X) #X
#define DBG(X) (cout<<ASSTRING(X)<<": "<<X<<"\n")

	EIGEN_STRONG_INLINE GoodBadMatch findMatches(TProcessedPoint const & trainPoint, int trainLabel) const {
		using std::cout;
		GoodBadMatch match;
		TDerivedModel const & self = static_cast<TDerivedModel const &>(*this);

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

		if(match.matchBad < 0 ||match.matchGood <0) {
			assert( match.matchBad >= 0 && match.matchGood >=0 );
			DBG(match.matchBad);
			DBG(match.matchGood);
			DBG(match.distBad);
			DBG(match.distGood);
			DBG(self.PrototypeCount());//WTF: this statement impacts gcc _correctness_?
		}
		return match;
	}

};