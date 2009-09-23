#include "StdAfx.h"
#include "CombinedFeatureDistribution.h"

void CombinedFeatureDistribution::initRandom(){
	for(int i=0;i<SUB_STATE_COUNT;i++) 
		state[i].initRandom();
}
void CombinedFeatureDistribution::resetToZero(){
	for(int i=0;i<SUB_STATE_COUNT;i++) 
		state[i].resetToZero();
}


void CombinedFeatureDistribution::ScaleWeightBy(double scaleFactor) { 
	for(int i=0;i<SUB_STATE_COUNT;i++) 
		state[i].ScaleWeightBy(scaleFactor);
}

void CombinedFeatureDistribution::RecomputeDCfactor() {
	for(int i=0;i<SUB_STATE_COUNT;i++) 
		state[i].RecomputeDCfactor();
}

void CombinedFeatureDistribution::CombineWithDistribution(CombinedFeatureDistribution const & other) {
	for(int i=0; i<SUB_STATE_COUNT;i++) 
		state[i].CombineWithDistribution(other.state[i]);
}

//learningTarget may be the current distribution, but to permit parallelism, it may be a different target that 
//can be combined into the current distribution at a thread-safe time via CombineWithDistribution
void CombinedFeatureDistribution::CombineInto(FeatureVector const & vect, double occurenceProb, CombinedFeatureDistribution & learningTarget) const {
	double ll[SUB_STATE_COUNT];

	for(int i=0;i<SUB_STATE_COUNT;i++) 
		ll[i] = state[i].LogProbDensityOf(vect);
	double maxLL = ll[0];
	int maxLLi=0;
	for(int i=1;i<SUB_STATE_COUNT;i++) {
		if(ll[i]>maxLL){
			maxLLi = i;
			maxLL = ll[i];
		}
	}
	if(learningTarget.CheckConsistency()>0)
		std::cout<<"inconsistent\n";

	for(int i=0;i<SUB_STATE_COUNT;i++) {
		learningTarget.state[i].CombineWith(vect, occurenceProb * exp( ll[i] - maxLL + (i==maxLLi?0:-1)  )  );
		if(learningTarget.CheckConsistency()>0)
			std::cout<<"inconsistent\n";

	}
}
