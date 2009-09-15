#pragma once
#include "stdafx.h"
#include <math.h>
#include "HwrConfig.h"
#include "feature/featurevector.h"
#include "randHelper.h"
#include "LogNumber.h"


class FeatureDistribution
{

	double logDCfactor;
public:
	double weightSum;
	FeatureVector meanX, sX;//public for managed/native transition simplicity.


	FeatureDistribution(void): weightSum(double(0.0)), meanX(double(0.0)), sX(double(0.0)),  logDCfactor(double(0.0)) {}
	double varX(int i) { return sX[i] / weightSum;}
	void setVarX(int i, double newVar) {sX[i] = newVar*weightSum;}
	double getWeightSum()const{return weightSum;}
	double getDCoffset()const{return logDCfactor;}

	void resetToZero();
	void initRandom();
	void RecomputeDCfactor();
	void CombineWithDistribution(FeatureDistribution const & other);
	void CombineWith(FeatureVector const & vect, double occurenceProb);
	void ScaleWeightBy(double scaleFactor);

	inline double LogProbDensityOf(FeatureVector const & target)const {
		double distSqr=0.0; 
		for(int i=0; i<NUMBER_OF_FEATURES;i++) 
			distSqr += sqr(target[i]-meanX[i])/sX[i];
		return /*logDCfactor*0.001 +*/ -0.5*distSqr*weightSum;
	}
};
