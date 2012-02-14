#pragma once
#include "GoodBadMatch.h"

template<typename TDerivedModel, typename TProcessedPoint> class LvqModelFindMatches {
protected:
#ifdef NDEBUG
	EIGEN_STRONG_INLINE 
#endif
		GoodBadMatch findMatches(TProcessedPoint const & trainPoint, int trainLabel) const {
		GoodBadMatch match;
		TDerivedModel const & self = static_cast<TDerivedModel const &>(*this);
		assert( match.matchBad <0 && match.matchGood <0 );
#ifndef NDEBUG
		if(!isfinite_emn(trainPoint.sum())) {
			std::cout << trainPoint<<"\n";
			std::cout.flush();
			assert(isfinite_emn(trainPoint.sum()));
		}
#endif
		for(int i=0;i<self.PrototypeCount();i++) {
			double curDist = self.SqrDistanceTo(i, trainPoint);
			if(self.PrototypeLabel(i) == trainLabel) {
				if(!(curDist >= match.distGood)) {
					match.matchGood = i;
					match.distGood = curDist;
				}
			} else {
				if(!(curDist >= match.distBad)) {
					match.matchBad = i;
					match.distBad = curDist;
				}
			}
		}

#ifndef NDEBUG
		if(match.matchBad < 0 ||match.matchGood <0 || !isfinite_emn(match.distGood + match.distBad)) {
			DBG(match.matchGood);
			DBG(match.distGood);
			DBG(match.matchBad);
			DBG(match.distBad);
			DBG(self.PrototypeCount());
			DBG(trainPoint.sum());
			DBG(trainPoint.sum() == trainPoint.sum());
			DBG(trainPoint.transpose());
			LvqModelRuntimeSettings const & settings = self.ModelSettings();
			DBG(settings.LR0);
			DBG(settings.LrScaleP);
			DBG(settings.LrScaleB);
			DBG(settings.MuOffset);
			DBG(settings.LrScaleBad);
		}
		assert( match.matchBad >= 0 && match.matchGood >=0 );
#endif
		return match;
	}
};