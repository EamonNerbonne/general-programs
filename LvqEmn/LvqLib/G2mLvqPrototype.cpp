#include "stdafx.h"
#include "G2mLvqPrototype.h"


G2mLvqPrototype::G2mLvqPrototype(int protoLabel, int thisIndex, VectorXd const & initialVal) 
	: classLabel(protoLabel)
	, protoIndex(thisIndex)
	, point(initialVal) 
	, B(new Matrix2d())
{ 
	B->setIdentity();
	point.setZero(); 
}
