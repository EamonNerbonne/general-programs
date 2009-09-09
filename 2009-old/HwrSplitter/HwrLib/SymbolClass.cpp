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
