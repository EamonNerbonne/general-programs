#include "stdafx.h"
#include "LvqPrototype.h"


LvqPrototype::LvqPrototype(int protoLabel, int thisIndex, VectorXd const & initialVal) 
	: classLabel(protoLabel)
	, protoIndex(thisIndex)
	, point(initialVal) 
	, B(new Matrix2d())
{ 
	B->setIdentity();
	point.setZero(); 
}

LvqPrototype::~LvqPrototype(void) { }
