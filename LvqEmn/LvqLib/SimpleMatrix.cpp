#include "StdAfx.h"
#include "SimpleMatrix.h"


SimpleMatrix::SimpleMatrix(int withRows, int withCols)
	: rows(withRows), cols(withCols), underlying_data(new double[withRows*withCols])
{}


SimpleMatrix::~SimpleMatrix(void){}

void SimpleMatrix::clear(void) {
	for(int i=0;i<rows*cols;i++) underlying_data[i] =0.0;
}

void SimpleMatrix::mulInto(SimpleMatrix const & right, SimpleMatrix & sink) const {
 //TODO:
}

