#include "stdafx.h"
#include "GoodBadMatch.h"
using std::tanh;
using std::max;
using std::min;
	MatchQuality GoodBadMatch::LvqQuality() {
		MatchQuality retval;
		
		retval.distBad = distBad;
		retval.distGood = distGood;

		double sqGood_sqBad = sqr(distGood) + sqr(distBad);
		retval.costFunc = (distGood - distBad)/(distGood+distBad);

		if(sqGood_sqBad > std::numeric_limits<double>::min()) {
			
			retval.muK =  -2.0*distGood / sqGood_sqBad;
			retval.muJ = +2.0*distBad / sqGood_sqBad;
			retval.isErr = distGood >= distBad;
		} else if(distBad > distGood) { //implies distBad > 0
			retval.isErr = false;
			double distRatioSq = sqr(distGood/distBad);//less than one
			double distRatioSqP1 = 1+ distRatioSq;
			retval.muK =  -2.0*distRatioSq / (max( std::numeric_limits<double>::min()*2, distGood) * distRatioSqP1);
			retval.muJ = +2.0 / (max( std::numeric_limits<double>::min()*2, distBad) * distRatioSqP1);
		} else if(distGood > distBad) { //implies distGood > 0
			retval.isErr = true;
			double distRatioSq = sqr(distBad/distGood);
			double distRatioSqP1 = 1+ distRatioSq;
			retval.muK = -2.0 / ( max( std::numeric_limits<double>::min()*2, distGood) * distRatioSqP1);
			retval.muJ =  +2.0*distRatioSq / (max( std::numeric_limits<double>::min()*2, distBad) * distRatioSqP1);
		} else {
			retval.isErr = true;
			retval.costFunc = 0.0; 
			retval.muJ = +1.0 / max(std::numeric_limits<double>::min()*2, distBad) ;
			retval.muK =  -retval.muJ;
		}
#ifndef NDEBUG
		if(!(isfinite_emn(retval.costFunc) && isfinite_emn(retval.muJ) && isfinite_emn(retval.muK))) {
			std::cout<< distBad <<" "<< distGood<<" "<<retval.muK <<" "<<retval.muJ<<" "<<retval.costFunc;
			//std::cout << " "<<path;
			std::cout<<"\n";
			retval.costFunc = 0.0; //so approximate with distBad == distGood == tiny
			retval.muK =  -1.0; // this makes no sense:should be much higher, but whatever; won't matter.
			retval.muJ = +1.0;
		}
#endif
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
	