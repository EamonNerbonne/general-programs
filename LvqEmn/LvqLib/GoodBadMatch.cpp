#include "stdafx.h"
#include "GoodBadMatch.h"

	MatchQuality GoodBadMatch::LvqQuality() {
		MatchQuality retval;
		retval.isErr = distGood >= distBad;
		retval.costFunc = (distGood - distBad)/(distGood+distBad);
		return retval;
	}

	MatchQuality GoodBadMatch::GmmQuality() {
		MatchQuality retval;
		retval.isErr = distGood >= distBad;
		if(retval.isErr) {
			double pj_pk = exp(distBad-distGood); //0 -> 1
			retval.costFunc = 2/(1+pj_pk)-1;
		} else {
			double pk_pj = exp(distGood-distBad); //0 -> 1
			retval.costFunc = 2*pk_pj/(1+pk_pj)-1;
		}
		if(!isfinite(retval.costFunc)) throw "Invalid Cost func!";
		return retval;
	}
