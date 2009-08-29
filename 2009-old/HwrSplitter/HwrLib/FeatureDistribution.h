#pragma once
#include "stdafx.h"
#include <math.h>
#include "HwrConfig.h"
#include "feature/featurevector.h"
#include "randHelper.h"
#include "LogNumber.h"

inline static Float sqr(Float x) { return x*x; }

class FeatureDistribution
{

	Float logDCfactor;
	Float varX(int i) { return sX[i] / weightSum;}
	void setVarX(int i, Float newVar) {sX[i] = newVar*weightSum;}
public:
	Float weightSum;
	FeatureVector meanX, sX;//public for managed/native transition simplicity.

	FeatureDistribution(void);
	~FeatureDistribution(void);
	void initRandom();
	Float getWeightSum()const{return weightSum;}
	Float getDCoffset()const{return logDCfactor;}

	inline Float occurence() {return weightSum;}
	void ScaleWeightBy(double scaleFactor) { 
		weightSum*=scaleFactor;
		for (int i=0;i<NUMBER_OF_FEATURES;i++) 
			sX[i]*=scaleFactor;
	}


	Float LogProbDensityOf(FeatureVector const & target) {
		Float distSqr=0.0; 
		for(int i=0; i<NUMBER_OF_FEATURES;i++) 
			distSqr += sqr(target[i]-meanX[i])/sX[i];
		return /*logDCfactor*0.001 +*/ -0.5*distSqr*weightSum;
	}

	void probDensityOf(FeatureVector const & target, Float & prob)	{ prob= std::exp(LogProbDensityOf(target));	}
	void probDensityOf(FeatureVector const & target, LogNumber & prob)	{ prob = LogNumber::FromExp(LogProbDensityOf(target)); }

	void RecomputeDCfactor();

	void CombineWith(FeatureDistribution const & other);
	void CombineWith(FeatureVector const & vect, Float occurenceProb);
};
