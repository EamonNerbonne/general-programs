#include "stdafx.h"
#include "GoodBadMatch.h"

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
		retval.costFunc=std::tanh((distGood-distBad)/4.0);
		retval.distBad = distBad;
		retval.distGood = distGood;
		retval.muK = -MuGgm();
		retval.muJ = MuGgm();//
		if(!isfinite(retval.costFunc)) throw "Invalid Cost func!";
		return retval;
	}
	