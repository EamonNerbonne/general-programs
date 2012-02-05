#include "stdafx.h"
#include "GoodBadMatch.h"
using std::tanh;
	MatchQuality GoodBadMatch::LvqQuality() {
		MatchQuality retval;
		retval.isErr = distGood >= distBad;
		retval.costFunc = (distGood - distBad)/(distGood+distBad);
		retval.distBad = distBad;
		retval.distGood = distGood;
		//retval.muK =  -2.0*distGood / (sqr(distGood) + sqr(distBad));
		//retval.muJ = +2.0*distBad / (sqr(distGood) + sqr(distBad));
		if(distGood == 0.0) {
			if(distBad == 0.0) {
				retval.muK =  -1.0;
				retval.muJ = +1.0;
			} else {
				retval.muK = 0.0;
				retval.muJ = +2.0 / (distBad);
			}
		} else if (distBad==0.0) {
			retval.muK =  -2.0 / distGood;
			retval.muJ = 0.0;
		} else {
			double distRatioSq = sqr(distGood/distBad);
			double distRatioSqP1 = 1+ distRatioSq;

			retval.muK =  -2.0*distRatioSq / (distGood * distRatioSqP1);
			retval.muJ = +2.0 / (distBad * distRatioSqP1);
		}
		assert(isfinite_emn(retval.costFunc) && isfinite_emn(retval.muJ) && isfinite_emn(retval.muK));
		return retval;
	}

	MatchQuality GoodBadMatch::GgmQuality() {
		MatchQuality retval;
		retval.isErr = distGood >= distBad;

		retval.costFunc = tanh((distGood-distBad)/4.0);
		retval.distBad = distBad;
		retval.distGood = distGood;
		retval.muJ = (1.0/4.0) * (1 - sqr(retval.costFunc));
		retval.muK = -retval.muJ;
		return retval;

		//double pGood = exp(-0.5*distGood);
		//double pBad = exp(-0.5*distBad);
		//retval.costFunc= pBad - pGood;
		//retval.distBad = distBad;
		//retval.distGood = distGood;
		//retval.muJ = pGood;//
		//retval.muK = -pBad;
		//return retval;

		//retval.costFunc=  distGood - distBad;
		//retval.distBad = distBad;
		//retval.distGood = distGood;
		//retval.muJ = 1;//
		//retval.muK = -1;
		//return retval;

		//double rawcost=1+tanh((distGood-distBad)/4.0);
		//retval.costFunc=log(rawcost);
		//retval.distBad = distBad;
		//retval.distGood = distGood;
		//retval.muJ = (1.0/4.0) * (1 - sqr(retval.costFunc)) / rawcost;//
		//retval.muK = -retval.muJ;
		//return retval;

	}
	