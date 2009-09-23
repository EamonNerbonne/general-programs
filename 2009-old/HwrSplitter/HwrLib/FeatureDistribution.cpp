#include "StdAfx.h"
#include "FeatureDistribution.h"

static const double scaleFactor() {return (double)std::pow(2.0*M_PI, -0.5*NUMBER_OF_FEATURES);}
static const double logScaleFactor() {return (double)log(2.0*M_PI)*(-0.5*NUMBER_OF_FEATURES);}

static const double scaleFactorC = scaleFactor();
static const double logScaleFactorC = logScaleFactor();
static const double mrV = 1.0000000001;

//slight variant of CombineWith(vector, weight) to account for variance inside 
void FeatureDistribution::CombineWithDistribution(FeatureDistribution const & other){
	double newWeightSum = weightSum + other.weightSum;
	double mScale = other.weightSum/newWeightSum;
	double sScale = weightSum*other.weightSum/newWeightSum;
	weightSum = newWeightSum;
	for(int i=0;i<NUMBER_OF_FEATURES;i++) {
		sX[i] = sX[i] + other.sX[i] + sqr(other.meanX[i] - meanX[i])*sScale;
		meanX[i] = meanX[i] + (other.meanX[i] - meanX[i])*mScale;
	}
}

void FeatureDistribution::initRandom()
{
	weightSum = 1000.0;//essentially 10-100 lines of weight.
	for(int i=0;i<NUMBER_OF_FEATURES;i++) {
		meanX[i] = FloatRand()*1.0;
		setVarX(i, 1.0);//		variance[i] = 1000.0;
	}
	RecomputeDCfactor();
}

void FeatureDistribution::resetToZero()
{
	logDCfactor=0.0;
	weightSum = 0.0;//essentially 10-100 lines of weight.
	meanX.clear(0.0);
	sX.clear(0.0);
}

void FeatureDistribution::ScaleWeightBy(double scaleFactor) { 
	weightSum*=scaleFactor;
	for (int i=0;i<NUMBER_OF_FEATURES;i++) 
		sX[i]*=scaleFactor;
}

//D. H. D. West (1979). Communications of the ACM, 22, 9, 532-535: Updating Mean and Variance Estimates: An Improved Method
//not going to bother with n/(n-1) factor; this is going to be virtually irrelevant anyhow.
void FeatureDistribution::CombineWith(FeatureVector const & vect, double weight){
	if(weight < std::numeric_limits<double>::min())
		return;//ignore virtually zero-weight stuff...
	double newWeightSum = weightSum + weight;
	double mScale = weight/newWeightSum;
	double sScale = weightSum*weight/newWeightSum;
	weightSum = newWeightSum;
	for(int i=0;i<NUMBER_OF_FEATURES;i++) {
		sX[i] = sX[i] + sqr(vect[i] - meanX[i])*sScale;
		meanX[i] = meanX[i] + (vect[i] - meanX[i])*mScale;
	}
}


void FeatureDistribution::RecomputeDCfactor(){
	logDCfactor = logScaleFactorC;
	for(int i=0;i<NUMBER_OF_FEATURES;i++) 
		logDCfactor+= -0.5*log( varX(i) );
}

