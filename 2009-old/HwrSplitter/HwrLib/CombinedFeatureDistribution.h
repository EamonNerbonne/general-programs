#pragma once
#include "stdafx.h"
#include "HwrConfig.h"
#include "FeatureDistribution.h"

class CombinedFeatureDistribution {
public:
	FeatureDistribution state[SUB_STATE_COUNT];

	//CombinedFeatureDistribution(void){}//TODO:delete
	//~CombinedFeatureDistribution(void){}//TODO:delete
	void initRandom();
	void resetToZero();
	void ScaleWeightBy(double scaleFactor);
	void RecomputeDCfactor();
	void CombineWithDistribution(CombinedFeatureDistribution const & other);
	//learningTarget may be the current distribution, but to permit parallelism, it may be a different target that 
	//can be combined into the current distribution at a thread-safe time via CombineWithDistribution
	void CombineInto(FeatureVector const & vect, double occurenceProb, CombinedFeatureDistribution & learningTarget)const;

	inline double LogProbDensityOf(FeatureVector const & target) const {
		double ll = state[0].LogProbDensityOf(target);
		for(int i=1; i<SUB_STATE_COUNT;i++) 
			ll = std::max(ll,state[i].LogProbDensityOf(target));
		return ll;
	}
	int CheckConsistency(){
		int errs =0 ;
		for(int i=0;i<SUB_STATE_COUNT;i++)
			errs+= state[i].CheckConsistency();
		return errs;
	}


};