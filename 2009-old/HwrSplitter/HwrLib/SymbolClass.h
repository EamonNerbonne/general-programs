#pragma once
#include "HwrConfig.h"
#include "CombinedFeatureDistribution.h"

class SymbolClass
{
public:
	double mLength;
	double sLength;
	double wLength;
	wchar_t originalChar;
	CombinedFeatureDistribution phase[SUB_PHASE_COUNT];
	SymbolClass(double meanLength, double varLength) : mLength(meanLength), wLength(100), sLength(varLength*100)	{ }
	SymbolClass() : mLength(0.0), wLength(0.0), sLength(0.0) {}
	
	inline double LogLikelihoodLength(double length) const { return -0.5*sqr(length - mLength)/sLength*wLength; } //we can ignore the DC offset - after all this is constant for any length.

	void ScaleWeightBy(double scaleFactor);
	void LearnLength(double length, double weight);
	void RecomputeDCoffset();
	void initializeRandomly();
	void resetToZero();
	void CombineWithDistribution(SymbolClass const & other);
};
