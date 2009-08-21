#include "StdAfx.h"
#include "FeatureDistribution.h"

//to build boost threads lib, in boost_1_39_0 dir execute:
//bjam --toolset=msvc --build-type=complete --with-thread link=static stage

static const Float scaleFactor() {return (Float)std::pow(2.0*M_PI, -0.5*NUMBER_OF_FEATURES);}
static const Float logScaleFactor() {return (Float)log(2.0*M_PI)*(-0.5*NUMBER_OF_FEATURES);}

static const Float scaleFactorC = scaleFactor();
static const Float logScaleFactorC = logScaleFactor();
static const Float mrV = 1.0000000001;


FeatureDistribution::FeatureDistribution(void): weightSum(Float(0.0)), meanX(Float(0.0)), sX(Float(0.0)){}

FeatureDistribution::~FeatureDistribution(void){}

//slight variant of CombineWith(vector, weight) to account for variance inside 
void FeatureDistribution::CombineWith(FeatureDistribution const & other){
	Float newWeightSum = weightSum + other.weightSum;
	Float mScale = other.weightSum/newWeightSum;
	Float sScale = weightSum*other.weightSum/newWeightSum;
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

//D. H. D. West (1979). Communications of the ACM, 22, 9, 532-535: Updating Mean and Variance Estimates: An Improved Method
//not going to bother with n/(n-1) factor; this is going to be virtually irrelevant anyhow.
void FeatureDistribution::CombineWith(FeatureVector const & vect, Float weight){
	Float newWeightSum = weightSum + weight;
	Float mScale = weight/newWeightSum;
	Float sScale = weightSum*weight/newWeightSum;
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

