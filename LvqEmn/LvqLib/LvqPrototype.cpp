#include "stdafx.h"
#include "LvqPrototype.h"


LvqPrototype::LvqPrototype(int protoLabel, int thisIndex) :classLabel(protoLabel), protoIndex(thisIndex) { B.setIdentity(); point.setZero(); }

LvqPrototype::~LvqPrototype(void) { }
