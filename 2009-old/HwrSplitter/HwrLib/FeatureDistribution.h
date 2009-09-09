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

	void ScaleWeightBy(double scaleFactor) { 
		weightSum*=scaleFactor;
		for (int i=0;i<NUMBER_OF_FEATURES;i++) 
			sX[i]*=scaleFactor;
	}


	Float LogProbDensityOf(FeatureVector const & target)const {
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

	void CombineWithDistribution(FeatureDistribution const & other);
	void CombineWith(FeatureVector const & vect, Float occurenceProb);
};

class CombinedFeatureDistribution {

public:
	FeatureDistribution state[SUB_STATE_COUNT];

	CombinedFeatureDistribution(void){}
	~CombinedFeatureDistribution(void){}
	void initRandom(){
		for(int i=0;i<SUB_STATE_COUNT;i++) 
			state[i].initRandom();
	}

	void ScaleWeightBy(double scaleFactor) { 
		for(int i=0;i<SUB_STATE_COUNT;i++) 
			state[i].ScaleWeightBy(scaleFactor);
	}


	Float LogProbDensityOf(FeatureVector const & target) const{
		Float ll = state[0].LogProbDensityOf(target);
		for(int i=1; i<SUB_STATE_COUNT;i++) 
			ll = std::max(ll,state[i].LogProbDensityOf(target));
		return ll;
	}

	void RecomputeDCfactor() {
		for(int i=0;i<SUB_STATE_COUNT;i++) 
			state[i].RecomputeDCfactor();
	}

	void CombineWithDistribution(CombinedFeatureDistribution const & other) {
		for(int i=1; i<SUB_STATE_COUNT;i++) 
			state[i].CombineWithDistribution(other.state[i]);
	}
	
	//learningTarget may be the current distribution, but to permit parallelism, it may be a different target that 
	//can be combined into the current distribution at a thread-safe time via CombineWithDistribution
	void CombineInto(FeatureVector const & vect, Float occurenceProb, CombinedFeatureDistribution & learningTarget)const {
		Float ll[SUB_STATE_COUNT];
		
		for(int i=0;i<SUB_STATE_COUNT;i++) 
			ll[i] = state[i].LogProbDensityOf(vect);
		Float maxLL = ll[0];
		int maxLLi=0;
		for(int i=1;i<SUB_STATE_COUNT;i++) {
			if(ll[i]>maxLL){
				maxLLi = i;
				maxLL = ll[i];
			}
		}
		for(int i=0;i<SUB_STATE_COUNT;i++) 
			learningTarget.state[i].CombineWith(vect, occurenceProb * exp( ll[i] - maxLL + (i==maxLLi?0:-1)  )  );
	}
};