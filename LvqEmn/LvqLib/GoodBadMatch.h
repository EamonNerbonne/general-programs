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
