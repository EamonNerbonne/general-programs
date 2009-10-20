#include "stdafx.h"
#include "LvqPrototype.h"


LvqPrototype::LvqPrototype(int protoLabel, int thisIndex, VectorXd const & initialVal) 
	: classLabel(protoLabel)
	, protoIndex(thisIndex)
	, point(initialVal) 
	, tmpDiff(initialVal.rows())
	, B(new Matrix2d())
{ 
	B->setIdentity();
	point.setZero(); 
}
