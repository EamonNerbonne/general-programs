#pragma once
#include "stdafx.h"
#include <math.h>
#include "HwrConfig.h"
#include "feature/featurevector.h"
#include "randHelper.h"
#include "LogNumber.h"


class FeatureDistribution
{

	Float logDCfactor;
public:
	Float varX(int i) { return sX[i] / weightSum;}
	void setVarX(int i, Float newVar) {sX[i] = newVar*weightSum;}
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

	//Float LogProbDensityOf(FeatureVector const & target,FeatureVector const & weights) {
	//	Float distSqr=0.0; 
	//	for(int i=0; i<NUMBER_OF_FEATURES;i++) 
	//		distSqr += weights[i]*sqr(target[i]-meanX[i])/sX[i];
	//	return /*logDCfactor*0.001 +*/ -0.5*distSqr*weightSum;
	//}

	void RecomputeDCfactor();

	void CombineWith(FeatureDistribution const & other);
	void CombineWith(FeatureVector const & vect, Float occurenceProb);
};

class CombinedFeatureDistribution {

public:
	FeatureDistribution state[SUB_SYMBOL_COUNT];

	FeatureDistribution(void);
	~FeatureDistribution(void);
	void initRandom();

	inline Float occurence() {return weightSum;}
	void ScaleWeightBy(double scaleFactor) { 
		for(int i=0;i<SUB_SYMBOL_COUNT;i++) 
			state[i].ScaleWeightBy(scaleFactor);
	}


	Float LogProbDensityOf(FeatureVector const & target) {
		Float ll = state[0].LogProbDensityOf(target);
		for(int i=1; i<SUB_SYMBOL_COUNT;i++) 
			ll = std::max(ll,state[i].LogProbDensityOf(target));
		return ll;
	}

	void RecomputeDCfactor() {
		for(int i=0;i<SUB_SYMBOL_COUNT;i++) 
			state[i].RecomputeDCfactor();
	}

	void CombineWith(FeatureDistribution const & other);
	void CombineWith(FeatureVector const & vect, Float occurenceProb);
};