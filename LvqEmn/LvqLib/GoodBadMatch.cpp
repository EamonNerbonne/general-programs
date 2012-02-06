#include "stdafx.h"
#include "GoodBadMatch.h"
using std::tanh;
	MatchQuality GoodBadMatch::LvqQuality() {
		MatchQuality retval;
		
		retval.distBad = distBad;
		retval.distGood = distGood;
		//retval.muK =  -2.0*distGood / (sqr(distGood) + sqr(distBad));
		//retval.muJ = +2.0*distBad / (sqr(distGood) + sqr(distBad));
		if(distGood > 2*std::numeric_limits<double>::min() && distBad > distGood) { //implies neither distance is zero
			retval.isErr = true;
			retval.costFunc = (distGood - distBad)/(distGood+distBad);
			double distRatioSq = sqr(distGood/distBad);
			double distRatioSqP1 = 1+ distRatioSq;
			retval.muK =  -2.0*distRatioSq / (distGood * distRatioSqP1);
			retval.muJ = +2.0 / (distBad * distRatioSqP1);
		} else if(distBad > 2*std::numeric_limits<double>::min() && distGood >= distBad) { //implies neither distance is zero
			retval.isErr = false;
			retval.costFunc = (distGood - distBad)/(distGood+distBad);
			double distRatioSq = sqr(distBad/distGood);
			double distRatioSqP1 = 1+ distRatioSq;
			retval.muK = -2.0 / (distGood * distRatioSqP1);
			retval.muJ =  +2.0*distRatioSq / (distBad * distRatioSqP1);
		} else {//smaller distance is just too small
			retval.costFunc = 0.0; //so approximate with distBad == distGood == tiny
			retval.muK =  -1.0; // this makes no sense:should be much higher, but whatever; won't matter.
			retval.muJ = +1.0;
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
	