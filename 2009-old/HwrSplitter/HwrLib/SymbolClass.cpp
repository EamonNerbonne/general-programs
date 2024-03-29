#include "StdAfx.h"
#include "SymbolClass.h"
void SymbolClass::initializeRandomly()
{
	mLength = 100*FloatRand();
	wLength = DefaultFeatureWeight;
	sLength = DefaultFeatureWeight * DefaultFeatureVariance;
	for(int i=0;i<SUB_PHASE_COUNT;i++)
		phase[i].initRandom();
}

void SymbolClass::resetToZero()
{
	mLength = 0.0;
	wLength = 0.0;
	sLength = 0.0;
	for(int i=0;i<SUB_PHASE_COUNT;i++)
		phase[i].resetToZero();
}

void SymbolClass::ScaleWeightBy(double scaleFactor) {
	wLength*=scaleFactor;
	sLength*=scaleFactor;
	for (int i=0;i<SUB_PHASE_COUNT;i++) 
		phase[i].ScaleWeightBy(scaleFactor);
}

void SymbolClass::LearnLength(double length, double weight) {
	if(weight==0) return;
	double newWeight = wLength + weight;
	sLength = sLength + sqr(length - mLength)*wLength*weight/newWeight;
	mLength = mLength + (length - mLength)*weight/newWeight;
	wLength = newWeight;
}

void SymbolClass::RecomputeDCoffset() {
	for(int i=0;i<SUB_PHASE_COUNT;i++)
		phase[i].RecomputeDCfactor();
}

void SymbolClass::CombineWithDistribution(SymbolClass const & other) {
	for(int i=0;i<SUB_PHASE_COUNT;i++)
		phase[i].CombineWithDistribution(other.phase[i]);
	double newWeightLength = wLength + other.wLength;
	double mScale = other.wLength/newWeightLength;
	double sScale = wLength*other.wLength/newWeightLength;
	wLength = newWeightLength;
	sLength = sLength + other.sLength+ sqr(other.mLength - mLength)*sScale;
	mLength = mLength + (other.mLength - mLength)*mScale;
}