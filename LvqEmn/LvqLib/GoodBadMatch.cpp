#include "stdafx.h"
#include "GoodBadMatch.h"
using std::tanh;
	MatchQuality GoodBadMatch::LvqQuality() {
		MatchQuality retval;
		retval.isErr = distGood >= distBad;
		retval.costFunc = (distGood - distBad)/(distGood+distBad);
		retval.distBad = distBad;
		retval.distGood = distGood;
		retval.muK = MuK();
		retval.muJ = MuJ();
		return retval;
	}

	MatchQuality GoodBadMatch::GgmQuality() {
		MatchQuality retval;
		retval.isErr = distGood >= distBad;
		retval.costFunc=tanh((distGood-distBad)/4.0);
		retval.distBad = distBad;
		retval.distGood = distGood;
		retval.muJ = (1.0/4.0) * (1 - sqr(retval.costFunc));//
		retval.muK = -retval.muJ;
		return retval;
	}
	