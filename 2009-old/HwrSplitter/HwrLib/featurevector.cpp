//+----------------------------------------------------------------------------+
//| Handwriting recognition - Features                                         |
//| Feature vector type                                                        |
//|                                                                            |
//| By Twan van Laarhoven, Eamon Nerbonne                                      |
//+----------------------------------------------------------------------------+

// ----------------------------------------------------------------------------- : Includes
#include "stdafx.h"
#include "feature/featurevector.h"
#include "feature/features.h"

using namespace std;

// ----------------------------------------------------------------------------- : Feature vectors

FeatureVector::FeatureVector(double def) {
	fill_n(features, size, def);
}

void FeatureVector::clear(double def) {
	fill_n(features, size, def);
}


