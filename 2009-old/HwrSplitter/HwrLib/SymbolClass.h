#pragma once
#include "HwrConfig.h"
#include "FeatureDistribution.h"
#define SUB_SYMBOL_COUNT 1
//#define SUB_STATE_COUNT (SUB_SYMBOL_COUNT + 1)
//#define TERMINATOR_STATE SUB_SYMBOL_COUNT

double const defaultWeight = 1.0;
double const defaultVariance = sqr(1000);

class SymbolClass
{
	Float mLength;
	Float sLength;
	Float wLength;
public:
	FeatureDistribution state[SUB_SYMBOL_COUNT];
	SymbolClass(Float meanLength, Float varLength) : mLength(meanLength), wLength(100), sLength(varLength*100)	{ }
	SymbolClass() : mLength(100*FloatRand()), wLength(defaultWeight), sLength(defaultWeight * defaultVariance) {}
	
	Float meanLength() const {return mLength;}
	Float varLength() const {return sLength/wLength;}
	Float weightLength() const {return wLength;}
	

	double LogLikelihoodLength(double length) const {
		return -0.5*sqr(length - mLength)/sLength*wLength; //we can ignore the DC offset - after all this is constant for any length.
	}
	void LearnLength(Float length, Float weight) {
		Float newWeight = wLength + weight;
		sLength = sLength + sqr(length - mLength)*wLength*weight/newWeight;
		mLength = mLength + (length - mLength)*weight/newWeight;
		wLength = newWeight;
	}

	void RecomputeDCoffset() {
		for(int i=0;i<SUB_SYMBOL_COUNT;i++)
			state[i].RecomputeDCfactor();
	}
	void initRandom();
};
