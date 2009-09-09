#pragma once
#include "HwrConfig.h"
#include "FeatureDistribution.h"

class SymbolClass
{
public:
	Float mLength;
	Float sLength;
	Float wLength;
	wchar_t originalChar;
	CombinedFeatureDistribution phase[SUB_PHASE_COUNT];
	SymbolClass(Float meanLength, Float varLength) : mLength(meanLength), wLength(100), sLength(varLength*100)	{ }
	SymbolClass() : mLength(0.0), wLength(0.0), sLength(0.0) {}
	
	Float meanLength() const {return mLength;}
	Float varLength() const {return sLength/wLength;}
	Float weightLength() const {return wLength;}

	void ScaleWeightBy(double scaleFactor) {
		wLength*=scaleFactor;
		sLength*=scaleFactor;
		for (int i=0;i<SUB_PHASE_COUNT;i++) 
			phase[i].ScaleWeightBy(scaleFactor);
	}


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
		for(int i=0;i<SUB_PHASE_COUNT;i++)
			phase[i].RecomputeDCfactor();
	}
	void initializeRandomly();
};
